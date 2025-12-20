using Microsoft.AspNetCore.Mvc;

namespace LabAssistanteProject.Controllers
{
    // 1. Inherit from Controller base class
    public class PagesController : Controller
    {
        // 2. Help Page Action
        [HttpGet]
        public IActionResult Help()
        {
            return View("~/Views/Pages/Help.cshtml");
        }
    }
}