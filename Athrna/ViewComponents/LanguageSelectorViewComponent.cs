using System.Threading.Tasks;
using Athrna.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace Athrna.ViewComponents
{
    public class LanguageSelectorViewComponent : ViewComponent
    {
        public async Task<IViewComponentResult> InvokeAsync()
        {
            // Get currently selected language from cookie or default to English
            string currentLanguage = "en";
            if (Request.Cookies.TryGetValue("Athrna_Language", out var lang) && !string.IsNullOrEmpty(lang))
            {
                currentLanguage = lang;
            }

            // Define expanded list of languages
            var languages = new List<LanguageInfo>
            {
                new LanguageInfo { Code = "en", Name = "English" },
                new LanguageInfo { Code = "ar", Name = "العربية", IsRtl = true }, // Arabic - with RTL flag
                new LanguageInfo { Code = "zh", Name = "中文" }, // Chinese
                new LanguageInfo { Code = "es", Name = "Español" }, // Spanish
                new LanguageInfo { Code = "hi", Name = "हिन्दी" }, // Hindi
                new LanguageInfo { Code = "fr", Name = "Français" }, // French
                new LanguageInfo { Code = "bn", Name = "বাংলা" }, // Bengali
                new LanguageInfo { Code = "ru", Name = "Русский" }, // Russian
                new LanguageInfo { Code = "pt", Name = "Português" }, // Portuguese
                new LanguageInfo { Code = "ja", Name = "日本語" }, // Japanese
                new LanguageInfo { Code = "de", Name = "Deutsch" }, // German
            };

            // Create the model
            var model = new LanguageSelectorViewModel
            {
                CurrentLanguage = currentLanguage,
                Languages = languages
            };

            return View(model);
        }
    }

    public class LanguageSelectorViewModel
    {
        public string CurrentLanguage { get; set; }
        public List<LanguageInfo> Languages { get; set; }
    }

    public class LanguageInfo
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public bool IsRtl { get; set; } = false; // Added RTL flag
    }
}