using System.ComponentModel.DataAnnotations;

namespace YousefZuaianatAPI.DTOs
{
    public class UpdateManagerDto
    {
        public string? Name { get; set; }

        [EmailAddress]
        public string? Email { get; set; }

        public string? Password { get; set; } // Optional password update

        public int? DepartmentId { get; set; } // Optional: Change department
        public string? DepartmentName { get; set; } // Optional: Rename department
    }
}
