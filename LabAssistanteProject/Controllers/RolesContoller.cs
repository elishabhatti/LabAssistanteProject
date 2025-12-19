using LabAssistanteProject.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Security.Claims;

namespace LabAssistanteProject.Controllers
{
    [Authorize]
    public class RolesController : Controller
    {
        private readonly MyAppContext _context;

        public RolesController(MyAppContext context)
        {
            _context = context;
        }

        // --- Helper: Redirect Logic ---
        private IActionResult RedirectToCorrectDashboard(string? currentRole)
        {
            return currentRole switch
            {
                "admin" => RedirectToAction("Admin"),
                "facility_head" => RedirectToAction("FacilityHead"),
                "assignee" => RedirectToAction("Assignee"),
                "enduser" => RedirectToAction("EndUser"),
                _ => RedirectToAction("Login", "Auth")
            };
        }

        // --- Facility Head Dashboard ---
        [HttpGet]
        public async Task<IActionResult> FacilityHead()
        {
            // Role Check
            var role = User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("Role");
            if (role != "facility_head") return RedirectToCorrectDashboard(role);

            ViewBag.username = User.Identity?.Name;
            ViewBag.userEmail = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email");
            ViewBag.userId = User.FindFirstValue("UserId");
            ViewBag.role = role;
            ViewBag.Message = TempData["Message"] as string;

            var allRequests = await _context.Requests
                .Include(r => r.Requestor)
                .Include(r => r.Facility)
                .Include(r => r.Assignee)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            ViewBag.Requests = allRequests;

            var assignees = await _context.Users
                .Where(u => u.Role == "Assignee")
                .OrderBy(u => u.Username)
                .ToListAsync();

            ViewBag.Assignees = assignees;
            var now = DateTime.Now;

            var monthlyRequests = _context.Requests
                .Where(r => r.CreatedAt.Month == now.Month
                         && r.CreatedAt.Year == now.Year)
                .ToList();

            ViewBag.MonthlyTotal = monthlyRequests.Count;

            ViewBag.MonthlyClosed = monthlyRequests
                .Count(r => r.Status == "Completed");

            ViewBag.MonthlyPending = monthlyRequests
                .Count(r => r.Status != "Completed");

            var completed = monthlyRequests
                .Where(r => r.Status == "Completed" && r.CompletedAt != null)
                .ToList();

            ViewBag.MonthlyAvgTime = completed.Any()
                ? completed.Average(r => (r.CompletedAt.Value - r.CreatedAt).TotalHours)
                : 0;


            return View("~/Views/Roles/FacilityHead.cshtml");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Assign(int requestId, int assigneeId)
        {
            var request = await _context.Requests.FindAsync(requestId);
            if (request == null)
            {
                TempData["Message"] = "Error: Request not found.";
                return RedirectToAction("FacilityHead");
            }

            string oldStatus = request.Status ?? "unassigned";
            string newStatus = "Work-In-Progress";
            var currentUserIdStr = User.FindFirstValue("UserId");
            int currentUserId = int.TryParse(currentUserIdStr, out int id) ? id : 0;

            request.AssigneeId = assigneeId;
            request.Status = newStatus;

            var historyEntry = new Models.History
            {
                RequestId = requestId,
                OldStatus = oldStatus,
                NewStatus = newStatus,
                UpdatedBy = currentUserId,
                UpdatedAt = DateTime.UtcNow
            };

            try
            {
                _context.Requests.Update(request);
                _context.History.Add(historyEntry);
                await _context.SaveChangesAsync();

                var assignee = await _context.Users.FindAsync(assigneeId);
                TempData["Message"] = $"Success: Assigned to {assignee?.Username} and history logged.";
            }
            catch (Exception ex)
            {
                TempData["Message"] = "Error saving assignment: " + ex.Message;
            }

            return RedirectToAction("FacilityHead");
        }

        // --- Admin Dashboard ---
        public async Task<IActionResult> Admin()
        {
            // Role Check
            var role = User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("Role");
            if (role != "admin") return RedirectToCorrectDashboard(role);

            ViewBag.username = User.Identity?.Name;
            ViewBag.TotalUsers = await _context.Users.CountAsync();
            ViewBag.TotalFacilities = await _context.Facilities.CountAsync();
            ViewBag.TotalRequests = await _context.Requests.CountAsync();

            ViewBag.AllUsers = await _context.Users.ToListAsync();
            ViewBag.AllFacilities = await _context.Facilities.ToListAsync();
            ViewBag.AllRequests = await _context.Requests
                .Include(r => r.Requestor)
                .Include(r => r.Facility)
                .Include(r => r.Assignee)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View("~/Views/Roles/Admin.cshtml");
        }

        // --- Assignee/Technician Dashboard ---
        public async Task<IActionResult> Assignee()
        {
            // Role Check
            var role = User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("Role");
            if (role != "assignee") return RedirectToCorrectDashboard(role);

            ViewBag.username = User.Identity?.Name;
            ViewBag.userId = User.FindFirstValue("UserId");
            ViewBag.role = role;
            ViewBag.Message = TempData["Message"] as string;

            if (int.TryParse(ViewBag.userId, out int loggedInUserId))
            {
                var myRequests = await _context.Requests
                    .Include(r => r.Requestor)
                    .Include(r => r.Facility)
                    .Where(r => r.AssigneeId == loggedInUserId)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync();

                ViewBag.WIPCount = myRequests.Count(r => r.Status == "Work-In-Progress");
                ViewBag.CompletedCount = myRequests.Count(r => r.Status == "Completed");
                ViewBag.MyRequests = myRequests;
            }

            return View("~/Views/Roles/Assignee.cshtml");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int requestId, string newStatus, string remarks)
        {
            var request = await _context.Requests.FindAsync(requestId);
            if (request == null)
            {
                TempData["Message"] = "Error: Request not found.";
                return RedirectToAction("Assignee");
            }

            string oldStatus = request.Status ?? "Unknown";
            var currentUserIdStr = User.FindFirstValue("UserId");
            int currentUserId = int.TryParse(currentUserIdStr, out int id) ? id : 0;

            request.Status = newStatus;
            request.Remarks = remarks;

            var historyEntry = new Models.History
            {
                RequestId = requestId,
                OldStatus = oldStatus,
                NewStatus = newStatus,
                UpdatedBy = currentUserId,
                UpdatedAt = DateTime.UtcNow
            };

            try
            {
                _context.Requests.Update(request);
                _context.History.Add(historyEntry);
                await _context.SaveChangesAsync();
                TempData["Message"] = "Success: Status and Remarks updated!";
            }
            catch (Exception ex)
            {
                TempData["Message"] = "Error: " + ex.Message;
            }

            return RedirectToAction("Assignee");
        }

        // --- EndUser Dashboard ---
        public async Task<IActionResult> EndUser()
        {
            // Role Check
            var role = User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("Role");
            if (role != "enduser") return RedirectToCorrectDashboard(role);

            ViewBag.username = User.Identity?.Name;
            ViewBag.userEmail = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email");
            ViewBag.userId = User.FindFirstValue("UserId");
            ViewBag.role = role;
            ViewBag.Message = TempData["Message"] as string;

            ViewBag.Facilities = await _context.Facilities.ToListAsync();

            return View("~/Views/Roles/EndUser.cshtml");
        }
        // --- Facility Management (Admin Only) ---

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateFacility(int id, string name, string description)
        {
            var facility = await _context.Facilities.FindAsync(id);
            if (facility == null) return NotFound();

            facility.Name = name;
            facility.Description = description;

            _context.Facilities.Update(facility);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Facility updated successfully!";
            return RedirectToAction("Admin");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFacility(int id)
        {
            var facility = await _context.Facilities.FindAsync(id);
            if (facility != null)
            {
                _context.Facilities.Remove(facility);
                await _context.SaveChangesAsync();
                TempData["Message"] = "Facility deleted successfully!";
            }
            return RedirectToAction("Admin");
        }
        [HttpPost]
        public IActionResult CompleteRequest(int requestId)
        {
            var request = _context.Requests.FirstOrDefault(r => r.Id == requestId);
            var role = User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("Role");

            if (request == null)
                return NotFound();

            request.Status = "Completed";
            request.CompletedAt = DateTime.UtcNow;   // ✅ MAIN LINE

            _context.SaveChanges();

            return RedirectToCorrectDashboard(role);
        }


        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}