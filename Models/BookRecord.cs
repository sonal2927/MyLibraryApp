using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryManagementSystem.Models
{
    public class BookRecord
    {
        [Key]
        public int Id { get; set; }

        // Foreign Key to Book
        [Required]
        public int BookId { get; set; }

        [ForeignKey("BookId")]
        [Required]
        public Book Book { get; set; } = null!; // Fixed CS8618

        // Foreign Key to User (by LibraryId)
        [Required]
        public string LibraryId { get; set; } = string.Empty; // Fixed CS8618

        // Tracking timestamps
        public DateTime? RequestedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public DateTime? IssuedAt { get; set; }
        public DateTime? DueAt { get; set; }
        public DateTime? ReturnedAt { get; set; }

        // Request/issue status
        public BookStatus Status { get; set; }

        public bool IsSubmitted { get; set; }  // ✅ True if user has returned book physically

        // Feedback (optional)
        public int? Rating { get; set; }  // ⭐ User rating after reading
    }
}
