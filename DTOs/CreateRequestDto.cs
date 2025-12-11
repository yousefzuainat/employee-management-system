using System.ComponentModel.DataAnnotations;
using YousefZuaianatAPI.Models.Enum;

namespace YousefZuaianatAPI.DTOs
{
    public class CreateRequestDto
    {
        public string? Description { get; set; }

        public decimal? Amount { get; set; } // For Advance requests hr

        [Required]
        public RequestType RequestType { get; set; }
    }
}
