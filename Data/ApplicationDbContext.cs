using Microsoft.EntityFrameworkCore;
using YousefZuaianatAPI.Models;

namespace YousefZuaianatAPI.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Request> Requests { get; set; }
        public DbSet<UserTask> UserTasks { get; set; }
        public DbSet<LeaveBalance> LeaveBalances { get; set; }
        public DbSet<Attendance> Attendances { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User - Department (Employee Relationship)
            modelBuilder.Entity<User>()
                .HasOne(u => u.Department)
                .WithMany(d => d.Employees)
                .HasForeignKey(u => u.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete

            // Department - User (Manager Relationship)
            modelBuilder.Entity<Department>()
                .HasOne(d => d.Manager)
                .WithMany() // Manager can be manager of one department? Or just one-to-one?
                            // User model doesn't have "ManagedDepartment" navigation yet, but we can map it one-way if needed.
                            // If we want One-to-One: .WithOne(u => u.ManagedDepartment) <-- User needs this prop
                            // For now, let's keep it simple. Department has one Manager. 
                .HasForeignKey(d => d.ManagerId)
                .OnDelete(DeleteBehavior.Restrict);

            // User - Manager (Self-referencing)
            modelBuilder.Entity<User>()
                .HasOne(u => u.Manager)
                .WithMany() // Explicitly one-to-many
                .HasForeignKey(u => u.ManagerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Request - User
            modelBuilder.Entity<Request>()
                .HasOne(r => r.User)
                .WithMany(u => u.Requests)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
