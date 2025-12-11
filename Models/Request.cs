using System.ComponentModel.DataAnnotations;
using YousefZuaianatAPI.Models.Enum;

namespace YousefZuaianatAPI.Models
{
    public class Request
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }
        public User? User { get; set; }

        public string? Description { get; set; }
        public decimal? Amount { get; set; }

        [Required]
        public RequestStatus Status { get; set; } = RequestStatus.Pending;

        [Required]
        public RequestType RequestType { get; set; }
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

        public int? ApprovedById { get; set; }
        public Role? ApprovedByRole { get; set; }
        public string? RejectionReason { get; set; }
    }
}
