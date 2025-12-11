using System.ComponentModel.DataAnnotations;

namespace YousefZuaianatAPI.DTOs
{
    public class AssignTaskDto
    {
        [Required]
        public int EmployeeId { get; set; }

        [Required]
        public string Description { get; set; }

        public DateTime? DueDate { get; set; }
    }
}
