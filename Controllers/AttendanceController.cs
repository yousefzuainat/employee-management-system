using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YousefZuaianatAPI.Data;
using YousefZuaianatAPI.Models;
using System.Security.Claims;
using YousefZuaianatAPI.Models.Enum;

namespace YousefZuaianatAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AttendanceController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AttendanceController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("MarkAttendance")]
        public async Task<IActionResult> MarkAttendance([FromBody] string qrContent)
        {
            // 1. Validate Format and Extract Time
            // Expected Format: "YousefZuaianat_Attendance_yyyyMMdd_HHmm"
            var parts = qrContent.Split('_');
            if (parts.Length != 4) return BadRequest("Invalid QR Code format. Expected: 'YousefZuaianat_Attendance_yyyyMMdd_HHmm'");

            string dateStr = parts[2];
            string timeStr = parts[3];
            string todayStr = DateTime.UtcNow.ToString("yyyyMMdd");

            if (dateStr != todayStr)
            {
                return BadRequest($"This QR Code is expired (wrong date). QR Date: {dateStr}, Today: {todayStr}");
            }

            // Parse Generation Time
            if (!DateTime.TryParseExact($"{dateStr}_{timeStr}", "yyyyMMdd_HHmm", null, System.Globalization.DateTimeStyles.None, out DateTime generationTime))
            {
                return BadRequest($"Invalid Scan time format. Got: {timeStr}");
            }

            // 2. Identify User
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized("User ID not found.");

            DateTime today = DateTime.UtcNow.Date;
            DateTime scanTime = DateTime.UtcNow;

            // 3. Find Record
            var attendance = await _context.Attendances
                .FirstOrDefaultAsync(a => a.UserId == userId.Value && a.Date == today);

            if (attendance == null)
            {
                // No record found (Maybe Initialization wasn't run?) -> Create New
                // Check Delay
                var delay = scanTime - generationTime;
                var status = (delay.TotalHours > 2) ? AttendanceStatus.Late : AttendanceStatus.Present;

                attendance = new Attendance
                {
                    UserId = userId.Value,
                    Date = today,
                    CheckInTime = scanTime.TimeOfDay,
                    Status = status
                };
                _context.Attendances.Add(attendance);
                await _context.SaveChangesAsync();

                return Ok(new { Message = $"Check-in successful ({status})", Time = attendance.CheckInTime });
            }
            else
            {
                // Existing Record Found
                // Case A: Is it an "Absent" record created by initialization?
                if (attendance.Status == AttendanceStatus.Absent && attendance.CheckInTime == null)
                {
                    // Update Absent -> Present/Late
                    var delay = scanTime - generationTime;
                    var status = (delay.TotalHours > 2) ? AttendanceStatus.Late : AttendanceStatus.Present;

                    attendance.CheckInTime = scanTime.TimeOfDay;
                    attendance.Status = status;

                    await _context.SaveChangesAsync();
                    return Ok(new { Message = $"Check-in successful (Updated from Absent to {status})", Time = attendance.CheckInTime });
                }

                // Case B: Already checked in, now checking out
                if (attendance.CheckOutTime == null)
                {
                    attendance.CheckOutTime = scanTime.TimeOfDay;
                    if (attendance.CheckInTime.HasValue)
                    {
                        attendance.WorkHours = (decimal)(attendance.CheckOutTime.Value - attendance.CheckInTime.Value).TotalHours;
                    }
                    await _context.SaveChangesAsync();
                    return Ok(new { Message = "Check-out successful", Time = attendance.CheckOutTime, WorkHours = attendance.WorkHours });
                }
                else
                {
                    return BadRequest("You have already checked out for today.");
                }
            }
        }

        // Helper to get current User ID
        private int? GetCurrentUserId()
        {
            // 1. محاولة قراءة الهوية من "بطاقة الدخول" (Token) التي أرسلها المستخدم
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("id")?.Value;

            // 2. إذا وجدنا رقماً داخل البطاقة، نقوم بتحويله لرقم صحيح (int) ونرجعه
            if (int.TryParse(userIdClaim, out int currentUserId))
            {
                return currentUserId;
            }

            // 3. هذا السطر مؤقت فقط للتجربة (لأننا لم نفعل نظام تسجيل الدخول الحقيقي بعد)
            // يعني: "إذا لم أعرف من أنت، سأفترض أنك المستخدم رقم 1"
            return 1;
        }
    }
}
