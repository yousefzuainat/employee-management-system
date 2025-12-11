using System.ComponentModel.DataAnnotations;
using System.Data;
using YousefZuaianatAPI.Models.Enum;

namespace YousefZuaianatAPI.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [StringLength(100)]
        public string Name { get; set; }

        [EmailAddress]
        [Required]
        public string Email { get; set; }

        [Required]
        public string PasswordHash { get; set; }


        public int? DepartmentId { get; set; }
        public Department? Department { get; set; }

        public int? ManagerId { get; set; }
        public User? Manager { get; set; }

        public Role Role { get; set; }

        public decimal Salary { get; set; } = 0; // الراتب

        public decimal Deductions { get; set; } = 0; // الخصومات (السلف)

        // Navigation property for requests made by the user
        public ICollection<Request> Requests { get; set; } = new List<Request>();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
