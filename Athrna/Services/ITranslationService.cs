using System.Collections.Generic;
using System.Threading.Tasks;

namespace Athrna.Services
{
    public interface ITranslationService
    {
        /// <summary>
        /// Translates text to the specified language
        /// </summary>
        Task<string> TranslateTextAsync(string text, string targetLanguageCode, string sourceLanguageCode = "en");

        /// <summary>
        /// Translates multiple texts in batch to save API calls
        /// </summary>
        Task<IList<string>> TranslateTextBatchAsync(IList<string> texts, string targetLanguageCode, string sourceLanguageCode = "en");

        /// <summary>
        /// Gets current language from request cookies
        /// </summary>
        string GetCurrentLanguage(HttpContext httpContext);
    }
}