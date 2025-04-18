using Microsoft.AspNetCore.Mvc;
using Athrna.Data;
using Athrna.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Linq;

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
            query = query.ToLower();

            // Search sites by name, description, or city name
            var results = await _context.Site
                .Include(s => s.City)
                .Include(s => s.CulturalInfo)
                .Where(s => s.Name.ToLower().Contains(query) ||
                           s.Description.ToLower().Contains(query) ||
                           s.City.Name.ToLower().Contains(query) ||
                           s.SiteType.ToLower().Contains(query))
                .ToListAsync();

            var viewModel = new SearchViewModel
            {
                Query = query,
                Results = results
            };

            return View(viewModel);
        }
    }
}