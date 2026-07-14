using FaydamPDKS.Core.DTOs;
using FaydamPDKS.Core.Enums;
using FaydamPDKS.Core.Interfaces;
using FaydamPDKS.Core.Models;

namespace FaydamPDKS.Web;

public sealed class WebWorkCalendarAdminService(
    IWorkCalendarRepository calendar,
    IOrganizationRepository organizations,
    IUnitOfWork unitOfWork) : IWorkCalendarAdminService
{
    public async Task<WorkCalendarPageDto> GetPageAsync(CancellationToken cancellationToken = default)
    {
        var days = (await calendar.GetAllAsync(cancellationToken)).Select(x => new WorkCalendarDayListItemDto(
            x.Id, x.WorkplaceId, x.Workplace?.Name ?? "Tüm işyerleri", x.Date, x.Name, x.DayType)).ToArray();
        var workplaces = (await organizations.GetWorkplacesAsync(cancellationToken)).Where(x => x.IsActive)
            .Select(x => new WorkplaceOptionDto(x.Id, x.Code, x.Name)).ToArray();
        return new(days, workplaces);
    }

    public async Task CreateAsync(CreateWorkCalendarDayDto request, CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(request.DayType)) throw new InvalidOperationException("Geçerli gün tipi seçin.");
        if (request.WorkplaceId.HasValue && !await organizations.ActiveWorkplaceExistsAsync(request.WorkplaceId.Value, cancellationToken))
            throw new InvalidOperationException("Aktif işyeri bulunamadı.");
        if (await calendar.ExistsAsync(request.WorkplaceId, request.Date, cancellationToken))
            throw new InvalidOperationException("Bu kapsam ve tarih için zaten bir takvim kaydı var.");
        await calendar.AddAsync(new WorkCalendarDay { Id = Guid.NewGuid(), WorkplaceId = request.WorkplaceId, Date = request.Date, Name = request.Name.Trim(), DayType = request.DayType }, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
