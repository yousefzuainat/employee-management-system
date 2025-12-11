using System.ComponentModel.DataAnnotations;

namespace YousefZuaianatAPI.DTOs
{
    public class UpdateEmployeeDto
    {
        [Required]
        public string Name { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        public string? Password { get; set; } // Optional password update
    }
}
