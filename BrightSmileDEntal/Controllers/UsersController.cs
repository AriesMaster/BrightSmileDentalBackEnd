using Microsoft.AspNetCore.Mvc;

namespace BrightSmileDEntal.Controllers
{
    public class UsersController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
