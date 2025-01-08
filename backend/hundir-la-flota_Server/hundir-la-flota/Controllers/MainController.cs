using Microsoft.AspNetCore.Mvc;

namespace hundir_la_flota.Controllers
{
    public class MainController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
