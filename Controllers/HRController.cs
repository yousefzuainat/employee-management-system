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
    public class HRController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public HRController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // Employee Management
        // ==========================================

        /// <summary>
        /// Creates a new Employee and assigns them to a Manager's Department.
        /// </summary>
        /// <param name="dto">Data Transfer Object containing employee details and manager ID.</param>
        /// <returns>Result of the creation process.</returns>
        [HttpPost("CreateEmployee")]
        public async Task<IActionResult> CreateEmployee([FromBody] CreateEmployeeDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            // 1. Verify that the specified Manager exists
            var manager = await _context.Users
                .Include(u => u.Department)
                .FirstOrDefaultAsync(u => u.Id == dto.ManagerId);

            if (manager == null)
            {
                return NotFound("Manager (Department Head) not found.");
            }

            // 2. Find the department managed by this manager
            var department = await _context.Departments.FirstOrDefaultAsync(d => d.ManagerId == manager.Id);

            if (department == null)
            {
                return BadRequest("The specified user is not managing any department.");
            }

            // 3. Create the Employee User object
            var employee = new User
            {
                Name = dto.Name,
                Email = dto.Email,
                PasswordHash = dto.Password, // Note: Password should be hashed in a real application
                Role = Role.Employee,
                ManagerId = manager.Id, // Assign direct manager
                DepartmentId = department.Id, // Assign to manager's department
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Users.Add(employee);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Employee created successfully", EmployeeId = employee.Id });
        }

        /// <summary>
        /// Deletes an Employee by their ID.
        /// </summary>
        /// <param name="id">The ID of the employee to delete.</param>
        /// <returns>Success message or Not Found.</returns>
        [HttpDelete("DeleteEmployee/{id}")]
        public async Task<IActionResult> DeleteEmployee(int id)
        {
            var employee = await _context.Users.FindAsync(id);
            if (employee == null)
            {
                return NotFound("Employee not found!!");
            }

            _context.Users.Remove(employee);
            await _context.SaveChangesAsync();
            return Ok("Employee deleted successfully");
        }

        /// <summary>
        /// Retrieves a list of all users in the system.
        /// </summary>
        /// <returns>List of Users.</returns>
        [HttpGet("GetAllUser")]
        public async Task<IActionResult> GetAllUser()
        {
            // Note: Returning password hashes is a security risk. 
            // Returning only names as requested.
            var users = await _context.Users
                .Select(u => new { u.Id, u.Name }) // Projecting to anonymous object with only specific fields
                .ToListAsync();
            return Ok(users);
        }

        /// <summary>
        /// Updates an existing Employee's details. Password update is optional.
        /// </summary>
        /// <param name="id">ID of the employee to update.</param>
        /// <param name="dto">Updated details.</param>
        /// <returns>Success message.</returns>
        [HttpPut("updateEmployee/{id}")]
        public async Task<IActionResult> UpdateEmployee(int id, [FromBody] UpdateEmployeeDto dto)
        {
            var employee = await _context.Users.FindAsync(id);
            if (employee == null)
            {
                return NotFound("Employee not found!!");
            }

            // Update basic fields
            employee.Name = dto.Name;
            employee.Email = dto.Email;

            // Only update password if a new one is provided
            if (!string.IsNullOrEmpty(dto.Password))
            {
                employee.PasswordHash = dto.Password;
            }

            employee.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok("Employee updated successfully");
        }


        // ==========================================
        // Manager Management
        // ==========================================

        /// <summary>
        /// Creates a new Department Manager and their Department.
        /// </summary>
        /// <param name="dto">Details for the manager and the department.</param>
        /// <returns>Created Manager and Department IDs.</returns>
        [HttpPost("CreateManager")]
        public async Task<IActionResult> CreateManager([FromBody] CreateManagerDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            using var transaction = _context.Database.BeginTransaction();
            try
            {
                // 1. Create the Manager User
                var manager = new User
                {
                    Name = dto.Name,
                    Email = dto.Email,
                    PasswordHash = dto.Password, // Note: Hash in production
                    Role = Role.DepartmentManager,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Users.Add(manager);
                await _context.SaveChangesAsync();

                // 2. Create the Department and link it to the Manager
                var department = new Department
                {
                    Name = dto.DepartmentName,
                    Description = dto.DepartmentDescription,
                    ManagerId = manager.Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Departments.Add(department);
                await _context.SaveChangesAsync();

                // 3. Assign the Manager to the Department (as a member too)
                manager.DepartmentId = department.Id;
                _context.Users.Update(manager);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return Ok(new { Message = "Manager and Department created successfully", ManagerId = manager.Id, DepartmentId = department.Id });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        /// <summary>
        /// Deletes a Manager by ID.
        /// </summary>
        /// <param name="id">Manager ID.</param>
        /// <returns>Success message.</returns>
        [HttpDelete("DeleteManager/{id}")]
        public async Task<IActionResult> DeleteManager(int id)
        {
            var manager = await _context.Users.FindAsync(id);
            if (manager == null)
            {
                return NotFound("Manager not found!!");
            }

            // Note: Deleting a manager might affect their department. 
            // Additional logic might be needed to handle the orphaned department.

            _context.Users.Remove(manager);
            await _context.SaveChangesAsync();
            return Ok("Manager deleted successfully");
        }

        /// <summary>
        /// Updates a Manager's details. Password update is optional.
        /// </summary>
        /// <param name="id">Manager ID.</param>
        /// <param name="dto">Updated details.</param>
        /// <returns>Success message.</returns>
        [HttpPut("updateManager/{id}")]
        public async Task<IActionResult> UpdateManager(int id, [FromBody] UpdateManagerDto dto)
        {
            var manager = await _context.Users.FindAsync(id);
            if (manager == null)
            {
                return NotFound("Manager not found!!");
            }

            manager.Name = dto.Name;
            manager.Email = dto.Email;

            // Optional: Change the department this manager is assigned to
            if (dto.DepartmentId.HasValue)
            {
                var dept = await _context.Departments.FindAsync(dto.DepartmentId.Value);
                if (dept == null)
                {
                    return BadRequest("Department not found");
                }
                manager.DepartmentId = dto.DepartmentId.Value;
            }

            // Optional: Update password if provided
            if (!string.IsNullOrEmpty(dto.Password))
            {
                manager.PasswordHash = dto.Password;
            }

            // Optional: Rename the department the manager belongs to
            if (!string.IsNullOrEmpty(dto.DepartmentName))
            {
                // Determine which department to rename: the newly assigned one or existing one
                var targetDeptId = dto.DepartmentId ?? manager.DepartmentId;//إذا كان العميل قد أرسل dto.DepartmentId جديداً (أي أنه يريد نقل المدير إلى قسم جديد)، فسنستخدم هذا الـ ID الجديد.
                                                                            //وإلا (إذا كان dto.DepartmentId فارغاً)، فسنستخدم الـ ID الحالي المخزن في بيانات المدير الأصلية (manager.DepartmentId).


                if (targetDeptId.HasValue)
                {
                    var existingDept = await _context.Departments.FindAsync(targetDeptId.Value);
                    if (existingDept != null)
                    {
                        existingDept.Name = dto.DepartmentName;
                        // Since we are in the same context, this change will be saved on SaveChangesAsync
                    }
                }
            }

            manager.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok("Manager updated successfully");
        }

        // ==========================================
        // Request Management
        // ==========================================

        /// <summary>
        /// Retrieves pending requests ( Advances) for HR review.
        /// </summary>
        /// <returns>List of pending requests.</returns>
        [HttpGet("GetPendingRequests")]
        public async Task<IActionResult> GetPendingRequests()
        {
            var requests = await _context.Requests
                .Where(r => r.Status == RequestStatus.Pending && r.RequestType == RequestType.Advance)
                .OrderByDescending(r => r.SubmittedAt)
                .Select(r => new PendingRequestDto
                {
                    Id = r.Id,
                    UserId = r.UserId,
                    UserName = r.User != null ? r.User.Name : "Unknown",
                    Description = r.Description,
                    Amount = r.Amount,
                    RequestType = r.RequestType,
                    SubmittedAt = r.SubmittedAt
                })
                .ToListAsync();

            return Ok(requests);
        }

        /// <summary>
        /// HR responds to a request by approving or rejecting it.
        /// </summary>
        /// <param name="dto">Action details (Approve/Reject).</param>
        /// <returns>Result of the action.</returns>
        [HttpPost("RespondToRequest")]
        public async Task<IActionResult> RespondToRequest([FromBody] RequestActionDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var request = await _context.Requests.FindAsync(dto.RequestId);
            if (request == null) return NotFound("Request not found");

            if (request.Status != RequestStatus.Pending)
                return BadRequest("Request is not pending.");

            if (dto.IsApproved)
            {
                // If it is an Advance request, ADD to Deductions
                if (request.RequestType == RequestType.Advance)
                {
                    var user = await _context.Users.FindAsync(request.UserId);
                    if (user != null)
                    {
                        var amount = request.Amount ?? 0;
                        // Optional check: Can they afford this deduction from their salary?
                        if ((user.Salary - user.Deductions) >= amount)
                        {
                            user.Deductions += amount; // Increase debt
                            _context.Users.Update(user);
                        }
                        else
                        {
                            // Optional: Block if remaining salary isn't enough
                            return BadRequest($"Insufficient remaining salary for this advance. Salary: {user.Salary}, Current Deductions: {user.Deductions}");
                        }
                    }
                }

                request.Status = RequestStatus.Approved;
            }
            else
            {
                request.Status = RequestStatus.Rejected;
                request.RejectionReason = dto.RejectionReason;
            }

            // Try to set ApprovedById from current user claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("id")?.Value;
            if (int.TryParse(userIdClaim, out int currentUserId))
            {
                request.ApprovedById = currentUserId;
            }
            else
            {
                request.ApprovedById = 1; // Default/Fallback if no user context
            }

            request.ApprovedByRole = Role.HR;

            await _context.SaveChangesAsync();

            return Ok(new { Message = $"Request {(dto.IsApproved ? "Approved" : "Rejected")}" });
        }


        [HttpGet("CreateDailyQRCode")]
        public IActionResult CreatingQRCode()
        {
            // Generate a unique token for today INCLUDING TIME
            // Format: "YousefZuaianat_Attendance_yyyyMMdd_HHmm"
            // Example: "YousefZuaianat_Attendance_20251210_0830"
            string dateTimePart = DateTime.UtcNow.ToString("yyyyMMdd_HHmm");
            string qrContent = $"YousefZuaianat_Attendance_{dateTimePart}";

            // Using QRCoder to generate PNG
            using (var qrGenerator = new QRCoder.QRCodeGenerator())
            {
                var qrCodeData = qrGenerator.CreateQrCode(qrContent, QRCoder.QRCodeGenerator.ECCLevel.Q);
                using (var qrCode = new QRCoder.PngByteQRCode(qrCodeData))
                {
                    var qrCodeImage = qrCode.GetGraphic(20);////(Pixels per
                    return File(qrCodeImage, "image/png"); // Returns the image directly
                }
            }
        }

        /// <summary>
        /// إضافة دالة جديدة InitializeDay يقوم الـ HR بضغطها صباحاً؛ وظيفتها إنشاء سجل "غائب" (Absent) لجميع الموظفين كحالة افتراضية.
        /// 
        /// </summary>
        [HttpPost("InitializeDailyAttendance")]
        public async Task<IActionResult> InitializeDailyAttendance()
        {
            DateTime today = DateTime.UtcNow.Date;

            // Get all user who don't have an attendance record for today yet, excluding HR
            var usersWithoutAttendance = await _context.Users
                .Where(u => u.Role != Role.HR &&
                            !_context.Attendances.Any(a => a.UserId == u.Id && a.Date == today))
                .ToListAsync();

            if (!usersWithoutAttendance.Any())
            {
                return Ok("Attendance already initialized for today or no users found.");
            }

            var absenteeRecords = new List<Attendance>();
            foreach (var user in usersWithoutAttendance)
            {
                absenteeRecords.Add(new Attendance
                {
                    UserId = user.Id,
                    Date = today,
                    Status = AttendanceStatus.Absent, // Default Absent
                    // CheckInTime is null, WorkHours is null
                });
            }

            _context.Attendances.AddRange(absenteeRecords);
            await _context.SaveChangesAsync();

            return Ok(new { Message = $"Initialized {absenteeRecords.Count} employees as Absent." });
        }
    }
}
