using FaydamPDKS.Core.DTOs;

namespace FaydamPDKS.Core.Interfaces;

public interface IDashboardQueryService
{
    Task<DashboardDto> GetAsync(CancellationToken cancellationToken = default);
}
