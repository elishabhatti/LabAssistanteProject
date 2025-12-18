// Controllers/RolesController.cs
using LabAssistanteProject.Data; // MyAppContext ke liye
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // Include() aur ToListAsync() ke liye
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks; // async/await ke liye

namespace LabAssistanteProject.Controllers
{
    [Authorize]
    public class RolesController : Controller
    {
        private readonly MyAppContext _context;

        public RolesController(MyAppContext context) // DbContext Injection
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> FacilityHead()
        {
            // --- 1. Basic User Info ---
            ViewBag.username = User.Identity?.Name;
            ViewBag.userEmail = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email");
            ViewBag.userId = User.FindFirstValue("UserId");
            ViewBag.role = User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("Role");

            // Success/Error message display
            ViewBag.Message = TempData["Message"] as string;

            // --- 2. Fetch Requests (End User created) ---
            // Fetch all requests, including details about who created it, the facility, and the assigned user.
            var allRequests = await _context.Requests
                .Include(r => r.Requestor) // Request banane wale ka detail (User)
                .Include(r => r.Facility)  // Facility detail
                .Include(r => r.Assignee)  // Assignee ka detail
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            ViewBag.Requests = allRequests;
            // 

            // --- 3. Fetch Assignees (Users who can resolve requests) ---
            // Fetch users whose role is 'Assignee' or 'Technician' (as potential resolvers)
            var assignees = await _context.Users
                .Where(u => u.Role == "Assignee" || u.Role == "Technician")
                .OrderBy(u => u.Username)
                .ToListAsync();

            ViewBag.Assignees = assignees;

            return View("~/Views/Roles/FacilityHead.cshtml");
        }

        // --- Assignment Logic (POST) ---
        // (Is logic ko hum nahi badal rahe, yeh assign karne ke liye use hoga)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Assign(int requestId, int assigneeId)
        {
            // 1. Request dhundo (Purana status janne ke liye)
            var request = await _context.Requests.FindAsync(requestId);

            if (request == null)
            {
                TempData["Message"] = "Error: Request not found.";
                return RedirectToAction("FacilityHead");
            }

            // --- 2. History ke liye purana data save karein ---
            string oldStatus = request.Status ?? "unassigned";
            string newStatus = "Work-In-Progress";

            // Logged-in User (Facility Head) ki ID nikalna
            var currentUserIdStr = User.FindFirstValue("UserId");
            int currentUserId = int.TryParse(currentUserIdStr, out int id) ? id : 0;

            // --- 3. Update Request ---
            request.AssigneeId = assigneeId;
            request.Status = newStatus;

            // --- 4. Create History Record ---
            var historyEntry = new Models.History
            {
                RequestId = requestId,
                OldStatus = oldStatus,
                NewStatus = newStatus,
                UpdatedBy = currentUserId, // Jisne assign kiya
                UpdatedAt = DateTime.UtcNow
            };

            try
            {
                _context.Requests.Update(request);
                _context.History.Add(historyEntry); // History table mein insert

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


        public async Task<IActionResult> Admin()
        {
            ViewBag.username = User.Identity?.Name;

            // Fetch all counts for the summary cards
            ViewBag.TotalUsers = await _context.Users.CountAsync();
            ViewBag.TotalFacilities = await _context.Facilities.CountAsync();
            ViewBag.TotalRequests = await _context.Requests.CountAsync();

            // Fetch lists for the tabs
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
        // --- Assignee Dashboard (GET) ---
        public async Task<IActionResult> Assignee()
        {
            ViewBag.username = User.Identity?.Name;
            ViewBag.userId = User.FindFirstValue("UserId");
            ViewBag.role = User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("Role");
            ViewBag.Message = TempData["Message"] as string;

            if (int.TryParse(ViewBag.userId, out int loggedInUserId))
            {
                // Sirf wahi requests layein jo is specific assignee ko assign hain
                var myRequests = await _context.Requests
                    .Include(r => r.Requestor)
                    .Include(r => r.Facility)
                    .Where(r => r.AssigneeId == loggedInUserId)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync();

                ViewBag.MyRequests = myRequests;
            }

            return View("~/Views/Roles/Assignee.cshtml");
        }

        // --- Status & Remarks Update (POST) ---
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

            string oldStatus = request.Status;
            var currentUserIdStr = User.FindFirstValue("UserId");
            int currentUserId = int.TryParse(currentUserIdStr, out int id) ? id : 0;

            // 1. Update Request table
            request.Status = newStatus;
            request.Remarks = remarks; // Aapka "Answer" yahan save ho raha hai

            // 2. Add to History table
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


        public async Task<IActionResult> EndUser() // async banayein kyunke facilities fetch karni hain
        {
            // 1. User Info set karein (Claims se)
            ViewBag.username = User.Identity?.Name;
            ViewBag.userEmail = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email");
            ViewBag.userId = User.FindFirstValue("UserId");
            ViewBag.role = User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("Role");

            // 2. TempData message set karein (Success/Error ke liye)
            ViewBag.Message = TempData["Message"] as string;

            // 3. Dropdown ke liye Facilities fetch karein
            ViewBag.Facilities = await _context.Facilities.ToListAsync();

            return View("~/Views/Roles/EndUser.cshtml");
        }

        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}