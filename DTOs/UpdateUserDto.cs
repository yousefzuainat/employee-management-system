using System.ComponentModel.DataAnnotations;
using YousefZuaianatAPI.Models.Enum;

namespace YousefZuaianatAPI.DTOs
{
    public class UpdateUserDto
    {
        public string? Name { get; set; }

        [EmailAddress]
        public string? Email { get; set; }

        // Optional: Update password? Let's leave it out for now or add it optional.
        public string? Password { get; set; }

        public int? DepartmentId { get; set; }
        public int? ManagerId { get; set; }
    }
}
