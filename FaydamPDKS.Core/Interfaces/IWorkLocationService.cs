using FaydamPDKS.Core.DTOs;
using FaydamPDKS.Core.Models;

namespace FaydamPDKS.Core.Interfaces;

public interface IWorkLocationService
{
    bool FeatureEnabled { get; }
    Task<WorkLocationPageDto> GetManagementPageAsync(CancellationToken cancellationToken = default);
    Task CreateAssignmentAsync(CreateWorkLocationAssignmentDto request, Guid actorId, CancellationToken cancellationToken = default);
    Task<bool> EndAssignmentAsync(Guid id, Guid actorId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FieldWorkRequestDto>> GetMyRequestsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task CreateFieldRequestAsync(Guid userId, CreateFieldWorkRequestDto request, CancellationToken cancellationToken = default);
    Task<bool> CancelFieldRequestAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
    Task<bool> ReviewFieldRequestAsync(Guid id, Guid reviewerId, bool approve, string? note, CancellationToken cancellationToken = default);
    Task<WorkLocationAssignment?> GetForDateAsync(Guid userId, DateOnly date, CancellationToken cancellationToken = default);
}
