using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YousefZuaianatAPI.Data;
using YousefZuaianatAPI.DTOs;
using YousefZuaianatAPI.Models;
using YousefZuaianatAPI.Models.Enum;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace YousefZuaianatAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Employee")]
    public class EmployeeController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public EmployeeController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Retrieves tasks assigned to the current employee.
        /// </summary>
        [HttpGet("My-Task")]
        public async Task<IActionResult> GetMyTask()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized("User ID not found in claims.");

            var tasks = await _context.UserTasks
                .Where(t => t.UserId == userId.Value)
                .OrderByDescending(t => t.DueDate)
                .ToListAsync();

            return Ok(tasks);
        }

        /// <summary>
        /// Responds to a task by accepting or rejecting it.
        /// </summary>
        [HttpPost("RespondToTask")]
        public async Task<IActionResult> RespondToTask([FromBody] TaskResponseDto dto)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized("User ID not found in claims.");

            var task = await _context.UserTasks.FindAsync(dto.TaskId);
            if (task == null) return NotFound("Task not found.");

            if (task.UserId != userId.Value) return Forbid("You do not have permission to respond to this task.");

            if (dto.IsAccepted)
            {
                task.Status = YousefZuaianatAPI.Models.Enum.TaskStatus.Accepted;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(dto.RejectionReason))
                {
                    return BadRequest("Rejection reason is required when rejecting a task.");
                }
                task.Status = YousefZuaianatAPI.Models.Enum.TaskStatus.Rejected;
                task.RejectionReason = dto.RejectionReason;
            }

            await _context.SaveChangesAsync();
            return Ok(new { Message = $"Task {(dto.IsAccepted ? "Accepted" : "Rejected")}" });
        }

        /// <summary>
        /// Submits a request (Leave/Holiday to Manager, Advance to HR).
        /// </summary>
        [HttpPost("CreateRequest")]
        public async Task<IActionResult> CreateRequest([FromBody] CreateRequestDto dto)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized("User ID not found in claims.");

            // Basic Validation
            if (dto.RequestType == RequestType.Advance && (!dto.Amount.HasValue || dto.Amount <= 0))
            {
                return BadRequest("Amount is required for Advance requests.");
            }

            var request = new Request
            {
                UserId = userId.Value,
                RequestType = dto.RequestType,
                Description = dto.Description,
                Amount = dto.Amount,
                Status = RequestStatus.Pending,
                SubmittedAt = DateTime.UtcNow
            };

            _context.Requests.Add(request);
            await _context.SaveChangesAsync();

            // Note on Routing:
            // Requests of type 'Leave' allow managers to see them.
            // Requests of type 'Advance' allow HR to see them.
            // This logic relies on the 'GetPendingRequests' endpoints in respective controllers filtering by Type if needed.
            // Current HRController gets ALL pending. We might assume HR orchestrates everything or filters later.

            return Ok(new { Message = "Request submitted successfully", RequestId = request.Id });
        }

        [HttpGet("EmployeeInfo")]
        public async Task<IActionResult> GetEmployeeInfo()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized("User ID not found.");

            var user = await _context.Users
                .Include(u => u.Department)
                .Include(u => u.Manager)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) return NotFound("User not found.");

            var dto = new EmployeeInfoDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Role = user.Role.ToString(), // Converting Enum to String
                DepartmentName = user.Department?.Name ?? "No Department",
                ManagerId = user.ManagerId,
                ManagerName = user.Manager?.Name ?? "No Manager",
                BasicSalary = user.Salary,
                Deductions = user.Deductions,
                NetSalary = user.Salary - user.Deductions
            };

            return Ok(dto);
        }


        //function to get current user id
        private int? GetCurrentUserId()
        {
            // Try to set ApprovedById from current user claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("id")?.Value;
            if (int.TryParse(userIdClaim, out int currentUserId))
            {
                return currentUserId;
            }
            // For development/testing simplicity, you might want a fallback or return null.
            // Returning 1 for now as a fallback if auth is not set up, similar to HRController logic.
            // REMOVE THIS FALLBACK IN PRODUCTION.
            return 1;
        }
    }
}