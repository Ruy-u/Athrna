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

            // Define just 5 core languages
            var languages = new List<LanguageInfo>
            {
                new LanguageInfo { Code = "en", Name = "English" },
                new LanguageInfo { Code = "ar", Name = "العربية" }, // Arabic
                new LanguageInfo { Code = "fr", Name = "Français" },
                new LanguageInfo { Code = "de", Name = "Deutsch" },
                new LanguageInfo { Code = "es", Name = "Español" }
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
    }
}