using Microsoft.AspNetCore.Mvc;
using Athrna.Data;
using Athrna.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

namespace Athrna.Controllers
{
    public class SearchController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SearchController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string query)
        {
            if (string.IsNullOrEmpty(query))
            {
                return View(new SearchViewModel { Query = "", Results = new List<Site>() });
            }

            // Normalize the query
            query = query.ToLower().Trim();

            // Get the current language from cookie or default to English
            string currentLanguage = Request.Cookies["Athrna_Language"] ?? "en";

            // The source language is always English
            string sourceLanguage = "en";

            var results = new List<Site>();

            if (currentLanguage != sourceLanguage)
            {
                // First try: search for translations that match the query
                // This handles cases where user searches in a non-English language 
                var matchingTranslatedSiteIds = new HashSet<int>();

                // Look up in the translation table
                var matchingTranslations = await _context.Translation
                    .Where(t => t.TargetLanguage == currentLanguage &&
                                EF.Functions.Like(t.TranslatedText.ToLower(), $"%{query}%"))
                    .ToListAsync();

                // If we found translations matching the query
                if (matchingTranslations.Any())
                {
                    // Get the corresponding English texts
                    var originalTexts = matchingTranslations.Select(t => t.OriginalText.ToLower()).ToList();

                    // For each site, check if any of its fields match these English texts
                    var sitesToCheck = await _context.Site
                        .Include(s => s.City)
                        .Include(s => s.CulturalInfo)
                        .ToListAsync();

                    foreach (var site in sitesToCheck)
                    {
                        foreach (var originalText in originalTexts)
                        {
                            // Check each relevant field
                            if (site.Name?.ToLower().Contains(originalText) == true ||
                                site.Description?.ToLower().Contains(originalText) == true ||
                                site.City?.Name?.ToLower().Contains(originalText) == true ||
                                site.SiteType?.ToLower().Contains(originalText) == true)
                            {
                                matchingTranslatedSiteIds.Add(site.Id);
                                break; // Move to next site once we find a match
                            }
                        }
                    }

                    // Get the actual sites with their related data
                    if (matchingTranslatedSiteIds.Any())
                    {
                        var translatedSites = await _context.Site
                            .Include(s => s.City)
                            .Include(s => s.CulturalInfo)
                            .Where(s => matchingTranslatedSiteIds.Contains(s.Id))
                            .ToListAsync();

                        results.AddRange(translatedSites);
                    }
                }
            }

            // Always include direct matches - this covers both:
            // 1. English searches directly matching English content
            // 2. Any non-English content that might be directly in the database
            var directMatches = await _context.Site
                .Include(s => s.City)
                .Include(s => s.CulturalInfo)
                .Where(s => EF.Functions.Like(s.Name.ToLower(), $"%{query}%") ||
                           EF.Functions.Like(s.Description.ToLower(), $"%{query}%") ||
                           EF.Functions.Like(s.City.Name.ToLower(), $"%{query}%") ||
                           EF.Functions.Like(s.SiteType.ToLower(), $"%{query}%"))
                .ToListAsync();

            // Combine results but avoid duplicates by using Union
            results = results.Union(directMatches).ToList();

            var viewModel = new SearchViewModel
            {
                Query = query,
                Results = results
            };

            return View(viewModel);
        }
    }
}