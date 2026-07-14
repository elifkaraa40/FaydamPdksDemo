using FaydamPDKS.Core.DTOs;

namespace FaydamPDKS.Core.Interfaces;

public interface IPersonalDataExportService
{
    Task<PersonalDataExportDto?> ExportAsync(Guid userId, CancellationToken cancellationToken = default);
}
