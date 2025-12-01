using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace LibraryManagementSystem.Models
{
    public class Book
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Author { get; set; } = string.Empty;

        [Required]
        public string Department { get; set; } = string.Empty;

        public bool IsAvailable { get; set; } = true;

        public string SerializedImages { get; set; } = "[]";

        public string? ISBN { get; set; }

        // Navigation property for BookRecords (for relationships)
        public ICollection<BookRecord>? BookRecords { get; set; }

        // NotMapped Properties
        [NotMapped]
        public List<string> ImageList
        {
            get
            {
                try
                {
                    return string.IsNullOrWhiteSpace(SerializedImages)
                        ? new List<string>()
                        : JsonSerializer.Deserialize<List<string>>(SerializedImages) ?? new List<string>();
                }
                catch
                {
                    return new List<string>();
                }
            }
            set
            {
                SerializedImages = JsonSerializer.Serialize(value ?? new List<string>());
            }
        }

        [NotMapped]
        public string ImagePath =>
            ImageList.FirstOrDefault() ?? "/images/books/default-book.png";
    }
}
