using System.ComponentModel.DataAnnotations;

namespace YousefZuaianatAPI.Models
{
    public class Company
    {
        public int Id;
        [Required(ErrorMessage = "Company name is required")]
        [MaxLength(200)]
        public string Name { get; set; }

        [EmailAddress]
        public string? Email { get; set; }

        [Phone]
        public string? Phone { get; set; }

        [MaxLength(500)]
        public string? Address { get; set; }


        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public string? Description { get; set; }




    }
}
