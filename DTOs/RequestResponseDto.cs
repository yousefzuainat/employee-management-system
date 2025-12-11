using System.ComponentModel.DataAnnotations;

namespace YousefZuaianatAPI.DTOs
{
    public class RequestResponseDto
    {
        [Required]
        public int RequestId { get; set; }

        [Required]
        public bool IsApproved { get; set; } // true = Approve, false = Reject

        public string? RejectionReason { get; set; }
    }
}
