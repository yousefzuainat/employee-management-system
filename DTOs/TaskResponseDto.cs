using System.ComponentModel.DataAnnotations;

namespace YousefZuaianatAPI.DTOs
{
    public class TaskResponseDto
    {
        [Required]
        public int TaskId { get; set; }

        [Required]
        public bool IsAccepted { get; set; } // true = Accepted, false = Rejected

        public string? RejectionReason { get; set; } // Required if Rejected
    }
}
