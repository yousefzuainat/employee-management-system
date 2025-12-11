using System.ComponentModel.DataAnnotations;
using YousefZuaianatAPI.Models.Enum;

namespace YousefZuaianatAPI.DTOs
{
    public class RequestActionDto
    {
        [Required]
        public int RequestId { get; set; }
        [Required]
        public bool IsApproved { get; set; } // true = Approve, false = Reject
        public string? RejectionReason { get; set; }
    }
}
