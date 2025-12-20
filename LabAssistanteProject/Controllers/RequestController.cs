// LabAssistanteProject.Controllers/RequestsController.cs
using LabAssistanteProject.Data;
using LabAssistanteProject.Models;
using LabAssistanteProject.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LabAssistanteProject.Controllers
{
    [Authorize]
    public class RequestsController : Controller
    {
        private readonly MyAppContext _context;
        private readonly EmailService _emailService;
        // Constructor
        public RequestsController(MyAppContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }
        // Post Reqquest
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

                // --- Send Email Notification ---
                var facilityHead = await _context.Users
                                    .FirstOrDefaultAsync(u => u.Role == "facility_head");
                if (facilityHead != null)
                {
                    string subject = "New Service Request Submitted";
                    string body = $"Hello {facilityHead.Username},<br>" +
                                  $"A new request has been submitted by {User.Identity?.Name} " +
                                  $"for {request.FacilityId} (Facility ID).<br>" +
                                  $"Please check your dashboard to assign it.";

                    // assuming you injected EmailService
                    _emailService.SendEmail(facilityHead.Email, subject, body);
                }

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
        // Delete Request
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userIdString = User.FindFirstValue("UserId");
            if (!int.TryParse(userIdString, out int userId)) return Unauthorized();

            var request = await _context.Requests
                .FirstOrDefaultAsync(r => r.Id == id && r.RequestorId == userId);

            if (request == null) return NotFound();

            // Security Check: Prevent deleting completed requests
            if (request.Status?.ToLower() == "completed")
            {
                TempData["Error"] = "Completed requests cannot be deleted.";
                return RedirectToAction("Index", "Home");
            }

            _context.Requests.Remove(request);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Request deleted successfully.";
            return RedirectToAction("Index", "Home");
        }
        // Edit Request Page
        [HttpGet]
        public async Task<IActionResult> EditRequest(int id)
        {
            var request = await _context.Requests.FindAsync(id);

            if (request == null || request.Status?.ToLower() == "completed")
            {
                return RedirectToAction("Index", "Home");
            }

            var facilities = await _context.Facilities.ToListAsync();
            ViewBag.FacilitiesSelectList = new SelectList(facilities, "Id", "Name", request.FacilityId);

            var severities = new List<string> { "Low", "Medium", "High", "Critical" };
            ViewBag.Severities = new SelectList(severities, request.Severity);

            return View("~/Views/Roles/EditRequest.cshtml", request);
        }


        // POST Edit Request
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditRequest(int id, [Bind("Id,FacilityId,Severity,Description")] Requests updatedReq)
        {
            if (id != updatedReq.Id) return NotFound();

            var existing = await _context.Requests.FindAsync(id);
            if (existing == null || existing.Status?.ToLower() == "completed") return BadRequest();

            existing.FacilityId = updatedReq.FacilityId;
            existing.Severity = updatedReq.Severity;
            existing.Description = updatedReq.Description;

            await _context.SaveChangesAsync();
            TempData["Message"] = "Request updated successfully!";
            return RedirectToAction("Index", "Home");
        }
    }
}