using System.ComponentModel.DataAnnotations;

namespace MyRazorApp.Models
{
    public class Class
    {
        public int Id { get; set; }

        [Required]
        public string ClassName { get; set; } = string.Empty;

        public int StudentCount { get; set; }

        [Required]
        public string Description { get; set; } = string.Empty;

        public bool IsActive { get; set; }
    }
}
