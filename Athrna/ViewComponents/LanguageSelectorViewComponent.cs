using Microsoft.AspNetCore.Mvc;
using Athrna.Data;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Athrna.Models;
using System.Collections.Generic;

namespace Athrna.ViewComponents
{
    public class LanguageSelectorViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;

        public LanguageSelectorViewComponent(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            // Get all available languages
            var languages = await _context.Language.ToListAsync();

            // Get current language from cookie
            string currentLanguage = "en"; // Default to English
            if (HttpContext.Request.Cookies.TryGetValue("language", out string langValue))
            {
                currentLanguage = langValue;
            }

            // Create the view model
            var model = new LanguageSelectorViewModel
            {
                Languages = languages,
                CurrentLanguageCode = currentLanguage
            };

            return View(model);
        }
    }

    public class LanguageSelectorViewModel
    {
        public IEnumerable<Language> Languages { get; set; }
        public string CurrentLanguageCode { get; set; }
    }
}