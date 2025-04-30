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
                Description = translation?.TranslatedDescription ?? site.Description,
                Location = site.Location,
                SiteType = site.SiteType
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
                Summary = translation?.TranslatedSummary ?? culturalInfo.Summary,
                EstablishedDate = culturalInfo.EstablishedDate
            };

            return Json(translationData);
        }

        // GET: /Translation/GetCityTranslation/cityname?lang=ar
        [HttpGet]
        public async Task<IActionResult> GetCityTranslation(string id, string lang = "en")
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest("City name is required");
            }

            // Normalize the city name
            id = id.ToLower();

            // Get city from database
            var city = await _context.City
                .FirstOrDefaultAsync(c => c.Name.ToLower() == id);

            if (city == null)
            {
                return NotFound();
            }

            // In a real implementation, we would fetch the translation from a database
            // For this example, we're using a simple dictionary for demonstration
            var translations = new Dictionary<string, Dictionary<string, string>>
            {
                { "ar", new Dictionary<string, string>
                    {
                        { "Madinah", "المدينة المنورة" },
                        { "Riyadh", "الرياض" },
                        { "AlUla", "العلا" },
                        { "Historical Sites in", "المواقع التاريخية في" },
                        { "Available Guides in", "المرشدون المتاحون في" },
                        { "Interactive Map of", "خريطة تفاعلية لـ" }
                    }
                },
                { "fr", new Dictionary<string, string>
                    {
                        { "Madinah", "Médine" },
                        { "Riyadh", "Riyad" },
                        { "AlUla", "AlUla" },
                        { "Historical Sites in", "Sites historiques à" },
                        { "Available Guides in", "Guides disponibles à" },
                        { "Interactive Map of", "Carte interactive de" }
                    }
                },
                { "es", new Dictionary<string, string>
                    {
                        { "Madinah", "Medina" },
                        { "Riyadh", "Riad" },
                        { "AlUla", "AlUla" },
                        { "Historical Sites in", "Sitios históricos en" },
                        { "Available Guides in", "Guías disponibles en" },
                        { "Interactive Map of", "Mapa interactivo de" }
                    }
                }
            };

            // Get translated name if available
            string translatedName = city.Name;
            if (lang != "en" && translations.ContainsKey(lang) && translations[lang].ContainsKey(city.Name))
            {
                translatedName = translations[lang][city.Name];
            }

            // Get section headings translations
            var headings = new Dictionary<string, string>();
            if (lang != "en" && translations.ContainsKey(lang))
            {
                foreach (var key in new[] { "Historical Sites in", "Available Guides in", "Interactive Map of" })
                {
                    if (translations[lang].ContainsKey(key))
                    {
                        headings[key] = translations[lang][key];
                    }
                }
            }

            var translationData = new
            {
                CityId = city.Id,
                LanguageCode = lang,
                Name = translatedName,
                Headings = headings
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

        // GET: /Translation/GetPageTranslations?lang=ar
        [HttpGet]
        public IActionResult GetPageTranslations(string lang = "en")
        {
            if (lang == "en")
            {
                return Json(new { });
            }

            // Get translations from the service
            var translationService = HttpContext.RequestServices.GetService(typeof(Services.TranslationService)) as Services.TranslationService;

            if (translationService == null)
            {
                return Json(new { error = "Translation service not available" });
            }

            var translations = translationService.GetAllTranslations(lang);
            return Json(translations);
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