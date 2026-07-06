using Microsoft.AspNetCore.Mvc;

namespace BrightSmileDEntal.Controllers
{
    public class ProfilesController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
