using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YousefZuaianatAPI.Data;
using YousefZuaianatAPI.DTOs;
using YousefZuaianatAPI.Models;
using YousefZuaianatAPI.Models.Enum;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity; // <--- مطلوب للـ PasswordHasher

namespace YousefZuaianatAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "admin")]
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // 1. Create Users
        // ==========================================
        [HttpPost("CreateUser")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
        {
            // 1. Admin cannot create another Admin
            if (dto.Role == Role.admin)
            {
                return BadRequest("Admins cannot create other Admins.");
            }

            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
            {
                return BadRequest("Email already exists.");
            }

            var newUser = new User
            {
                Name = dto.Name,
                Email = dto.Email,
                Role = dto.Role,
                Salary = dto.Salary,
                CreatedAt = DateTime.UtcNow
            };

            // Hash the password
            newUser.PasswordHash = new PasswordHasher<User>().HashPassword(newUser, dto.Password);

            // Logic for Department Manager
            if (dto.Role == Role.DepartmentManager)
            {
                if (string.IsNullOrWhiteSpace(dto.DepartmentName))
                {
                    return BadRequest("Department Name is required for Department Managers.");
                }

                var newDepartment = new Department
                {
                    Name = dto.DepartmentName
                };

                _context.Departments.Add(newDepartment);
                await _context.SaveChangesAsync(); // Save to get Dept ID

                newUser.DepartmentId = newDepartment.Id;

                // Save User first (to get User ID)
                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                // 2. Set Department ManagerId to this new User
                newDepartment.ManagerId = newUser.Id;
                _context.Departments.Update(newDepartment);

                // 3. Set User's ManagerId to themselves (Self-Managed)
                newUser.ManagerId = newUser.Id;
                _context.Users.Update(newUser); // Update user again

                await _context.SaveChangesAsync();
            }
            // Logic for Employee or HR
            else if (dto.Role == Role.Employee || dto.Role == Role.HR)
            {
                if (!dto.DirectManagerId.HasValue || dto.DirectManagerId.Value == 0)
                {
                    return BadRequest("Direct Manager ID is required for Employees and HR.");
                }

                if (dto.Salary <= 0)
                {
                    return BadRequest("Salary is required and must be greater than 0 for Employees and HR.");
                }

                // Find the manager
                var manager = await _context.Users.FindAsync(dto.DirectManagerId.Value);
                if (manager == null)
                {
                    return BadRequest("The specified manager does not exist.");
                }

                // Check if manager has a department
                if (manager.DepartmentId == null)
                {
                    return BadRequest("The specified manager is not assigned to any department, so the employee cannot be assigned.");
                }

                // Assign User's Manager and Department match the Manager's
                newUser.ManagerId = manager.Id;
                newUser.DepartmentId = manager.DepartmentId;

                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();
            }
            // Logic for other roles (like SuperAdmin) or fallback
            else
            {
                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();
            }

            return Ok(new { Message = "User created successfully", UserId = newUser.Id });
        }

        // ==========================================
        // 2 & 6. Manage All Requests (View & Respond)
        // ==========================================
        [HttpGet("GetAllRequests")]
        public async Task<IActionResult> GetAllRequests()
        {
            var requests = await _context.Requests
                .Include(r => r.User)
                .OrderByDescending(r => r.SubmittedAt)
                .Select(r => new PendingRequestDto
                {
                    Id = r.Id,
                    UserId = r.UserId,
                    UserName = r.User.Name,
                    Description = r.Description,
                    Amount = r.Amount,
                    RequestType = r.RequestType,
                    SubmittedAt = r.SubmittedAt
                })
                .ToListAsync();

            return Ok(requests);
        }

        [HttpPost("RespondToRequest")]
        public async Task<IActionResult> RespondToRequest([FromBody] RequestResponseDto dto)
        {
            var request = await _context.Requests
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == dto.RequestId);

            if (request == null) return NotFound("Request not found.");

            if (dto.IsApproved)
            {
                // If it's a Leave request, handle balance logic?
                // Admin has overridden power. Let's apply same logic as Manager if it's a Leave.
                if (request.RequestType == RequestType.Leave)
                {
                    int daysRequested = (int)(request.Amount ?? 1);
                    var leaveBalance = await _context.LeaveBalances
                        .FirstOrDefaultAsync(lb => lb.UserId == request.UserId);

                    if (leaveBalance == null)
                    {
                        // Create default
                        _context.LeaveBalances.Add(new LeaveBalance
                        {
                            UserId = request.UserId,
                            Total = 21,
                            Remaining = 21 - daysRequested,
                            LeaveType = "Annual"
                        });
                    }
                    else
                    {
                        if (leaveBalance.Remaining >= daysRequested)
                        {
                            leaveBalance.Remaining -= daysRequested;
                            _context.LeaveBalances.Update(leaveBalance);
                        }
                        else
                        {
                            return BadRequest("Insufficient leave balance.");
                        }
                    }
                }
                // If it's an Advance request, ADD to Deductions
                else if (request.RequestType == RequestType.Advance)
                {
                    var user = await _context.Users.FindAsync(request.UserId);
                    if (user != null)
                    {
                        var amount = request.Amount ?? 0;
                        if ((user.Salary - user.Deductions) >= amount)
                        {
                            user.Deductions += amount;
                            _context.Users.Update(user);
                        }
                        else
                        {
                            return BadRequest($"Insufficient remaining salary. Salary: {user.Salary}, Current Deductions: {user.Deductions}");
                        }
                    }
                }

                request.Status = RequestStatus.Approved;
            }
            else
            {
                request.Status = RequestStatus.Rejected;
                request.RejectionReason = string.IsNullOrWhiteSpace(dto.RejectionReason)
                    ? "Rejected by Admin"
                    : $"Rejected by Admin: {dto.RejectionReason}";
            }

            // Using ID 1 as Admin or finding actual admin
            request.ApprovedById = 1;
            request.ApprovedByRole = Role.admin;

            await _context.SaveChangesAsync();

            return Ok(new { Message = $"Request {(dto.IsApproved ? "Approved" : "Rejected")} by Admin." });
        }

        // ==========================================
        // 3. Update User
        // ==========================================
        [HttpPut("UpdateUser/{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserDto dto)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound("User not found.");

            // Admin cannot edit other Admins (except maybe themselves, but let's restrict for now as per requirement)
            if (user.Role == Role.admin)
            {
                return Forbid("Cannot modify another Admin account.");
            }

            if (!string.IsNullOrWhiteSpace(dto.Name)) user.Name = dto.Name;
            if (!string.IsNullOrWhiteSpace(dto.Email)) user.Email = dto.Email;
            if (!string.IsNullOrWhiteSpace(dto.Password))
                user.PasswordHash = new PasswordHasher<User>().HashPassword(user, dto.Password);

            if (dto.DepartmentId.HasValue && dto.DepartmentId.Value != 0)
                user.DepartmentId = dto.DepartmentId;

            if (dto.ManagerId.HasValue && dto.ManagerId.Value != 0)
                user.ManagerId = dto.ManagerId;

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "User updated successfully." });
        }

        // ==========================================
        // 4. Delete User
        // ==========================================
        [HttpDelete("DeleteUser/{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound("User not found.");


            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "User deleted successfully." });
        }

        // ==========================================
        // 7. Update Department
        // ==========================================
        [HttpPut("UpdateDepartment/{id}")]
        public async Task<IActionResult> UpdateDepartment(int id, [FromBody] UpdateDepartmentDto dto)
        {
            var department = await _context.Departments.FindAsync(id);
            if (department == null) return NotFound("Department not found.");

            department.Name = dto.Name;
            if (dto.Description != null) department.Description = dto.Description;

            if (dto.ManagerId.HasValue)
            {
                var manager = await _context.Users.FindAsync(dto.ManagerId.Value);
                if (manager != null)
                {
                    department.ManagerId = manager.Id;
                    // Ensure manager is linked to this department?
                    if (manager.DepartmentId != id)
                    {
                        manager.DepartmentId = id;
                        _context.Users.Update(manager);
                    }
                }
            }

            _context.Departments.Update(department);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Department updated successfully." });
        }
    }
}
