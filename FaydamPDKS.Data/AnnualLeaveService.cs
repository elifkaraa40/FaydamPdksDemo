using FaydamPDKS.Core;
using FaydamPDKS.Core.DTOs;
using FaydamPDKS.Core.Enums;
using FaydamPDKS.Core.Exceptions;
using FaydamPDKS.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace FaydamPDKS.Data;

public sealed class AnnualLeaveService(
    AppDbContext context,
    IWorkCalendarResolver workCalendar,
    TimeProvider timeProvider) : IAnnualLeaveService
{
    public async Task<AnnualLeaveBalanceDto> GetBalanceAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);
        var user = await context.Users.AsNoTracking()
            .Where(x => x.Id == userId)
            .Select(x => new
            {
                x.HireDate,
                x.BirthDate
            })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new AnnualLeaveException("EMPLOYEE_NOT_FOUND", "Personel kaydı bulunamadı.");

        if (!user.HireDate.HasValue || !user.BirthDate.HasValue)
        {
            var missing = !user.HireDate.HasValue && !user.BirthDate.HasValue
                ? "işe giriş ve doğum tarihiniz"
                : !user.HireDate.HasValue
                    ? "işe giriş tarihiniz"
                    : "doğum tarihiniz";
            return new(
                user.HireDate,
                user.BirthDate,
                0,
                0,
                0,
                0,
                0,
                user.HireDate?.AddYears(1),
                user.HireDate?.AddYears(1),
                false,
                $"Yıllık izin hakkınız hesaplanamıyor; {missing} personel kaydında eksik. Lütfen yöneticinizle iletişime geçin.");
        }

        var hireDate = user.HireDate.Value;
        var birthDate = user.BirthDate.Value;
        var completedYears = AnnualLeavePolicy.CompletedServiceYears(hireDate, today);
        var totalEntitlement = AnnualLeavePolicy.TotalEntitlement(
            hireDate,
            birthDate,
            today);
        var requests = await context.LeaveRequests.AsNoTracking()
            .Where(x => x.UserId == userId
                        && x.LeaveType == LeaveType.Annual
                        && (x.Status == LeaveRequestStatus.Approved
                            || x.Status == LeaveRequestStatus.Pending))
            .ToListAsync(cancellationToken);

        var approved = 0d;
        var pending = 0d;
        foreach (var request in requests)
        {
            var days = await CalculateDaysAsync(
                userId,
                request.StartDate,
                request.EndDate,
                request.DayPortion,
                cancellationToken);
            if (request.Status == LeaveRequestStatus.Approved)
                approved += days;
            else
                pending += days;
        }

        var firstEntitlement = hireDate.AddYears(1);
        var nextEntitlement = hireDate.AddYears(completedYears + 1);
        var eligible = completedYears >= 1;
        var available = Math.Max(0, totalEntitlement - approved - pending);
        var information = eligible
            ? pending > 0
                ? $"{FormatDays(pending)} günlük bekleyen yıllık izin talebiniz bakiyeden ayrılmıştır."
                : null
            : $"Yıllık ücretli izin hakkınız henüz oluşmadı. İlk hak ediş tarihiniz {firstEntitlement:dd.MM.yyyy}.";
        return new(
            hireDate,
            birthDate,
            completedYears,
            totalEntitlement,
            approved,
            pending,
            available,
            firstEntitlement,
            nextEntitlement,
            eligible,
            information);
    }

    public async Task<double> EnsureCanRequestAsync(
        Guid userId,
        DateOnly startDate,
        DateOnly endDate,
        LeaveDayPortion dayPortion,
        CancellationToken cancellationToken = default)
    {
        var balance = await GetBalanceAsync(userId, cancellationToken);
        if (!balance.HireDate.HasValue || !balance.BirthDate.HasValue)
            throw new AnnualLeaveException(
                "ANNUAL_LEAVE_PROFILE_INCOMPLETE",
                balance.InformationMessage ?? "Yıllık izin hakkınız için personel bilgileriniz tamamlanmalıdır.");
        if (!balance.IsEligible)
            throw new AnnualLeaveException(
                "ANNUAL_LEAVE_NOT_EARNED",
                $"Yıllık ücretli izin hakkınız henüz oluşmadı. İşe giriş tarihiniz {balance.HireDate:dd.MM.yyyy}; ilk hak ediş tarihiniz {balance.FirstEntitlementDate:dd.MM.yyyy}.");

        var requestedDays = await CalculateDaysAsync(
            userId,
            startDate,
            endDate,
            dayPortion,
            cancellationToken);
        if (requestedDays <= 0)
            throw new AnnualLeaveException(
                "ANNUAL_LEAVE_NO_WORKDAYS",
                "Seçtiğiniz tarihler yıllık izin bakiyesinden düşülecek bir çalışma günü içermiyor. Lütfen tarihleri kontrol edin.");
        if (requestedDays > balance.AvailableDays + .001)
            throw new AnnualLeaveException(
                "ANNUAL_LEAVE_BALANCE_INSUFFICIENT",
                $"Talebiniz {FormatDays(requestedDays)} gün, kullanılabilir yıllık izin bakiyeniz {FormatDays(balance.AvailableDays)} gün. Daha kısa bir tarih aralığı seçin.");
        return requestedDays;
    }

    private async Task<double> CalculateDaysAsync(
        Guid userId,
        DateOnly startDate,
        DateOnly endDate,
        LeaveDayPortion dayPortion,
        CancellationToken cancellationToken)
    {
        var count = 0d;
        for (var date = startDate; date <= endDate; date = date.AddDays(1))
            count += (await workCalendar.ResolveAsync(userId, date, cancellationToken)).WorkdayWeight;
        return dayPortion == LeaveDayPortion.FullDay ? count : Math.Min(.5, count);
    }

    private static string FormatDays(double value) =>
        value.ToString("0.#", CultureInfo.GetCultureInfo("tr-TR"));
}
