using FaydamPDKS.Core.DTOs;
using FaydamPDKS.Core.Enums;

namespace FaydamPDKS.Core.Interfaces;

public interface IAnnualLeaveService
{
    Task<AnnualLeaveBalanceDto> GetBalanceAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<double> EnsureCanRequestAsync(
        Guid userId,
        DateOnly startDate,
        DateOnly endDate,
        LeaveDayPortion dayPortion,
        CancellationToken cancellationToken = default);
}
