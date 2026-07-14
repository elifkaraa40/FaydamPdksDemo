using FaydamPDKS.Core.DTOs;
using FaydamPDKS.Core.Interfaces;
using FaydamPDKS.Core.Models;

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
        user.PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim();
        user.IsEmailNotificationEnabled = request.IsEmailNotificationEnabled;
        user.IsSmsNotificationEnabled = request.IsSmsNotificationEnabled;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(user);
    }

    private static MobileProfileDto Map(User x) => new(
        x.Id, x.Name, x.Email, x.Role?.Name ?? "Personel", x.PhoneNumber,
        x.ProfileImageUrl, x.IsEmailNotificationEnabled, x.IsSmsNotificationEnabled);
}
