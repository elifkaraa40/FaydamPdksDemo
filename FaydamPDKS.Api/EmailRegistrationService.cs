using FaydamPDKS.Core.DTOs.Auth;
using FaydamPDKS.Core.Enums;
using FaydamPDKS.Core.Interfaces;
using FaydamPDKS.Core.Models;
using FaydamPDKS.Data;
using Microsoft.EntityFrameworkCore;

namespace FaydamPDKS.Api;

public sealed class EmailRegistrationService(AppDbContext context, MobileTokenService tokens,
    IManagerNotificationService managerNotifications)
{
    public async Task<MobileAuthResponse> RegisterAsync(EmailRegistrationRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await context.Users.AnyAsync(x => x.Email.ToLower() == email, cancellationToken))
            throw new InvalidOperationException("EMAIL_ALREADY_REGISTERED");

        var role = await context.Roles.SingleOrDefaultAsync(x => x.Name == "Personel", cancellationToken)
            ?? throw new InvalidOperationException("PERSONNEL_ROLE_NOT_FOUND");
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = request.FullName.Trim(),
            Email = email,
            EmployeeNumber = $"PENDING-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            RoleId = role.Id,
            Role = role,
            IsActive = true,
            AccountStatus = AccountStatus.PendingApproval
        };
        context.Users.Add(user);
        await context.SaveChangesAsync(cancellationToken);
        await managerNotifications.NotifyAsync(NotificationType.RegistrationApprovalRequested, "Yeni kullanıcı onayı",
            $"{user.Name} ({user.Email}) mobil kayıt onayı bekliyor.", user.Id, cancellationToken);
        return await tokens.IssueForUserAsync(user, request.DeviceName, cancellationToken);
    }
}
