using LabAssistanteProject.Data;
using LabAssistanteProject.Helpers;
using LabAssistanteProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace LabAssistanteProject.Controllers // Namespace sahi kar diya
{
    public class AuthController : Controller
    {
        private readonly JwtService _jwtService;
        private readonly MyAppContext _context;

        public AuthController(MyAppContext context, JwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;
        }

        // --- Role-Based Redirection Helper ---
        private IActionResult RedirectToRolePage(Users user)
        {
            // Database mein roles "Admin", "FacilityHead", etc hain, switch case handle kar lega
            var role = user.Role?.ToLowerInvariant() ?? "enduser";

            return role switch
            {
                "admin" => RedirectToAction("Admin", "Roles"),
                "facilityhead" => RedirectToAction("FacilityHead", "Roles"),
                "facility_head" => RedirectToAction("FacilityHead", "Roles"), // Handle both naming styles
                "assignee" => RedirectToAction("Assignee", "Roles"),
                "technician" => RedirectToAction("Assignee", "Roles"),
                "enduser" => RedirectToAction("EndUser", "Roles"),
                _ => RedirectToAction("Index", "Home")
            };
        }

        // ------------------ REGISTER ------------------
        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(Users user)
        {
            if (!ModelState.IsValid) return View(user);

            // 1. Password Hash
            user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);

            // 2. Save User
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // 3. Create JWT & Session
            var token = _jwtService.GenerateToken(user);
            var session = new Sessions
            {
                userId = user.Id,
                token = token,
                IsActive = true,
                CreatedAt = DateTime.Now,
                ExpiryDate = DateTime.Now.AddDays(7)
            };

            _context.Sessions.Add(session);
            await _context.SaveChangesAsync();

            // 4. Set Cookie
            AppendJwtCookie(token, session.ExpiryDate);

            return RedirectToRolePage(user);
        }

        // ------------------ LOGIN ------------------
        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password)
        {
            // 1. Find User
            var user = _context.Users.FirstOrDefault(u => u.Email == email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.Password))
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                return View();
            }

            // 2. Generate New Token
            var token = _jwtService.GenerateToken(user);

            // 3. Update or Create Session in DB
            var session = new Sessions
            {
                userId = user.Id,
                token = token,
                IsActive = true,
                CreatedAt = DateTime.Now,
                ExpiryDate = DateTime.Now.AddDays(7)
            };

            _context.Sessions.Add(session);
            await _context.SaveChangesAsync();

            // 4. Save Cookie
            AppendJwtCookie(token, session.ExpiryDate);

            // 5. ✅ Updated: Redirect to Role-specific Dashboard
            return RedirectToRolePage(user);
        }
        // --- Admin Registration GET ---
        [HttpGet]
        public IActionResult AdminRegister()
        {
            return View();
        }

        // --- Admin Registration POST ---
        // --- Admin Registration POST (Updated with Hashing, Session, and Cookies) ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminRegister(Users user, string SecretKey)
        {
            const string SYSTEM_SECRET = "LabAdmin@2025";

            // 1. Secret Key Check
            if (SecretKey != SYSTEM_SECRET)
            {
                TempData["Error"] = "Invalid Secret Key! Authorization Denied.";
                return View(user);
            }

            if (ModelState.IsValid)
            {
                // 2. Check if user already exists
                var existing = await _context.Users.AnyAsync(u => u.Username == user.Username || u.Email == user.Email);
                if (existing)
                {
                    TempData["Error"] = "User or Email already registered.";
                    return View(user);
                }

                // 3. ✅ Password Hashing
                user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);

                // 4. Save User
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // 5. ✅ Create JWT Token
                var token = _jwtService.GenerateToken(user);

                // 6. ✅ Create Session in DB
                var session = new Sessions
                {
                    userId = user.Id,
                    token = token,
                    IsActive = true,
                    CreatedAt = DateTime.Now,
                    ExpiryDate = DateTime.Now.AddDays(7)
                };

                _context.Sessions.Add(session);
                await _context.SaveChangesAsync();

                // 7. ✅ Append JWT Cookie
                AppendJwtCookie(token, session.ExpiryDate);

                // 8. ✅ Redirect to Admin/Role Dashboard automatically
                return RedirectToRolePage(user);
            }

            return View(user);
        }
        // GET: /Auth/ChangePassword
        [HttpGet]
        [Authorize] // Sirf logged in user access kar sake
        public IActionResult ChangePassword() => View();

        // POST: /Auth/ChangePassword
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string oldPassword, string newPassword, string confirmPassword)
        {
            // 1. Basic Validation
            if (newPassword != confirmPassword)
            {
                ModelState.AddModelError("", "New password and confirmation do not match.");
                return View();
            }

            // 2. Get Current User
            var email = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email");
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null) return RedirectToAction("Login");

            // 3. Verify Old Password
            if (!BCrypt.Net.BCrypt.Verify(oldPassword, user.Password))
            {
                ModelState.AddModelError("", "Current password is incorrect.");
                return View();
            }

            // 4. Hash & Save New Password
            user.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Password changed successfully!";

            // Role ke mutabiq dashboard pe wapis bhej dein
            return RedirectToRolePage(user);
        }

        // ------------------ LOGOUT ------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            Response.Cookies.Delete("jwt");
            return RedirectToAction("Login");
        }

        // --- Helper: Cookie Append ---
        private void AppendJwtCookie(string token, DateTime expiry)
        {
            Response.Cookies.Append("jwt", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = false, // HTTPS par true karein
                Expires = expiry,
                SameSite = SameSiteMode.Strict
            });
        }

    }
}