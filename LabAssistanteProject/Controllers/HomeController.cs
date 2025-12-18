using LabAssistanteProject.Data;
using LabAssistanteProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Security.Claims;

namespace LabAssistanteProject.Controllers
{
    // Ensure all authenticated users are handled here, or redirect to Login
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly MyAppContext _context;

        public HomeController(ILogger<HomeController> logger, MyAppContext context)
        {
            _logger = logger;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            var userIdString = User.FindFirstValue("UserId");
            var username = User.Identity?.Name;
            var role = User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("Role");

            if (!int.TryParse(userIdString, out int requestorId))
            {
                return RedirectToAction("Login", "Auth");
            }

            // 2. Set ViewBag data 
            ViewBag.UserEmail = userEmail;
            ViewBag.username = username;
            ViewBag.userId = userIdString;
            ViewBag.role = role;

            ViewBag.Facilities = await _context.Facilities.ToListAsync();
            ViewBag.Message = TempData["Message"] as string;
                var userRequests = await _context.Requests
                    .Where(r => r.RequestorId == requestorId)
                    .Include(r => r.Facility)
                    .Include(r => r.Assignee)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync();

                ViewBag.Requests = userRequests;

            return View("~/Views/Home/Index.cshtml");
        }


        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(); 
        }
    }
}