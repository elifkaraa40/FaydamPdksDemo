using FaydamPDKS.Core.DTOs;

namespace FaydamPDKS.Core.Interfaces;

public interface IMobileProfileService
{
    Task<MobileProfileDto?> GetAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<MobileProfileDto?> UpdateAsync(Guid userId, UpdateMobileProfileDto request, CancellationToken cancellationToken = default);
}
