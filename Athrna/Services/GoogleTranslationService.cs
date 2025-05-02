using Google.Cloud.Translation.V2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Athrna.Services
{
    public class GoogleTranslationService : ITranslationService
    {
        private readonly TranslationClient _translationClient;
        private readonly ILogger<GoogleTranslationService> _logger;
        private readonly IConfiguration _configuration;
        private readonly Dictionary<string, Dictionary<string, string>> _cache;

        public GoogleTranslationService(IConfiguration configuration, ILogger<GoogleTranslationService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _cache = new Dictionary<string, Dictionary<string, string>>();

            try
            {
                // Check if credential file path is provided
                var credentialPath = _configuration["GoogleTranslate:CredentialPath"];
                if (!string.IsNullOrEmpty(credentialPath))
                {
                    // Set environment variable for Google credentials
                    Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", credentialPath);
                }

                // Create the translation client
                _translationClient = TranslationClient.Create();
                _logger.LogInformation("Google Cloud Translation API client created successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create Google Cloud Translation API client");
                throw;
            }
        }

        /// <summary>
        /// Translates text to the specified language
        /// </summary>
        public async Task<string> TranslateTextAsync(string text, string targetLanguageCode, string sourceLanguageCode = "en")
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            if (targetLanguageCode == sourceLanguageCode || targetLanguageCode == "en")
                return text; // No translation needed

            try
            {
                // Check cache first
                if (TryGetCachedTranslation(text, targetLanguageCode, out var cachedTranslation))
                {
                    return cachedTranslation;
                }

                // Translate using Google Translation API
                var response = await Task.Run(() => _translationClient.TranslateText(
                    text: text,
                    targetLanguage: targetLanguageCode,
                    sourceLanguage: sourceLanguageCode));

                // Cache the result
                CacheTranslation(text, targetLanguageCode, response.TranslatedText);

                return response.TranslatedText;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error translating text to {TargetLanguage}: {Text}", targetLanguageCode, text);
                return text; // Return original text if translation fails
            }
        }

        /// <summary>
        /// Translates multiple texts in batch to save API calls
        /// </summary>
        public async Task<IList<string>> TranslateTextBatchAsync(IList<string> texts, string targetLanguageCode, string sourceLanguageCode = "en")
        {
            if (texts == null || texts.Count == 0 || targetLanguageCode == sourceLanguageCode || targetLanguageCode == "en")
                return texts; // No translation needed

            try
            {
                var translatedTexts = new List<string>();
                var textsToTranslate = new List<string>();
                var indicesOfTextsToTranslate = new List<int>();

                // Check which texts are in cache
                for (int i = 0; i < texts.Count; i++)
                {
                    var text = texts[i];
                    if (string.IsNullOrWhiteSpace(text))
                    {
                        translatedTexts.Add(text);
                    }
                    else if (TryGetCachedTranslation(text, targetLanguageCode, out var cachedTranslation))
                    {
                        translatedTexts.Add(cachedTranslation);
                    }
                    else
                    {
                        // Add placeholder
                        translatedTexts.Add(null);
                        textsToTranslate.Add(text);
                        indicesOfTextsToTranslate.Add(i);
                    }
                }

                // If there are texts to translate
                if (textsToTranslate.Count > 0)
                {
                    // Translate using Google Translation API
                    var responses = await Task.Run(() => _translationClient.TranslateText(
                        textsToTranslate,
                        targetLanguageCode,
                        sourceLanguageCode));

                    // Update translated texts and cache
                    for (int i = 0; i < responses.Count; i++)
                    {
                        var index = indicesOfTextsToTranslate[i];
                        var originalText = textsToTranslate[i];
                        var translatedText = responses[i].TranslatedText;

                        translatedTexts[index] = translatedText;
                        CacheTranslation(originalText, targetLanguageCode, translatedText);
                    }
                }

                return translatedTexts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error translating text batch to {TargetLanguage}", targetLanguageCode);
                return texts; // Return original texts if translation fails
            }
        }

        /// <summary>
        /// Gets current language from request cookies
        /// </summary>
        public string GetCurrentLanguage(HttpContext httpContext)
        {
            if (httpContext.Request.Cookies.TryGetValue("language", out var language))
            {
                return language;
            }

            return "en"; // Default to English
        }

        /// <summary>
        /// Try to get cached translation
        /// </summary>
        private bool TryGetCachedTranslation(string originalText, string languageCode, out string translation)
        {
            if (_cache.TryGetValue(languageCode, out var languageCache))
            {
                if (languageCache.TryGetValue(originalText, out translation))
                {
                    return true;
                }
            }

            translation = null;
            return false;
        }

        /// <summary>
        /// Cache translation for future use
        /// </summary>
        private void CacheTranslation(string originalText, string languageCode, string translation)
        {
            if (!_cache.TryGetValue(languageCode, out var languageCache))
            {
                languageCache = new Dictionary<string, string>();
                _cache[languageCode] = languageCache;
            }

            languageCache[originalText] = translation;
        }
    }
}