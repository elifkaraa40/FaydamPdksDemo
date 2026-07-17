using FaydamPDKS.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FaydamPDKS.Data;

public static class DependencyInjection
{
    public static IServiceCollection AddPdksData(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("ConnectionStrings:DefaultConnection yapılandırılmalıdır.");

        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IAccessLogRepository, AccessLogRepository>();
        services.AddScoped<ILeaveRequestRepository, LeaveRequestRepository>();
        services.AddScoped<IDashboardQueryService, DashboardQueryService>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IShiftResolver, ShiftResolver>();
        services.AddScoped<IShiftAdminRepository, ShiftAdminRepository>();
        services.AddScoped<IAttendanceReportService, AttendanceReportService>();
        services.AddScoped<IAuditTrail, AuditTrail>();
        services.AddScoped<IAttendanceCorrectionRepository, AttendanceCorrectionRepository>();
        services.AddScoped<IOrganizationRepository, OrganizationRepository>();
        services.AddScoped<IWorkCalendarResolver, WorkCalendarResolver>();
        services.AddScoped<IWorkCalendarRepository, WorkCalendarRepository>();
        services.AddScoped<IAttendanceTerminalService, AttendanceTerminalService>();
        services.AddScoped<IAttendanceQrService, AttendanceQrService>();
        services.AddScoped<IPersonalDataExportService, PersonalDataExportService>();
        services.AddScoped<IBreakService, BreakService>();
        services.AddScoped<IManagerNotificationService, ManagerNotificationService>();
        services.AddScoped<IWorkLocationService, WorkLocationService>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        return services;
    }
}
