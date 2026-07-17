using FaydamPDKS.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace FaydamPDKS.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<AccessLog> AccessLogs { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<Zone> Zones { get; set; }
        public DbSet<Report> Reports { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<LeaveRequest> LeaveRequests { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Shift> Shifts { get; set; }
        public DbSet<EmployeeShiftAssignment> EmployeeShiftAssignments { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<AttendanceCorrectionRequest> AttendanceCorrectionRequests { get; set; }
        public DbSet<Workplace> Workplaces { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<WorkCalendarDay> WorkCalendarDays { get; set; }
        public DbSet<AttendanceTerminal> AttendanceTerminals { get; set; }
        public DbSet<AttendanceQrCode> AttendanceQrCodes { get; set; }
        public DbSet<BreakRecord> BreakRecords { get; set; }
        public DbSet<WorkLocationAssignment> WorkLocationAssignments { get; set; }
        public DbSet<WorkLocationAssignmentDay> WorkLocationAssignmentDays { get; set; }
        public DbSet<FieldWorkRequest> FieldWorkRequests { get; set; }
        public DbSet<FieldWorkRequestDay> FieldWorkRequestDays { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Role.Name unique
            modelBuilder.Entity<Role>()
                .HasIndex(r => r.Name)
                .IsUnique();

            // User - Role ilişkisi
            // (User modelinde RoleId + Role navigation property olmalı)
            modelBuilder.Entity<User>()
                .HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<User>()
                .HasIndex(x => x.Email)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(x => x.EmployeeNumber)
                .IsUnique();
            modelBuilder.Entity<User>().HasIndex(x => x.PhoneNumber).IsUnique().HasFilter("\"PhoneNumber\" IS NOT NULL");

            modelBuilder.Entity<RefreshToken>()
                .HasIndex(x => x.TokenHash)
                .IsUnique();

            modelBuilder.Entity<RefreshToken>()
                .HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AccessLog>()
                .HasIndex(x => x.DeviceEventId)
                .IsUnique()
                .HasFilter("device_event_id IS NOT NULL");

            modelBuilder.Entity<LeaveRequest>()
                .HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<LeaveRequest>()
                .HasIndex(x => new { x.UserId, x.StartDate, x.EndDate });

            modelBuilder.Entity<Notification>()
                .HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Notification>()
                .HasIndex(x => new { x.UserId, x.ReadAt, x.CreatedAt });

            modelBuilder.Entity<Shift>()
                .HasIndex(x => x.Name)
                .IsUnique();

            modelBuilder.Entity<EmployeeShiftAssignment>()
                .HasOne(x => x.Employee)
                .WithMany()
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<EmployeeShiftAssignment>()
                .HasOne(x => x.Shift)
                .WithMany()
                .HasForeignKey(x => x.ShiftId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<EmployeeShiftAssignment>()
                .HasIndex(x => new { x.EmployeeId, x.ValidFrom, x.ValidTo });

            modelBuilder.Entity<AuditLog>()
                .HasOne(x => x.ActorUser)
                .WithMany()
                .HasForeignKey(x => x.ActorUserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<AuditLog>()
                .HasIndex(x => x.OccurredAt);

            modelBuilder.Entity<AuditLog>()
                .HasIndex(x => new { x.EntityType, x.EntityId });

            modelBuilder.Entity<AttendanceCorrectionRequest>()
                .HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<AttendanceCorrectionRequest>()
                .HasIndex(x => new { x.UserId, x.WorkDate, x.Status });

            modelBuilder.Entity<Workplace>().HasIndex(x => x.Code).IsUnique();
            modelBuilder.Entity<Department>().HasIndex(x => new { x.WorkplaceId, x.Code }).IsUnique();
            modelBuilder.Entity<Department>().HasOne(x => x.Workplace).WithMany(x => x.Departments)
                .HasForeignKey(x => x.WorkplaceId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<User>().HasOne(x => x.Workplace).WithMany().HasForeignKey(x => x.WorkplaceId)
                .OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<User>().HasOne(x => x.Department).WithMany().HasForeignKey(x => x.DepartmentId)
                .OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<WorkCalendarDay>().HasOne(x => x.Workplace).WithMany().HasForeignKey(x => x.WorkplaceId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<WorkCalendarDay>().HasIndex(x => new { x.WorkplaceId, x.Date }).IsUnique()
                .HasFilter("workplace_id IS NOT NULL");
            modelBuilder.Entity<WorkCalendarDay>().HasIndex(x => x.Date).IsUnique()
                .HasFilter("workplace_id IS NULL");
            modelBuilder.Entity<AttendanceTerminal>().HasOne(x => x.Workplace).WithMany().HasForeignKey(x => x.WorkplaceId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<AttendanceTerminal>().HasIndex(x => x.SerialNumber).IsUnique();
            modelBuilder.Entity<Zone>().HasOne(x => x.Workplace).WithMany().HasForeignKey(x => x.WorkplaceId)
                .OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<AttendanceQrCode>().HasOne(x => x.Workplace).WithMany().HasForeignKey(x => x.WorkplaceId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<AttendanceQrCode>().HasOne(x => x.Zone).WithMany().HasForeignKey(x => x.ZoneId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<AttendanceQrCode>().HasIndex(x => x.TokenHash).IsUnique();
            modelBuilder.Entity<AttendanceQrCode>().HasIndex(x => new { x.WorkplaceId, x.ZoneId, x.EventType, x.IsActive });
            modelBuilder.Entity<BreakRecord>().HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<BreakRecord>().HasIndex(x => x.StartDeviceEventId).IsUnique();
            modelBuilder.Entity<BreakRecord>().HasIndex(x => x.EndDeviceEventId).IsUnique()
                .HasFilter("end_device_event_id IS NOT NULL");
            modelBuilder.Entity<BreakRecord>().HasIndex(x => new { x.UserId, x.EndedAt });

            modelBuilder.Entity<WorkLocationAssignment>().HasOne(x => x.User).WithMany()
                .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<WorkLocationAssignment>().HasIndex(x => new { x.UserId, x.StartDate, x.EndDate, x.IsActive });
            modelBuilder.Entity<WorkLocationAssignmentDay>().HasOne(x => x.Assignment).WithMany(x => x.Days)
                .HasForeignKey(x => x.AssignmentId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<WorkLocationAssignmentDay>().HasIndex(x => new { x.AssignmentId, x.DayOfWeek }).IsUnique();

            modelBuilder.Entity<FieldWorkRequest>().HasOne(x => x.User).WithMany()
                .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<FieldWorkRequest>().HasIndex(x => new { x.UserId, x.StartDate, x.EndDate, x.Status });
            modelBuilder.Entity<FieldWorkRequestDay>().HasOne(x => x.Request).WithMany(x => x.Days)
                .HasForeignKey(x => x.RequestId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<FieldWorkRequestDay>().HasIndex(x => new { x.RequestId, x.DayOfWeek }).IsUnique();

        }
    }
}
