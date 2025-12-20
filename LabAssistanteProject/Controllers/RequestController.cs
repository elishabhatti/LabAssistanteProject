using LabAssistanteProject.Data;
using LabAssistanteProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Resend;
using System.Security.Claims;

namespace LabAssistanteProject.Controllers
{
    [Authorize]
    public class RequestsController : Controller
    {
        private readonly MyAppContext _context;
        private readonly IResend _resend;

        public RequestsController(
            MyAppContext context, IResend resend)
        {
            _context = context;
            _resend = resend;
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
                return View("~/Views/Roles/EndUser.cshtml", request);
            }

            try
            {
                _context.Requests.Add(request);
                await _context.SaveChangesAsync();

                // 1. Get Facility Heads (Email + Name filter)
                var facilityHeads = await _context.Users
                    .Where(u => u.Role == "facility_head" && u.Email != null)
                    .Select(u => new { u.Email, u.Username }) // Fetching both email and name
                    .ToListAsync();

                if (facilityHeads.Any())
                {
                    var message = new EmailMessage();
                    message.From = "onboarding@resend.dev"; 
                    message.Subject = "New Request Submitted";

                    // Professional HTML Body
                    message.HtmlBody = $@"
                <div style='font-family: Arial, sans-serif; border: 1px solid #eee; padding: 20px; border-radius: 10px;'>
                    <h2 style='color: #2c3e50;'>New Service Request Received</h2>
                    <p>Dear Facility Head,</p>
                    <p>A new maintenance request has been submitted in the <strong>LabAssist OHD</strong> system.</p>
                    
                    <table style='width: 100%; background: #f9f9f9; padding: 15px; border-radius: 5px;'>
                        <tr><td><strong>Requestor:</strong></td><td>{User.Identity?.Name}</td></tr>
                        <tr><td><strong>Facility ID:</strong></td><td>{request.FacilityId}</td></tr>
                        <tr><td><strong>Priority:</strong></td><td><span style='color: red;'>{request.Severity}</span></td></tr>
                        <tr><td><strong>Date:</strong></td><td>{request.CreatedAt:f}</td></tr>
                    </table>

                    <p style='margin-top: 20px;'><strong>Description:</strong><br/>
                    {request.Description}</p>

                    <div style='margin-top: 30px;'>
                        <a href='http://localhost:5030/Roles/FacilityHead' 
                           style='background-color: #3498db; color: white; padding: 12px 25px; text-decoration: none; border-radius: 5px;'>
                           Open Admin Dashboard
                        </a>
                    </div>
                    
                    <hr style='border: 0; border-top: 1px solid #eee; margin-top: 40px;' />
                    <p style='font-size: 12px; color: #7f8c8d;'>This is an automated notification from LabAssist Assistant Project.</p>
                </div>";

                    foreach (var head in facilityHeads)
                    {
                        message.To.Add(head.Email!);
                    }

                    await _resend.EmailSendAsync(message);
                }

                TempData["Message"] = "Request successfully created! Facility Heads have been notified via email.";
            }
            catch (Exception ex)
            {
                TempData["Message"] = "Request saved, but email failed: " + ex.Message;
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