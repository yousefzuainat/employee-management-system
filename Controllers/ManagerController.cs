using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YousefZuaianatAPI.Data;
using YousefZuaianatAPI.DTOs;
using YousefZuaianatAPI.Models;
using YousefZuaianatAPI.Models.Enum;
using System.Security.Claims;

namespace YousefZuaianatAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ManagerController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ManagerController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // Helper to get current Manager ID
        // ==========================================
        //function to get current manager id
        private async Task<User?> GetCurrentManagerAsync()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("id")?.Value;

            // Temporary fallback for dev/testing if no auth
            if (userIdClaim == null) return await _context.Users.FirstOrDefaultAsync(u => u.Role == Role.DepartmentManager); // Just pick first manager for testing

            if (int.TryParse(userIdClaim, out int userId))
            {
                return await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.Role == Role.DepartmentManager);
            }
            return null;
        }

        // ==========================================
        // 1. Assign Tasks to Employees
        // ==========================================
        [HttpPost("AssignTask")]
        public async Task<IActionResult> AssignTask([FromBody] AssignTaskDto dto)
        {
            var manager = await GetCurrentManagerAsync();
            if (manager == null) return Unauthorized("Manager not found or not authenticated.");

            // Verify the employee exists and belongs to the manager's department
            var employee = await _context.Users.FirstOrDefaultAsync(u => u.Id == dto.EmployeeId);
            if (employee == null) return NotFound("Employee not found.");

            // Strict check: Manager can only assign tasks to their own direct reports or department members
            // Assuming ManagerId link or DepartmentId link.
            // Option A: Check if employee.ManagerId == manager.Id
            // Option B: Check if employee.DepartmentId == manager.DepartmentId

            // Let's use DepartmentId check for broader scope within department
            if (employee.DepartmentId != manager.DepartmentId)
            {
                return BadRequest("You can only assign tasks to employees within your department.");
            }

            var task = new UserTask
            {
                UserId = dto.EmployeeId,
                Description = dto.Description,
                DueDate = dto.DueDate,
                Status = YousefZuaianatAPI.Models.Enum.TaskStatus.Pending
            };

            _context.UserTasks.Add(task);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Task assigned successfully", TaskId = task.Id });
        }

        // ==========================================
        // 2. View Employees in Department
        // ==========================================
        [HttpGet("GetMyEmployees")]
        public async Task<IActionResult> GetMyEmployees()
        {
            var manager = await GetCurrentManagerAsync();
            if (manager == null) return Unauthorized("Manager not found or not authenticated.");

            var employees = await _context.Users
                .Where(u => u.DepartmentId == manager.DepartmentId && u.Role == Role.Employee)
                .Select(u => new
                {
                    u.Id,
                    u.Name,
                    u.Email,
                })
                .ToListAsync();

            return Ok(employees);
        }

        // ==========================================
        // 3. Manage Leave Requests (Approve/Reject)
        // ==========================================

        // Get Pending LEAVE Requests for this Manager's Department
        [HttpGet("GetPendingLeaveRequests")]
        public async Task<IActionResult> GetPendingLeaveRequests()
        {
            var manager = await GetCurrentManagerAsync();
            if (manager == null) return Unauthorized("Manager not found or not authenticated.");



            //  join users
            var requests = await _context.Requests
                .Include(r => r.User)
                .Where(r =>
                    r.RequestType == RequestType.Leave &&
                    r.Status == RequestStatus.Pending &&
                    r.User.DepartmentId == manager.DepartmentId
                )
                .Select(r => new PendingRequestDto
                {
                    Id = r.Id,
                    UserId = r.UserId,
                    UserName = r.User.Name,
                    Description = r.Description,
                    RequestType = r.RequestType,
                    SubmittedAt = r.SubmittedAt
                })
                .ToListAsync();

            return Ok(requests);
        }

        [HttpPost("RespondToLeaveRequest")]
        public async Task<IActionResult> RespondToLeaveRequest([FromBody] RequestResponseDto dto)
        {
            var manager = await GetCurrentManagerAsync();
            if (manager == null) return Unauthorized("Manager not found or not authenticated.");

            var request = await _context.Requests
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == dto.RequestId);

            if (request == null) return NotFound("Request not found.");

            // Security check: ensure this request belongs to an employee in manager's department
            if (request.User.DepartmentId != manager.DepartmentId)
            {
                return Forbid("This request does not belong to your department.");
            }

            if (request.RequestType != RequestType.Leave)
            {
                return BadRequest("This endpoint is only for Leave requests.");
            }

            if (request.Status != RequestStatus.Pending)
            {
                return BadRequest("Request is already processed.");
            }

            if (dto.IsApproved)
            {

                int daysRequested = (int)(request.Amount ?? 1); // Default to 1 day if not specified

                var leaveBalance = await _context.LeaveBalances
                    .FirstOrDefaultAsync(lb => lb.UserId == request.UserId && lb.LeaveType == "Annual"); // Assuming 'Annual' is the default type

                if (leaveBalance != null)
                {
                    // السيناريو الثاني: رصيد الموظف موجود مسبقاً في قاعدة البيانات
                    // نقوم بفحص ما إذا كان الرصيد المتبقي يكفي للإجازة المطلوبة
                    // ثم نخصم منها ونحدث السجل
                    if (leaveBalance.Remaining >= daysRequested)
                    {
                        leaveBalance.Remaining -= daysRequested;
                        _context.LeaveBalances.Update(leaveBalance);
                        request.Status = RequestStatus.Approved;
                    }
                    else
                    {
                        return BadRequest($"Insufficient leave balance. Remaining: {leaveBalance.Remaining}, Requested: {daysRequested}");
                    }
                }
                else
                {
                    // السيناريو الأول: الموظف يطلب إجازة لأول مرة وليس لديه سجل رصيد بعد
                    // نقوم بإنشاء سجل جديد له، نعطيه الرصيد الافتراضي (مثلاً 21 يوم)
                    // نخصم منه أيام الإجازة الحالية فوراً
                    leaveBalance = new LeaveBalance
                    {
                        UserId = request.UserId,
                        LeaveType = "Annual",
                        Total = 21,
                        Remaining = 21 - daysRequested
                    };
                    _context.LeaveBalances.Add(leaveBalance);
                    request.Status = RequestStatus.Approved;
                }
            }
            else
            {
                request.Status = RequestStatus.Rejected;
                request.RejectionReason = dto.RejectionReason;
            }

            request.ApprovedById = manager.Id;
            request.ApprovedByRole = Role.DepartmentManager;

            await _context.SaveChangesAsync();

            return Ok(new { Message = $"Leave Request {(dto.IsApproved ? "Approved" : "Rejected")}" });
        }
    }
}
