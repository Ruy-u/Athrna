using Google.Cloud.Translation.V2;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Athrna.Data;
using Athrna.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Athrna.Services
{
    public class TranslationService
    {
        private readonly TranslationClient _translationClient;
        private readonly ILogger<TranslationService> _logger;
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _dbContext;

        // In-memory cache for translations within a session
        private static Dictionary<string, Dictionary<string, string>> _memoryCache =
            new Dictionary<string, Dictionary<string, string>>();

        // Maximum number of segments to translate in a single batch
        private const int MaxBatchSize = 100;

        public TranslationService(
            IConfiguration configuration,
            ILogger<TranslationService> logger,
            ApplicationDbContext dbContext)
        {
            _configuration = configuration;
            _logger = logger;
            _dbContext = dbContext;

            // Initialize Google Cloud Translation client
            try
            {
                // Try to use API key from configuration
                string apiKey = _configuration["GoogleCloud:TranslationApiKey"];
                if (!string.IsNullOrEmpty(apiKey))
                {
                    _translationClient = TranslationClient.CreateFromApiKey(apiKey);
                    _logger.LogInformation("TranslationService initialized with API key");
                }
                else
                {
                    // Fall back to default credentials (for local development)
                    _translationClient = TranslationClient.Create();
                    _logger.LogInformation("TranslationService initialized with default credentials");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize TranslationService");
                // Don't rethrow - service will work in degraded mode
            }
        }

        public async Task<string> TranslateTextAsync(string text, string targetLanguage, string sourceLanguage = "en")
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            if (targetLanguage.Equals(sourceLanguage, StringComparison.OrdinalIgnoreCase))
                return text;

            try
            {
                // Check memory cache first
                if (_memoryCache.TryGetValue(text, out var languageDict) &&
                    languageDict.TryGetValue(targetLanguage, out var cachedTranslation))
                {
                    return cachedTranslation;
                }

                // Generate hash for lookup
                string textHash = ComputeHash(text);

                // Check database cache using the hash
                var existingTranslation = await _dbContext.Translation
                    .FirstOrDefaultAsync(t =>
                        t.SourceLanguage == sourceLanguage &&
                        t.TargetLanguage == targetLanguage &&
                        t.TextHash == textHash &&
                        t.OriginalText == text); // Double-check exact match

                if (existingTranslation != null)
                {
                    // Add to memory cache
                    if (!_memoryCache.ContainsKey(text))
                        _memoryCache[text] = new Dictionary<string, string>();

                    _memoryCache[text][targetLanguage] = existingTranslation.TranslatedText;

                    return existingTranslation.TranslatedText;
                }

                // Perform translation
                var response = await _translationClient.TranslateTextAsync(
                    text,
                    targetLanguage,
                    sourceLanguage);

                var translatedText = response.TranslatedText;

                // Save to database
                _dbContext.Translation.Add(new Translation
                {
                    SourceLanguage = sourceLanguage,
                    TargetLanguage = targetLanguage,
                    OriginalText = text,
                    TranslatedText = translatedText,
                    TextHash = textHash,
                    CreatedAt = DateTime.UtcNow
                });

                await _dbContext.SaveChangesAsync();

                // Add to memory cache
                if (!_memoryCache.ContainsKey(text))
                    _memoryCache[text] = new Dictionary<string, string>();

                _memoryCache[text][targetLanguage] = translatedText;

                return translatedText;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error translating text to {targetLanguage}: {text.Substring(0, Math.Min(100, text.Length))}...");
                return text; // Return original text on error
            }
        }

        public async Task<IList<string>> TranslateBatchAsync(IList<string> texts, string targetLanguage, string sourceLanguage = "en")
        {
            if (texts == null || texts.Count == 0)
                return new List<string>();

            if (targetLanguage.Equals(sourceLanguage, StringComparison.OrdinalIgnoreCase))
                return texts;

            try
            {
                var results = new List<string>(texts.Count);

                for (int i = 0; i < texts.Count; i += MaxBatchSize)
                {
                    var batch = texts.Skip(i).Take(MaxBatchSize).ToList();
                    var batchResults = new string[batch.Count];

                    var textsToTranslate = new List<string>();
                    var textsToTranslateIndices = new List<int>();

                    for (int j = 0; j < batch.Count; j++)
                    {
                        var text = batch[j];

                        if (string.IsNullOrWhiteSpace(text) || text.Length > 5000)
                        {
                            batchResults[j] = text;
                            continue;
                        }

                        if (_memoryCache.TryGetValue(text, out var languageDict) &&
                            languageDict.TryGetValue(targetLanguage, out var cachedTranslation))
                        {
                            batchResults[j] = cachedTranslation;
                            continue;
                        }

                        textsToTranslate.Add(text);
                        textsToTranslateIndices.Add(j);
                    }

                    if (textsToTranslate.Count > 0)
                    {
                        var textHashes = textsToTranslate.Select(ComputeHash).ToList();

                        var existingTranslations = await _dbContext.Translation
                            .Where(t =>
                                t.SourceLanguage == sourceLanguage &&
                                t.TargetLanguage == targetLanguage &&
                                textHashes.Contains(t.TextHash))
                            .ToListAsync();

                        var translationDict = existingTranslations
                            .Where(t => textsToTranslate.Contains(t.OriginalText))
                            .ToDictionary(t => t.OriginalText, t => t.TranslatedText);

                        var textsStillNeedingTranslation = new List<string>();
                        var textsStillNeedingTranslationIndices = new List<int>();

                        for (int j = 0; j < textsToTranslate.Count; j++)
                        {
                            var text = textsToTranslate[j];
                            if (translationDict.TryGetValue(text, out var dbTranslation))
                            {
                                batchResults[textsToTranslateIndices[j]] = dbTranslation;

                                if (!_memoryCache.ContainsKey(text))
                                    _memoryCache[text] = new Dictionary<string, string>();

                                _memoryCache[text][targetLanguage] = dbTranslation;
                            }
                            else
                            {
                                textsStillNeedingTranslation.Add(text);
                                textsStillNeedingTranslationIndices.Add(textsToTranslateIndices[j]);
                            }
                        }

                        if (textsStillNeedingTranslation.Count > 0)
                        {
                            for (int k = 0; k < textsStillNeedingTranslation.Count; k += 50)
                            {
                                var microBatch = textsStillNeedingTranslation.Skip(k).Take(50).ToList();
                                var microBatchIndices = textsStillNeedingTranslationIndices.Skip(k).Take(50).ToList();

                                var responses = await _translationClient.TranslateTextAsync(
                                    microBatch,
                                    targetLanguage,
                                    sourceLanguage);

                                var translationsToAdd = new List<Translation>();

                                for (int m = 0; m < responses.Count; m++)
                                {
                                    var originalText = microBatch[m];
                                    var translatedText = responses[m].TranslatedText;

                                    batchResults[microBatchIndices[m]] = translatedText;

                                    if (!_memoryCache.ContainsKey(originalText))
                                        _memoryCache[originalText] = new Dictionary<string, string>();

                                    _memoryCache[originalText][targetLanguage] = translatedText;

                                    translationsToAdd.Add(new Translation
                                    {
                                        SourceLanguage = sourceLanguage,
                                        TargetLanguage = targetLanguage,
                                        OriginalText = originalText,
                                        TranslatedText = translatedText,
                                        TextHash = ComputeHash(originalText),
                                        CreatedAt = DateTime.UtcNow
                                    });
                                }

                                if (translationsToAdd.Count > 0)
                                {
                                    await _dbContext.Translation.AddRangeAsync(translationsToAdd);
                                    await _dbContext.SaveChangesAsync();
                                }
                            }
                        }
                    }

                    results.AddRange(batchResults);
                }

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error batch translating texts to {targetLanguage}");
                return texts;
            }
        }

        // Get a list of supported languages for the dropdown
        public async Task<List<LanguageInfo>> GetSupportedLanguagesAsync(string displayLanguage = "en")
        {
            try
            {
                var response = await _translationClient.ListLanguagesAsync(displayLanguage);

                return response
                    .OrderBy(l => l.Name)
                    .Select(l => new LanguageInfo { Code = l.Code, Name = l.Name })
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting supported languages");

                // Return a fallback list of common languages that includes our expanded set
                return new List<LanguageInfo>
        {
            new LanguageInfo { Code = "en", Name = "English" },
            new LanguageInfo { Code = "ar", Name = "Arabic" },
            new LanguageInfo { Code = "zh", Name = "Chinese" },
            new LanguageInfo { Code = "es", Name = "Spanish" },
            new LanguageInfo { Code = "hi", Name = "Hindi" },
            new LanguageInfo { Code = "fr", Name = "French" },
            new LanguageInfo { Code = "bn", Name = "Bengali" },
            new LanguageInfo { Code = "ru", Name = "Russian" },
            new LanguageInfo { Code = "pt", Name = "Portuguese" },
            new LanguageInfo { Code = "ja", Name = "Japanese" },
            new LanguageInfo { Code = "de", Name = "German" }
        };
            }
        }
        private string ComputeHash(string text)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(text);
                var hashBytes = sha.ComputeHash(bytes);
                return Convert.ToBase64String(hashBytes).Substring(0, 44); // Slightly shorter than the full hash
            }
        }
    }

    public class LanguageInfo
    {
        public string Code { get; set; }
        public string Name { get; set; }
    }
}