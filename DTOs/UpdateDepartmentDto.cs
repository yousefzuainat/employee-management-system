using System.ComponentModel.DataAnnotations;

namespace YousefZuaianatAPI.DTOs
{
    public class UpdateDepartmentDto
    {
        [Required]
        public string Name { get; set; }

        public string? Description { get; set; }

        public int? ManagerId { get; set; }
    }
}
