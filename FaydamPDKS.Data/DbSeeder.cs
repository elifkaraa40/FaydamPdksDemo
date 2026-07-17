using FaydamPDKS.Core.Models;
using Microsoft.EntityFrameworkCore;
using FaydamPDKS.Core.Attendance;

namespace FaydamPDKS.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(AppDbContext context)
        {
            // =====================
            // 1. ROLLER
            // =====================
            if (!await context.Roles.AnyAsync())
            {
                var yoneticiRole = new Role
                {
                    Id = Guid.NewGuid(),
                    Name = "Yonetici",
                    NormalizedName = "YONETICI",
                    Description = "Sistem yöneticisi / İK / Yetkili kişi. Tüm verilere erişir, izin onaylar, rapor alır."
                };

                var personelRole = new Role
                {
                    Id = Guid.NewGuid(),
                    Name = "Personel",
                    NormalizedName = "PERSONEL",
                    Description = "Normal çalışan. Sadece kendi verilerine erişir."
                };

                context.Roles.AddRange(yoneticiRole, personelRole);
                await context.SaveChangesAsync();
            }

            // =====================
            // 2. TEST KULLANICILARI
            // =====================
            if (!await context.Users.AnyAsync())
            {
                var yoneticiRole = await context.Roles.FirstAsync(r => r.Name == "Yonetici");
                var personelRole = await context.Roles.FirstAsync(r => r.Name == "Personel");

                // --- Yönetici (test) ---
                var yoneticiUser = new User
                {
                    Id = Guid.NewGuid(),
                    Name = "Demo Yonetici",
                    Email = "yonetici@faydam.com",
                    EmployeeNumber = "YON-0001",
                    DepartmentLegacy = "Yonetim",
                    IsActive = true,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Yonetici123!"),
                    RoleId = yoneticiRole.Id,
                    PhoneNumber = "0555 000 0001",
                    IsEmailNotificationEnabled = true,
                    IsSmsNotificationEnabled = false
                };

                // --- Personel (test) ---
                var personelUser = new User
                {
                    Id = Guid.NewGuid(),
                    Name = "Demo Personel",
                    Email = "personel@faydam.com",
                    EmployeeNumber = "PER-0001",
                    DepartmentLegacy = "Operasyon",
                    IsActive = true,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Personel123!"),
                    RoleId = personelRole.Id,
                    PhoneNumber = "0555 000 0002",
                    IsEmailNotificationEnabled = true,
                    IsSmsNotificationEnabled = false
                };

                context.Users.AddRange(yoneticiUser, personelUser);
                await context.SaveChangesAsync();
            }

            var workplace = await context.Workplaces.FirstOrDefaultAsync(x => x.Code == "MERKEZ");
            if (workplace is null)
            {
                workplace = new Workplace { Id = Guid.NewGuid(), Code = "MERKEZ", Name = "Merkez İşyeri", TimeZoneId = "Europe/Istanbul", IsActive = true };
                context.Workplaces.Add(workplace);
                await context.SaveChangesAsync();
            }
            foreach (var departmentName in new[] { "Yonetim", "Operasyon" })
            {
                var department = await context.Departments.FirstOrDefaultAsync(x => x.WorkplaceId == workplace.Id && x.Name == departmentName);
                if (department is null)
                {
                    department = new Department { Id = Guid.NewGuid(), WorkplaceId = workplace.Id, Code = departmentName.ToUpperInvariant(), Name = departmentName, IsActive = true };
                    context.Departments.Add(department);
                    await context.SaveChangesAsync();
                }
                var users = await context.Users.Where(x => x.DepartmentLegacy == departmentName && !x.DepartmentId.HasValue).ToListAsync();
                foreach (var user in users) { user.WorkplaceId = workplace.Id; user.DepartmentId = department.Id; }
                await context.SaveChangesAsync();
            }

            if (!await context.Shifts.AnyAsync())
            {
                var shift = new Shift
                {
                    Id = Guid.NewGuid(), Name = "Standart 08:30-18:00", StartsAt = new TimeOnly(8, 30),
                    EndsAt = new TimeOnly(18, 0), BreakMinutes = 60, ScheduledBreakStart = new TimeOnly(12, 30),
                    ScheduledBreakEnd = new TimeOnly(13, 30), LateToleranceMinutes = 5,
                    EarlyLeaveToleranceMinutes = 5, IsActive = true
                };
                context.Shifts.Add(shift);
                var employees = await context.Users.Select(x => x.Id).ToListAsync();
                context.EmployeeShiftAssignments.AddRange(employees.Select(employeeId => new EmployeeShiftAssignment
                {
                    Id = Guid.NewGuid(), EmployeeId = employeeId, ShiftId = shift.Id,
                    ValidFrom = DateOnly.FromDateTime(DateTime.UtcNow)
                }));
                await context.SaveChangesAsync();
            }

            var mainZone = await context.Zones.FirstOrDefaultAsync(x => x.Name == "Faydam Merkez Giriş-Çıkış");
            if (mainZone is null)
            {
                mainZone = new Zone { Name = "Faydam Merkez Giriş-Çıkış", WorkplaceId = workplace.Id, IsActive = true };
                context.Zones.Add(mainZone);
                await context.SaveChangesAsync();
            }

            if (!await context.AttendanceQrCodes.AnyAsync())
            {
                var createdAt = DateTimeOffset.UtcNow;
                context.AttendanceQrCodes.AddRange(
                    new AttendanceQrCode
                    {
                        Id = Guid.NewGuid(), WorkplaceId = workplace.Id, ZoneId = mainZone.Id, Name = "Mevcut Giriş QR",
                        EventType = AttendanceEventType.Entry, TokenHash = "568A654846A94ACA3682AAC2BB5197E896FFFD2018B5A296483F0A6C543EE0BA",
                        IsLegacy = true, IsActive = true, CreatedAt = createdAt
                    },
                    new AttendanceQrCode
                    {
                        Id = Guid.NewGuid(), WorkplaceId = workplace.Id, ZoneId = mainZone.Id, Name = "Mevcut Çıkış QR",
                        EventType = AttendanceEventType.Exit, TokenHash = "4115505540BD420C0B4BC01B5733A1EDB910897747BA5EA9CD25E13BC597C481",
                        IsLegacy = true, IsActive = true, CreatedAt = createdAt
                    });
                await context.SaveChangesAsync();
            }
        }
    }
}
