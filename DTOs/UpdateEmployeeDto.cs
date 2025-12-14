using System.ComponentModel.DataAnnotations;

namespace YousefZuaianatAPI.DTOs
{
    public class UpdateEmployeeDto
    {
        public string? Name { get; set; }

        [EmailAddress]
        public string? Email { get; set; }

        public string? Password { get; set; } // Optional password update

        public decimal? Salary { get; set; } // Optional salary update
    }
}
