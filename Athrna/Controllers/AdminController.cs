using Microsoft.AspNetCore.Mvc;

namespace Athrna.Controllers
{
    public class AdminController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
