using System.ComponentModel.DataAnnotations;

namespace YousefZuaianatAPI.Models
{
    public class UserTask
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        public string? Description { get; set; }


        [Required]
        public YousefZuaianatAPI.Models.Enum.TaskStatus Status { get; set; } = YousefZuaianatAPI.Models.Enum.TaskStatus.Pending;

        public string? RejectionReason { get; set; } // Reason if rejected by employee

        public DateTime? DueDate { get; set; }

    }
}
