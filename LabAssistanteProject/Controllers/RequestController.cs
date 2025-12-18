// LabAssistanteProject.Controllers/RequestsController.cs
using LabAssistanteProject.Data;
using LabAssistanteProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LabAssistanteProject.Controllers
{
    [Authorize]
    public class RequestsController : Controller
    {
        private readonly MyAppContext _context;

        public RequestsController(MyAppContext context)
        {
            _context = context;
        }
        [HttpPost]
        public async Task<IActionResult> Create(Requests request)
        {
            var requestorIdString = User.FindFirstValue("UserId");
            if (!int.TryParse(requestorIdString, out int requestorId))
            {
                return Unauthorized();
            }

            request.RequestorId = requestorId;
            request.CreatedAt = DateTime.UtcNow;
            request.Status = "unassigned";

            if (!ModelState.IsValid)
            {
                ViewBag.Facilities = await _context.Facilities.ToListAsync(); 
                ViewBag.username = User.Identity?.Name;
                ViewBag.role = User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("Role");
                ViewBag.userId = requestorId;
                return View("~/Views/Roles/EndUser.cshtml", request);
            }

            try
            {
                _context.Requests.Add(request);
                await _context.SaveChangesAsync();

                TempData["Message"] = $"Service Request submitted successfully!";
            }
            catch (Exception ex)
            {
                string errorMessage = "An error occurred while saving the request.";
                if (ex.InnerException != null)
                {
                    errorMessage += " Details: " + ex.InnerException.Message;
                }
                else
                {
                    errorMessage += " Details: " + ex.Message;
                }
                TempData["Message"] = errorMessage;
                return RedirectToAction("Index", "Home");
            }

            return RedirectToAction("Index", "Home");
        }
    }
}