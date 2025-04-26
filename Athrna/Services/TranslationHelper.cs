using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Athrna.Helpers
{
    public static class TranslationHelper
    {
        /// <summary>
        /// HTML helper extension method for translating text in Razor views
        /// </summary>
        public static IHtmlContent T(this IHtmlHelper htmlHelper, string key)
        {
            var translationService = (Services.TranslationService)htmlHelper.ViewContext.HttpContext.RequestServices
                .GetService(typeof(Services.TranslationService));

            if (translationService == null)
            {
                return new HtmlString(key);
            }

            // Get current language from cookies
            var languageCode = translationService.GetCurrentLanguage(htmlHelper.ViewContext.HttpContext);

            // Get translation
            string translated = translationService.Translate(key, languageCode);

            return new HtmlString(translated);
        }

        /// <summary>
        /// String extension method for translating text in C# code
        /// </summary>
        public static string Translate(this string key, IServiceProvider services, HttpContext httpContext)
        {
            var translationService = (Services.TranslationService)services.GetService(typeof(Services.TranslationService));

            if (translationService == null)
            {
                return key;
            }

            // Get current language from cookies
            var languageCode = translationService.GetCurrentLanguage(httpContext);

            // Get translation
            return translationService.Translate(key, languageCode);
        }
    }
}