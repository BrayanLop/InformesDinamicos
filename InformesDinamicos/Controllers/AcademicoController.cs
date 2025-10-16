using Microsoft.AspNetCore.Mvc;

namespace InformesDinamicos.Controllers
{
    public class AcademicoController : Controller
    {
        public IActionResult Index()
        {
            return RedirectToAction("Nuevo", "Home");
        }
    }
}