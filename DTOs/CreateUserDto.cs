using System.ComponentModel.DataAnnotations;
using YousefZuaianatAPI.Models.Enum;

namespace YousefZuaianatAPI.DTOs
{
    public class CreateUserDto
    {
        [Required]
        public string Name { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }

        [Required]
        public Role Role { get; set; } = Role.Employee;

        public decimal Salary { get; set; } // New Field

        // Required if Role == DepartmentManager
        public string? DepartmentName { get; set; }

        // Required if Role == Employee
        public int? DirectManagerId { get; set; }
    }
}
