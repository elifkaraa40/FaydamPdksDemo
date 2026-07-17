using System.Security.Cryptography;
using FaydamPDKS.Core.DTOs.Auth;
using FaydamPDKS.Core.Enums;
using FaydamPDKS.Core.Interfaces;
using FaydamPDKS.Core.Models;
using FaydamPDKS.Data;
using Microsoft.EntityFrameworkCore;

namespace FaydamPDKS.Api;

public sealed class PhoneAuthService(AppDbContext context, MobileTokenService tokens,
    IManagerNotificationService managerNotifications)
{
    public async Task<MobileAuthResponse> RegisterAsync(PhoneRegistrationRequest request, CancellationToken cancellationToken)
    {
        var phone = NormalizePhone(request.PhoneNumber);
        if (await context.Users.AnyAsync(x => x.PhoneNumber == phone, cancellationToken))
            throw new InvalidOperationException("PHONE_ALREADY_REGISTERED");

        var role = await context.Roles.SingleOrDefaultAsync(x => x.Name == "Personel", cancellationToken)
            ?? throw new InvalidOperationException("PERSONNEL_ROLE_NOT_FOUND");
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = request.FullName.Trim(),
            PhoneNumber = phone,
            Email = $"pending-{Guid.NewGuid():N}@phone.local",
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
            $"{user.Name} ({user.PhoneNumber}) mobil kayıt onayı bekliyor.", user.Id, cancellationToken);
        return await tokens.IssueForUserAsync(user, request.DeviceName, cancellationToken);
    }

    public async Task<MobileAuthResponse?> LoginAsync(PhonePasswordLoginRequest request, CancellationToken cancellationToken)
    {
        var phone = NormalizePhone(request.PhoneNumber);
        var user = await context.Users.Include(x => x.Role).SingleOrDefaultAsync(x => x.PhoneNumber == phone, cancellationToken);
        if (user is null || !user.IsActive || user.AccountStatus is AccountStatus.Rejected or AccountStatus.Suspended
            || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash)) return null;
        return await tokens.IssueForUserAsync(user, request.DeviceName, cancellationToken);
    }

    private static string NormalizePhone(string value)
    {
        var digits = new string(value.Where(char.IsDigit).ToArray());
        if (digits.StartsWith("90") && digits.Length == 12) return $"+{digits}";
        if (digits.StartsWith('0') && digits.Length == 11) return $"+90{digits[1..]}";
        if (digits.Length == 10 && digits.StartsWith('5')) return $"+90{digits}";
        throw new ArgumentException("Geçerli bir Türkiye telefon numarası girin.");
    }
}
