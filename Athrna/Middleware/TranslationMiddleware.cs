using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Athrna.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack; // We'll use this to properly parse HTML

namespace Athrna.Middleware
{
    public class TranslationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<TranslationMiddleware> _logger;

        // Elements and attributes that should not be translated
        private static readonly HashSet<string> _excludedElements = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "script", "style", "code", "pre", "textarea", "input", "button", "select", "option"
        };

        private static readonly HashSet<string> _excludedAttributes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "href", "src", "alt", "title", "placeholder", "value", "type", "id", "class", "style", "data-","name", "for", "action", "method", "required", "disabled", "readonly", "selected"
        };

        public TranslationMiddleware(RequestDelegate next, ILogger<TranslationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, TranslationService translationService)
        {
            // Skip static files and API calls
            var path = context.Request.Path.Value;
            if (path.StartsWith("/css") ||
                path.StartsWith("/js") ||
                path.StartsWith("/lib") ||
                path.StartsWith("/images") ||
                path.StartsWith("/api") ||
                path.StartsWith("/Admin") ||
                path.StartsWith("/GuideDashboard"))
            {
                await _next(context);
                return;
            }

            // Get selected language from cookie or query string
            string targetLanguage = GetSelectedLanguage(context);

            // If no translation needed, just continue the pipeline
            if (string.IsNullOrEmpty(targetLanguage) || targetLanguage.Equals("en", StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }

            // Store original response body stream
            var originalBodyStream = context.Response.Body;

            try
            {
                // Create a new memory stream to capture the response
                using var responseBody = new MemoryStream();
                context.Response.Body = responseBody;

                // Continue down the middleware pipeline
                await _next(context);

                // Check if the response is HTML
                if (context.Response.ContentType?.ToLower().Contains("text/html") == true)
                {
                    // Reset the memory stream position
                    responseBody.Seek(0, SeekOrigin.Begin);

                    // Read the response body
                    var responseBodyText = await new StreamReader(responseBody).ReadToEndAsync();

                    // Skip translation if the response is too large (> 1MB to prevent memory issues)
                    if (responseBodyText.Length > 1024 * 1024)
                    {
                        _logger.LogWarning("Response too large for translation: {Size} bytes", responseBodyText.Length);
                        responseBody.Seek(0, SeekOrigin.Begin);
                        await responseBody.CopyToAsync(originalBodyStream);
                        return;
                    }

                    // Translate the response body
                    var translatedContent = await TranslateHtmlAsync(responseBodyText, targetLanguage, translationService);

                    // Replace the response with the translated content
                    var translatedBytes = Encoding.UTF8.GetBytes(translatedContent);
                    await originalBodyStream.WriteAsync(translatedBytes, 0, translatedBytes.Length);
                }
                else
                {
                    // For non-HTML responses, just copy the stream
                    responseBody.Seek(0, SeekOrigin.Begin);
                    await responseBody.CopyToAsync(originalBodyStream);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in TranslationMiddleware");

                // On error, reset the context
                context.Response.Body = originalBodyStream;

                // Continue the pipeline
                await _next(context);
            }
            finally
            {
                // Always restore the original body stream
                context.Response.Body = originalBodyStream;
            }
        }

        private string GetSelectedLanguage(HttpContext context)
        {
            // First check query string
            if (context.Request.Query.TryGetValue("lang", out var queryLang) &&
                !string.IsNullOrEmpty(queryLang))
            {
                // Save language to cookie
                context.Response.Cookies.Append("Athrna_Language", queryLang,
                    new CookieOptions { Expires = DateTimeOffset.Now.AddDays(30) });

                return queryLang;
            }

            // Then check cookie
            if (context.Request.Cookies.TryGetValue("Athrna_Language", out var cookieLang) &&
                !string.IsNullOrEmpty(cookieLang))
            {
                return cookieLang;
            }

            // Default to English
            return "en";
        }

        private async Task<string> TranslateHtmlAsync(string html, string targetLanguage, TranslationService translationService)
        {
            try
            {
                // Load the HTML document
                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                // First pass: collect all translatable text nodes and elements with data-translate attribute
                var textsToTranslate = new Dictionary<string, List<HtmlNode>>();

                // Preserve specific elements that should not be removed during translation
                var siteDescriptionSections = doc.DocumentNode.SelectNodes("//section[contains(@class, 'site-description-section')]");
                if (siteDescriptionSections != null)
                {
                    foreach (var section in siteDescriptionSections)
                    {
                        section.SetAttributeValue("data-preserve", "true");
                    }
                }

                // Process elements with data-translate attribute first
                var dataTranslateNodes = doc.DocumentNode.SelectNodes("//*[@data-translate]");
                if (dataTranslateNodes != null)
                {
                    foreach (var node in dataTranslateNodes)
                    {
                        var text = node.GetAttributeValue("data-translate", "").Trim();
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            if (!textsToTranslate.ContainsKey(text))
                                textsToTranslate[text] = new List<HtmlNode>();

                            textsToTranslate[text].Add(node);
                        }
                    }
                }

                // Then process regular text nodes
                var textNodes = doc.DocumentNode.SelectNodes("//text()[normalize-space(.) != '']");
                if (textNodes != null)
                {
                    foreach (var node in textNodes)
                    {
                        // Skip if parent is in excluded elements
                        if (node.ParentNode != null && _excludedElements.Contains(node.ParentNode.Name))
                            continue;

                        // Skip if parent or any ancestor has data-no-translate attribute
                        var parent = node.ParentNode;
                        bool skipTranslation = false;
                        while (parent != null)
                        {
                            if (parent.Attributes["data-no-translate"] != null)
                            {
                                skipTranslation = true;
                                break;
                            }
                            parent = parent.ParentNode;
                        }

                        if (skipTranslation)
                            continue;

                        var text = node.InnerText.Trim();

                        // Skip short texts, numbers, dates, and special content
                        if (string.IsNullOrWhiteSpace(text) ||
                            text.Length < 3 ||
                            Regex.IsMatch(text, @"^\d+(\.\d+)?$|^\d{1,2}[/\-\.]\d{1,2}[/\-\.]\d{2,4}$") ||
                            Regex.IsMatch(text, @"^(http|https|ftp|file|www)"))
                            continue;

                        if (!textsToTranslate.ContainsKey(text))
                            textsToTranslate[text] = new List<HtmlNode>();

                        textsToTranslate[text].Add(node);
                    }
                }

                // Translate all texts in smaller batches
                var allTexts = textsToTranslate.Keys.ToList();
                var translatedTexts = await translationService.TranslateBatchAsync(allTexts, targetLanguage);

                // Create translation dictionary
                var translationDict = new Dictionary<string, string>();
                for (int i = 0; i < allTexts.Count; i++)
                {
                    translationDict[allTexts[i]] = translatedTexts[i];
                }

                // Apply translations
                foreach (var entry in textsToTranslate)
                {
                    var originalText = entry.Key;
                    var nodes = entry.Value;

                    if (translationDict.TryGetValue(originalText, out var translatedText))
                    {
                        foreach (var node in nodes)
                        {
                            if (node.NodeType == HtmlNodeType.Text)
                            {
                                // Replace the text node content
                                node.InnerHtml = node.InnerHtml.Replace(originalText, translatedText);
                            }
                            else if (node.HasAttributes && node.Attributes["data-translate"] != null)
                            {
                                // Update the InnerHtml of data-translate nodes
                                node.InnerHtml = translatedText;
                            }
                        }
                    }
                }

                // Return the translated HTML
                return doc.DocumentNode.OuterHtml;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error translating HTML");
                return html; // Return original HTML on error
            }
        }
    }
}