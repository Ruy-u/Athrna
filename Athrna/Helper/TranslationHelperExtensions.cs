using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;

namespace Athrna
{
    public static class TranslationHelperExtensions
    {
        public static string T(this IHtmlHelper htmlHelper, string key)
        {
            // Get current language from cookie or use default
            var httpContext = htmlHelper.ViewContext.HttpContext;
            string lang = "en";

            if (httpContext.Request.Cookies.TryGetValue("language", out var cookieLang))
            {
                lang = cookieLang;
            }

            // If English is requested, just return the original text
            if (lang == "en")
            {
                return key;
            }

            // Look up the translation
            var translationDict = htmlHelper.ViewContext.HttpContext.RequestServices
                .GetService<Dictionary<string, Dictionary<string, string>>>();

            if (translationDict != null &&
                translationDict.TryGetValue(lang, out var langDict) &&
                langDict.TryGetValue(key, out var translation))
            {
                return translation;
            }

            // Return original key if no translation found
            return key;
        }
    }
}