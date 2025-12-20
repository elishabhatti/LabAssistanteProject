using LabAssistanteProject.Data;
using LabAssistanteProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Security.Claims;

namespace LabAssistanteProject.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly MyAppContext _context;
        // Role-Based Redirection
        private IActionResult RedirectToCorrectDashboard(string? currentRole)
        {
            return currentRole switch
            {
                "admin" => RedirectToAction("Admin", "Roles"),
                "facility_head" => RedirectToAction("FacilityHead", "Roles"),
                "assignee" => RedirectToAction("Assignee", "Roles"),
                "enduser" => RedirectToAction("EndUser", "Roles"),
                _ => RedirectToAction("Login", "Auth")
            };
        }
        // Constructor
        public HomeController(ILogger<HomeController> logger, MyAppContext context)
        {
            _logger = logger;
            _context = context;
        }
        // Getting Home Page
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            var userIdString = User.FindFirstValue("UserId");
            var username = User.Identity?.Name;
            var role = User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("Role");
            if (role != "enduser") return RedirectToCorrectDashboard(role);

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

        // Getting Privacy Page
        public IActionResult Privacy()
        {
            return View();
        }
        // Getting Error Page
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }
    }
}