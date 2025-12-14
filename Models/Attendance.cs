using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using YousefZuaianatAPI.Models;
using YousefZuaianatAPI.Models.Enum;

namespace YousefZuaianatAPI.Models
{
    public class Attendance
    {
        public int Id { get; set; }
        [Required]
        public int UserId { get; set; }
        public User? User { get; set; }
        [Required]

        public DateTime Date { get; set; }
        public TimeSpan? CheckInTime { get; set; }
        public TimeSpan? CheckOutTime { get; set; }
        public decimal? WorkHours { get; set; }
        [Required]
        public AttendanceStatus Status { get; set; } = AttendanceStatus.Present;


    }
}
