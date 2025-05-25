using Microsoft.AspNetCore.Mvc;

namespace MyApp.Models
{
    public class CalendarController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
