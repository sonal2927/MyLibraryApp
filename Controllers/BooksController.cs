// BooksController.cs
using LibraryManagementSystem.Models;
using LibraryManagementSystem.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.IO;

namespace LibraryManagementSystem.Controllers
{
    public class BooksController : Controller
    {
        private readonly LibraryDbContext _context;

        public BooksController(LibraryDbContext context)
        {
            _context = context;
        }

        public IActionResult Index(string department = "All", string search = "")
        {
            var books = _context.Books.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                books = books.Where(b => b.Title.Contains(search) || b.Author.Contains(search));

            if (!string.IsNullOrWhiteSpace(department) && department != "All")
                books = books.Where(b => b.Department == department);

            return View(books.ToList());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> IssueBook(int bookId)
        {
            var role = HttpContext.Session.GetString("UserRole");
            var libraryId = HttpContext.Session.GetString("LibraryId");

            if (string.IsNullOrWhiteSpace(role) || string.IsNullOrWhiteSpace(libraryId))
                return RedirectToAction("Login", "Account");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.LibraryId == libraryId);
            if (user == null) return RedirectToAction("Login", "Account");

            var book = await _context.Books.FirstOrDefaultAsync(b => b.Id == bookId);
            if (book == null || !book.IsAvailable)
            {
                TempData["Error"] = "Book is not available.";
                return RedirectToAction("Index");
            }

            var record = new BookRecord
            {
                BookId = bookId,
                LibraryId = user.LibraryId!,
                Status = BookStatus.Issued,
                IssuedAt = DateTime.Now,
                DueAt = DateTime.Now.AddDays(7),
                IsSubmitted = false
            };

            book.IsAvailable = false;
            _context.BookRecords.Add(record);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Book issued successfully.";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Details(int id)
        {
            var book = await _context.Books.FirstOrDefaultAsync(b => b.Id == id);
            if (book == null)
                return NotFound();

            return View(book);
        }

        [HttpGet]
        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Book book, IFormFile imageFile)
        {
            if (!ModelState.IsValid) return View(book);

            if (imageFile != null && imageFile.Length > 0)
            {
                var fileName = Path.GetFileName(imageFile.FileName);
                var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/books/", fileName);

                using (var stream = new FileStream(path, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                book.ImageList = new List<string> { fileName };
            }

            _context.Books.Add(book);
            await _context.SaveChangesAsync();

            TempData["Success"] = "✅ Book added successfully!";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var book = await _context.Books.FindAsync(id);
            if (book == null) return NotFound();
            return View(book);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Book book)
        {
            if (id != book.Id) return NotFound();
            if (!ModelState.IsValid) return View(book);

            try
            {
                _context.Update(book);
                await _context.SaveChangesAsync();
                TempData["Success"] = "✅ Book updated successfully!";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Books.Any(e => e.Id == book.Id))
                    return NotFound();
                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var book = await _context.Books.FirstOrDefaultAsync(m => m.Id == id);
            if (book == null) return NotFound();

            return View(book);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var book = await _context.Books.FindAsync(id);
            if (book != null)
            {
                _context.Books.Remove(book);
                await _context.SaveChangesAsync();
                TempData["Success"] = "🗑️ Book deleted successfully!";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult Search(string title, string author, string category, string isbn)
        {
            var books = _context.Books.AsQueryable();

            if (!string.IsNullOrEmpty(title))
                books = books.Where(b => b.Title.Contains(title));
            if (!string.IsNullOrEmpty(author))
                books = books.Where(b => b.Author.Contains(author));
            if (!string.IsNullOrEmpty(category))
                books = books.Where(b => b.Department.Contains(category));
            if (!string.IsNullOrEmpty(isbn))
               books = books.Where(b => !string.IsNullOrEmpty(b.ISBN) && b.ISBN.Contains(isbn));


            return View(books.ToList());
        }

        [HttpGet]
        public IActionResult IssuedBooks()
        {
            string? libraryId = HttpContext.Session.GetString("LibraryId");
            if (string.IsNullOrEmpty(libraryId))
                return RedirectToAction("Login", "Account");

            var issuedRecords = _context.BookRecords
                .Where(r => r.LibraryId == libraryId && r.ReturnedAt == null)
                .Include(r => r.Book)
                .ToList();

            return View(issuedRecords);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RequestReturn(int recordId)
        {
            var record = _context.BookRecords.FirstOrDefault(r => r.Id == recordId);
            if (record != null && record.ReturnedAt == null)
            {
                record.Status = BookStatus.ReturnRequested;
                _context.SaveChanges();
                TempData["Success"] = "📩 Return request sent to librarian.";
            }
            return RedirectToAction("IssuedBooks");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RequestRenewal(int recordId)
        {
            var record = _context.BookRecords.FirstOrDefault(r => r.Id == recordId);
            if (record != null && record.DueAt.HasValue && record.Status == BookStatus.Issued)
            {
                record.Status = BookStatus.RenewalRequested;
                _context.SaveChanges();
                TempData["Success"] = "🔁 Renewal request sent to librarian.";
            }
            return RedirectToAction("IssuedBooks");
        }

        [HttpGet]
        public async Task<IActionResult> RequestIssue(int id)
        {
            var libraryId = HttpContext.Session.GetString("LibraryId");
            if (string.IsNullOrEmpty(libraryId))
                return RedirectToAction("Login", "Account");

            var book = await _context.Books.FindAsync(id);
            if (book == null) return NotFound();

            bool alreadyRequested = _context.BookRecords.Any(r =>
                r.BookId == id && r.LibraryId == libraryId && r.Status == BookStatus.Pending);

            if (alreadyRequested)
            {
                TempData["Error"] = "You already have a pending request for this book.";
                return RedirectToAction("Index");
            }

            var request = new BookRecord
            {
                BookId = id,
                LibraryId = libraryId,
                RequestedAt = DateTime.UtcNow,
                Status = BookStatus.Pending
            };

            _context.BookRecords.Add(request);
            await _context.SaveChangesAsync();

            TempData["Success"] = "✅ Book request submitted successfully!";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult ApproveRequests()
        {
            var requests = _context.BookRecords
                .Where(r => r.Status == BookStatus.Pending)
                .Include(r => r.Book)
                .ToList();

            return View(requests);
        }

        [HttpPost]
        public IActionResult ApproveRequest(int id)
        {
            var request = _context.BookRecords.FirstOrDefault(r => r.Id == id);
            if (request != null)
            {
                var book = _context.Books.FirstOrDefault(b => b.Id == request.BookId);
                if (book != null)
                {
                    request.Status = BookStatus.Approved;
                    request.ApprovedAt = DateTime.UtcNow;
                    request.IssuedAt = DateTime.UtcNow;
                    request.DueAt = DateTime.UtcNow.AddDays(7);
                    book.IsAvailable = false;

                    _context.SaveChanges();
                }
                else
                {
                    TempData["Error"] = "Book not found.";
                }
            }

            return RedirectToAction("ApproveRequests");
        }

        [HttpPost]
        public IActionResult RejectRequest(int id)
        {
            var request = _context.BookRecords.FirstOrDefault(r => r.Id == id);
            if (request != null)
            {
                request.Status = BookStatus.Rejected;
                _context.SaveChanges();
            }
            return RedirectToAction("ApproveRequests");
        }

        [HttpGet]
        public IActionResult StudentDashboard()
        {
            string? id = HttpContext.Session.GetString("LibraryId");
            if (string.IsNullOrEmpty(id))
                return RedirectToAction("Login", "Account");

            var student = _context.Users.FirstOrDefault(u => u.Role == "Student" && u.LibraryId == id);
            if (student == null)
                return RedirectToAction("Login", "Account");

            var issuedBooks = _context.BookRecords
                .Where(r => r.LibraryId == id && r.IssuedAt != null && r.ReturnedAt == null)
                .Include(r => r.Book)
                .ToList();

            var featuredBooks = _context.Books
                .Where(b => b.IsAvailable)
                .OrderBy(b => Guid.NewGuid())
                .Take(6)
                .ToList();

            var notifications = new List<string>
            {
                "📢 You have 2 books due in 3 days.",
                "📘 New books added in Science section."
            };

            var model = new DashboardViewModel
            {
                User = student,
                BookRequests = issuedBooks,
                FeaturedBooks = featuredBooks,
                Notifications = notifications
            };

            return View(model);
        }

        public IActionResult FacultyDashboard()
        {
            string? id = HttpContext.Session.GetString("LibraryId");
            var faculty = _context.Users.FirstOrDefault(u => u.Role == "Faculty" && u.LibraryId == id);

            if (faculty == null)
                return RedirectToAction("Login", "Account");

            var issuedBooks = _context.BookRecords
                .Where(r => r.LibraryId == id && r.IssuedAt != null && r.ReturnedAt == null)
                .Include(r => r.Book)
                .ToList();

            var trackedRequests = _context.BookRequests
                .OrderByDescending(r => r.RequestDate)
                .ToList();

            var categoryBooks = _context.Books
                .OrderBy(b => b.Department)
                .Take(10)
                .ToList();

            var notices = new List<string>
            {
                "📢 New books available in Research section.",
                "🛑 Library closed this Sunday."
            };

            var model = new FacultyDashboardViewModel
            {
                Faculty = faculty,
                IssuedBooks = issuedBooks,
                TrackedRequests = trackedRequests,
                CategoryWiseBooks = categoryBooks,
                LibraryNotices = notices
            };

            return View(model);
        }


        [HttpGet]
        public IActionResult MyRequests()
        {
            var libraryId = HttpContext.Session.GetString("LibraryId");
            if (string.IsNullOrEmpty(libraryId))
                return RedirectToAction("Login", "Account");

            var bookRequests = _context.BookRequests
                .Where(r => r.StudentId == libraryId)
                .OrderByDescending(r => r.RequestDate)
                .ToList();

            return View(bookRequests); // ✅ correct type: List<BookRequest>
        }


        [HttpGet]
        public IActionResult LibrarianDashboard()
        {
            string? id = HttpContext.Session.GetString("LibraryId");
            var librarian = _context.Users.FirstOrDefault(u => u.Role == "Librarian" && u.LibraryId == id);
            return librarian == null ? RedirectToAction("Login") : View(librarian);
        }

        [HttpGet]
        public IActionResult StudentBooks(string? category, string? search)
        {
            var books = _context.Books.AsQueryable();

            if (!string.IsNullOrEmpty(category))
                books = books.Where(b => b.Department == category);

            if (!string.IsNullOrEmpty(search))
                books = books.Where(b => b.Title.Contains(search) || b.Author.Contains(search));

            return View("StudentBooks", books.ToList());
        }

        [HttpGet]
        public IActionResult RequestBook()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RequestBook(BookRequest model)
        {
            if (ModelState.IsValid)
            {
                model.RequestDate = DateTime.Now;
                model.Status = "Pending";
                model.StudentId = HttpContext.Session.GetString("LibraryId") ?? "unknown";

                _context.BookRequests.Add(model);
                _context.SaveChanges();

                TempData["Success"] = "✅ Your book request has been submitted!";
                return RedirectToAction(nameof(RequestBook));
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult MyBookRequests()
        {
            var studentId = HttpContext.Session.GetString("LibraryId");
            if (string.IsNullOrEmpty(studentId))
                return RedirectToAction("Login", "Account");

            var requests = _context.BookRequests
                .Where(r => r.StudentId == studentId)
                .OrderByDescending(r => r.RequestDate)
                .ToList();

            return View(requests);
        }

        public IActionResult ReadingHistory()
        {
            var studentId = HttpContext.Session.GetString("LibraryId");

            var history = _context.BookRecords
                .Include(br => br.Book)
                .Where(br => br.LibraryId == studentId && br.IssuedAt != null && br.ReturnedAt != null)
                .OrderByDescending(br => br.ReturnedAt)
                .ToList();

            return View("ReadingHistory", history);
        }

        [HttpGet("/student/profile")]
        public IActionResult Profile()
        {
            var libraryId = HttpContext.Session.GetString("LibraryId");
            if (string.IsNullOrEmpty(libraryId))
                return RedirectToAction("Login", "Account");

            var user = _context.Users.FirstOrDefault(u => u.LibraryId == libraryId);
            if (user == null) return RedirectToAction("Login", "Account");

            return View("Profile", user);
        }

        [HttpPost("/student/profile")]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateProfile(User updatedUser)
        {
            if (!ModelState.IsValid)
                return View("Profile", updatedUser);

            var existingUser = _context.Users.FirstOrDefault(u => u.LibraryId == updatedUser.LibraryId);
            if (existingUser == null) return NotFound();

            existingUser.Email = updatedUser.Email;
            existingUser.Contact = updatedUser.Contact;
            existingUser.Address = updatedUser.Address;

            _context.SaveChanges();
            TempData["Success"] = "✅ Your profile has been updated successfully.";

            return RedirectToAction("Profile");
        }

    }
}
