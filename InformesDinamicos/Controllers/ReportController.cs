using Microsoft.AspNetCore.Mvc;

namespace InformesDinamicos.Controllers
{
    public class ReportController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}