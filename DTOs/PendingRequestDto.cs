using YousefZuaianatAPI.Models.Enum;

namespace YousefZuaianatAPI.DTOs
{
    public class PendingRequestDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string? Description { get; set; }
        public decimal? Amount { get; set; }
        public RequestType RequestType { get; set; }
        public DateTime SubmittedAt { get; set; }
    }
}
