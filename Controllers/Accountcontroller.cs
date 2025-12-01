// AccountController.cs
using LibraryManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;

namespace LibraryManagementSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly LibraryDbContext _context;

        public AccountController(LibraryDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public IActionResult Login(string role, string libraryId, string password)
        {
            if (string.IsNullOrWhiteSpace(role) || string.IsNullOrWhiteSpace(libraryId) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Please enter all fields.";
                return View();
            }

            role = role.Trim();
            libraryId = libraryId.Trim();
            password = password.Trim();

            if (role.Equals("Admin", StringComparison.OrdinalIgnoreCase) &&
                libraryId == "admin" && password == "admin")
            {
                HttpContext.Session.SetString("UserRole", "Admin");
                HttpContext.Session.SetString("LibraryId", "admin");
                TempData["Success"] = "✅ Admin login successful!";
                return RedirectToAction("UserInfo");
            }

            var user = _context.Users
                .AsEnumerable()
                .FirstOrDefault(u =>
                    u.Role.Equals(role, StringComparison.OrdinalIgnoreCase) &&
                    u.LibraryId == libraryId &&
                    u.Password == password &&
                    u.IsApproved);

            if (user != null)
            {
                HttpContext.Session.SetString("UserRole", user.Role);
                HttpContext.Session.SetString("LibraryId", user.LibraryId ?? string.Empty);
                TempData["Success"] = $"✅ Login successful! Welcome {user.FullName}";

                return user.Role switch
                {
                    "Student" => RedirectToAction("StudentDashboard", "Books"),
                    "Faculty" => RedirectToAction("FacultyDashboard", "Books"),
                    "Librarian" => RedirectToAction("LibrarianDashboard", "Books"),
                    _ => RedirectToAction("Login")
                };
            }

            ViewBag.Error = "Invalid credentials or not approved by admin.";
            return View();
        }

        [HttpPost]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            TempData["Success"] = "✅ You have been logged out successfully.";
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(User user)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Error = "Please fill all required fields.";
                return View(user);
            }

            if (user.Password != user.ConfirmPassword)
            {
                ViewBag.Error = "Passwords do not match.";
                return View(user);
            }

            bool userExists = _context.Users.Any(u => u.Email == user.Email || u.Phone == user.Phone);
            if (userExists)
            {
                ViewBag.Error = "A user with this email or phone already exists.";
                return View(user);
            }

            user.IsApproved = false;
            user.LibraryId = null;
            user.RegisteredAt = DateTime.UtcNow;

            _context.Users.Add(user);
            _context.SaveChanges();

            TempData["Success"] = "✅ Registration submitted. Please wait for admin approval.";
            return RedirectToAction("Login");
        }

 

        [HttpGet]
        public IActionResult AdminApproval()
        {
            ViewBag.Students = _context.Users.Where(u => !u.IsApproved && u.Role == "Student").OrderByDescending(u => u.RegisteredAt).ToList();
            ViewBag.Faculty = _context.Users.Where(u => !u.IsApproved && u.Role == "Faculty").OrderByDescending(u => u.RegisteredAt).ToList();
            ViewBag.Librarians = _context.Users.Where(u => !u.IsApproved && u.Role == "Librarian").OrderByDescending(u => u.RegisteredAt).ToList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ApproveUser(int id)
        {
            var user = _context.Users.FirstOrDefault(u => u.Id == id);
            if (user != null && !user.IsApproved)
            {
                user.IsApproved = true;
                user.ApprovedAt = DateTime.UtcNow;
                user.LibraryId = GenerateLibraryId(user.Role);
                _context.SaveChanges();
                TempData["Success"] = $"✅ {user.FullName} approved with Library ID: {user.LibraryId}";
            }
            return RedirectToAction("AdminApproval");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteUserRequest(int id)
        {
            var user = _context.Users.FirstOrDefault(u => u.Id == id && !u.IsApproved);
            if (user != null)
            {
                _context.Users.Remove(user);
                _context.SaveChanges();
                TempData["Success"] = "❌ User request deleted successfully.";
            }
            return RedirectToAction("AdminApproval");
        }

        [HttpGet]
        public IActionResult UserInfo(string role = "All", string search = "")
        {
            var query = _context.Users.AsQueryable();
            if (role != "All") query = query.Where(u => u.Role == role);
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(u =>
                    u.FullName.Contains(search) ||
                    u.Email.Contains(search) ||
                    (u.LibraryId != null && u.LibraryId.Contains(search)));
            }
            ViewBag.SelectedRole = role;
            ViewBag.SearchQuery = search;
            return View(query.ToList());
        }

        [HttpGet]
        public IActionResult EditUser(string id)
        {
            var user = _context.Users.FirstOrDefault(u => u.LibraryId == id);
            return user == null ? NotFound() : View(user);
        }

        [HttpPost]
        public IActionResult EditUser(User updatedUser)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Error = "Please fill all required fields.";
                return View(updatedUser);
            }
            var user = _context.Users.FirstOrDefault(u => u.LibraryId == updatedUser.LibraryId);
            if (user == null) return NotFound();
            user.FullName = updatedUser.FullName;
            user.Email = updatedUser.Email;
            user.Phone = updatedUser.Phone;
            user.Gender = updatedUser.Gender;
            user.Department = updatedUser.Department;
            _context.SaveChanges();
            TempData["Success"] = "✅ User updated successfully!";
            return RedirectToAction("UserInfo");
        }

        [HttpGet]
        public IActionResult UserDetails(string id)
        {
            var user = _context.Users.FirstOrDefault(u => u.LibraryId == id);
            if (user == null) return NotFound();
            ViewBag.Books = new List<BookRecord>(); // Replace later with actual records
            return View(user);
        }

        private string GenerateLibraryId(string role)
        {
            var random = new Random().Next(1000, 9999);
            return $"LIB-2025-{role.ToUpper()[0..3]}-{random}";
        }
    }
}