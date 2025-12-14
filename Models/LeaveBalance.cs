using System.ComponentModel.DataAnnotations;
using YousefZuaianatAPI.Models;

namespace YousefZuaianatAPI.Models
{
    public class LeaveBalance //رصيد الاجازات 
    {

        public int Id { get; set; }
        [Required]

        public int UserId { get; set; }
        public User? User { get; set; }

        [Required]
        public string LeaveType { get; set; } = string.Empty; // "إجازة سنوية", "إجازة مرضية", etc.
        //public int Remaining { get; set; }
        //public int Total { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;


        [Required]
        public int Remaining { get; set; } // 💡 متبقي

        [Required]
        public int Total { get; set; }     // 💡 كلي


    }
}
