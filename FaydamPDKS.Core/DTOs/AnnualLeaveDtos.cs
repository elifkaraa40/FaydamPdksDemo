namespace FaydamPDKS.Core.DTOs;

public sealed record AnnualLeaveBalanceDto(
    DateOnly? HireDate,
    DateOnly? BirthDate,
    int CompletedServiceYears,
    int TotalEntitledDays,
    double ApprovedUsedDays,
    double PendingDays,
    double AvailableDays,
    DateOnly? FirstEntitlementDate,
    DateOnly? NextEntitlementDate,
    bool IsEligible,
    string? InformationMessage);
