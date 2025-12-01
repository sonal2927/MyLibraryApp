using System;
using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.Models
{
    public class User
    {
        public int Id { get; set; }

        // 📌 Required Fields
        [Required]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Phone { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        [Required]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required]
        public string Gender { get; set; } = string.Empty;

        [Required]
        public string Department { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = string.Empty;

        [Required]
        public string EnrollmentNumber { get; set; } = string.Empty;
        

        // ✅ New Field: Shown only for students
        public string? Year { get; set; }

        public string? Semester { get; set; }

        // 🛠️ Optional / System Fields
        public bool IsApproved { get; set; }

        public string? LibraryId { get; set; }

        public DateTime RegisteredAt { get; set; }

        public DateTime? ApprovedAt { get; set; }

        public string? Contact { get; set; }

        public string? Address { get; set; }

        public DateTime? DOB { get; set; }

        public string? PasswordHash { get; set; }
    }
}
