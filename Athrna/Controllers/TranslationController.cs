using Microsoft.AspNetCore.Mvc;
using Athrna.Data;
using Athrna.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace Athrna.Controllers
{
    public class TranslationController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TranslationController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Translation/SetLanguage/ar
        public IActionResult SetLanguage(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                code = "en"; // Default to English
            }

            // Store the selected language in a cookie
            Response.Cookies.Append("language", code, new CookieOptions
            {
                Expires = DateTimeOffset.Now.AddYears(1)
            });

            // Redirect back to the referring URL, or home if none
            string returnUrl = Request.Headers["Referer"].ToString();
            if (string.IsNullOrEmpty(returnUrl))
            {
                returnUrl = Url.Action("Index", "Home");
            }

            return Redirect(returnUrl);
        }

        // GET: /Translation/GetSiteTranslation/5?lang=ar
        [HttpGet]
        public async Task<IActionResult> GetSiteTranslation(int id, string lang = "en")
        {
            // Get the site and its default data
            var site = await _context.Site
                .Include(s => s.SiteTranslations)
                .ThenInclude(t => t.Language)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (site == null)
            {
                return NotFound();
            }

            // Get language ID
            var language = await _context.Language
                .FirstOrDefaultAsync(l => l.Code == lang);

            if (language == null)
            {
                return BadRequest("Invalid language code");
            }

            // Check if there's a translation for this site in the requested language
            var translation = site.SiteTranslations?
                .FirstOrDefault(t => t.LanguageId == language.Id);

            // Prepare the response data
            var translationData = new
            {
                SiteId = site.Id,
                LanguageCode = lang,
                Name = translation?.TranslatedName ?? site.Name,
                Description = translation?.TranslatedDescription ?? site.Description
            };

            return Json(translationData);
        }

        // GET: /Translation/GetCulturalInfoTranslation/5?lang=ar
        [HttpGet]
        public async Task<IActionResult> GetCulturalInfoTranslation(int id, string lang = "en")
        {
            // Get the cultural info and its default data
            var culturalInfo = await _context.CulturalInfo
                .Include(c => c.CulturalInfoTranslations)
                .ThenInclude(t => t.Language)
                .FirstOrDefaultAsync(c => c.SiteId == id);

            if (culturalInfo == null)
            {
                return NotFound();
            }

            // Get language ID
            var language = await _context.Language
                .FirstOrDefaultAsync(l => l.Code == lang);

            if (language == null)
            {
                return BadRequest("Invalid language code");
            }

            // Check if there's a translation for this cultural info in the requested language
            var translation = culturalInfo.CulturalInfoTranslations?
                .FirstOrDefault(t => t.Language.Code == lang);

            // Prepare the response data
            var translationData = new
            {
                SiteId = id,
                LanguageCode = lang,
                Summary = translation?.TranslatedSummary ?? culturalInfo.Summary
            };

            return Json(translationData);
        }

        // GET: /Translation/GetAvailableLanguages
        [HttpGet]
        public async Task<IActionResult> GetAvailableLanguages()
        {
            var languages = await _context.Language
                .Select(l => new
                {
                    l.Id,
                    l.Name,
                    l.Code
                })
                .ToListAsync();

            return Json(languages);
        }

        // Admin Methods

        // POST: /Translation/AddSiteTranslation
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> AddSiteTranslation(int siteId, int languageId, string translatedName, string translatedDescription)
        {
            // Check if site exists
            var site = await _context.Site.FindAsync(siteId);
            if (site == null)
            {
                return NotFound("Site not found");
            }

            // Check if language exists
            var language = await _context.Language.FindAsync(languageId);
            if (language == null)
            {
                return NotFound("Language not found");
            }

            // Check if translation already exists
            var existingTranslation = await _context.SiteTranslation
                .FirstOrDefaultAsync(t => t.SiteId == siteId && t.LanguageId == languageId);

            if (existingTranslation != null)
            {
                // Update existing translation
                existingTranslation.TranslatedName = translatedName;
                existingTranslation.TranslatedDescription = translatedDescription;
                _context.Update(existingTranslation);
            }
            else
            {
                // Create new translation
                var translation = new SiteTranslation
                {
                    SiteId = siteId,
                    LanguageId = languageId,
                    TranslatedName = translatedName,
                    TranslatedDescription = translatedDescription
                };
                _context.SiteTranslation.Add(translation);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("EditSite", "Admin", new { id = siteId });
        }

        // POST: /Translation/AddCulturalInfoTranslation
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> AddCulturalInfoTranslation(int culturalInfoId, int languageId, string translatedSummary)
        {
            // Check if cultural info exists
            var culturalInfo = await _context.CulturalInfo.FindAsync(culturalInfoId);
            if (culturalInfo == null)
            {
                return NotFound("Cultural info not found");
            }

            // Check if language exists
            var language = await _context.Language.FindAsync(languageId);
            if (language == null)
            {
                return NotFound("Language not found");
            }

            // Check if translation already exists
            var existingTranslation = await _context.CulturalInfoTranslation
                .FirstOrDefaultAsync(t => t.CulturalInfoId == culturalInfoId && t.LanguageId == languageId);

            if (existingTranslation != null)
            {
                // Update existing translation
                existingTranslation.TranslatedSummary = translatedSummary;
                _context.Update(existingTranslation);
            }
            else
            {
                // Create new translation
                var translation = new CulturalInfoTranslation
                {
                    CulturalInfoId = culturalInfoId,
                    LanguageId = languageId,
                    TranslatedSummary = translatedSummary
                };
                _context.CulturalInfoTranslation.Add(translation);
            }

            await _context.SaveChangesAsync();

            // Redirect back to the edit site page
            var siteId = culturalInfo.SiteId;
            return RedirectToAction("EditSite", "Admin", new { id = siteId });
        }
    }
}