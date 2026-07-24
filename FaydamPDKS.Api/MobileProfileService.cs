using FaydamPDKS.Core.DTOs;
using FaydamPDKS.Core.Interfaces;
using FaydamPDKS.Core.Models;
using FaydamPDKS.Core.Security;

namespace FaydamPDKS.Api;

public sealed class MobileProfileService(IUserRepository users, IUnitOfWork unitOfWork) : IMobileProfileService
{
    public async Task<MobileProfileDto?> GetAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await users.GetByIdWithRoleAsync(userId, false, cancellationToken);
        return user is null ? null : Map(user);
    }

    public async Task<MobileProfileDto?> UpdateAsync(Guid userId, UpdateMobileProfileDto request, CancellationToken cancellationToken = default)
    {
        var user = await users.GetByIdWithRoleAsync(userId, true, cancellationToken);
        if (user is null) return null;
        var phone = PhoneNumberNormalizer.NormalizeOptionalTurkishMobile(request.PhoneNumber);
        if (phone is not null && await users.PhoneNumberExistsAsync(phone, userId, cancellationToken))
            throw new InvalidOperationException("PHONE_ALREADY_REGISTERED");
        user.PhoneNumber = phone;
        user.IsEmailNotificationEnabled = request.IsEmailNotificationEnabled;
        user.IsSmsNotificationEnabled = request.IsSmsNotificationEnabled;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(user);
    }

    public async Task<bool> ChangePasswordAsync(
        Guid userId,
        ChangeMobilePasswordDto request,
        CancellationToken cancellationToken = default)
    {
        var user = await users.GetByIdWithRoleAsync(userId, true, cancellationToken);
        if (user is null) return false;
        if (!string.Equals(request.NewPassword, request.NewPasswordConfirmation, StringComparison.Ordinal))
            throw new InvalidOperationException("PASSWORD_CONFIRMATION_MISMATCH");
        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            throw new InvalidOperationException("CURRENT_PASSWORD_INVALID");
        if (BCrypt.Net.BCrypt.Verify(request.NewPassword, user.PasswordHash))
            throw new InvalidOperationException("PASSWORD_REUSE_NOT_ALLOWED");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static MobileProfileDto Map(User x) => new(
        x.Id, x.Name, x.Email, x.Role?.Name ?? "Personel", x.PhoneNumber,
        x.ProfileImageUrl, x.IsEmailNotificationEnabled, x.IsSmsNotificationEnabled,
        string.IsNullOrWhiteSpace(x.EmployeeNumber) ? null : x.EmployeeNumber,
        x.Department?.Name ?? x.DepartmentLegacy, x.Workplace?.Name, x.HireDate);
}
