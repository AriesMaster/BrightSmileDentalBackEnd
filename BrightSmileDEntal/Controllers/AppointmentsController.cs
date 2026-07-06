using Microsoft.AspNetCore.Mvc;

namespace BrightSmileDEntal.Controllers
{
    public class AppointmentsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
