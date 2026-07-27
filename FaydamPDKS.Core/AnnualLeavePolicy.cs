namespace FaydamPDKS.Core;

public static class AnnualLeavePolicy
{
    public static int CompletedServiceYears(DateOnly hireDate, DateOnly asOf)
    {
        if (asOf < hireDate) return 0;
        var years = asOf.Year - hireDate.Year;
        if (hireDate.AddYears(years) > asOf) years--;
        return Math.Max(0, years);
    }

    public static int AgeOn(DateOnly birthDate, DateOnly date)
    {
        if (date < birthDate) return 0;
        var age = date.Year - birthDate.Year;
        if (birthDate.AddYears(age) > date) age--;
        return Math.Max(0, age);
    }

    public static int EntitlementForServiceYear(
        int completedServiceYear,
        int ageAtEntitlement)
    {
        if (completedServiceYear < 1) return 0;
        var days = completedServiceYear <= 5
            ? 14
            : completedServiceYear < 15
                ? 20
                : 26;
        if (ageAtEntitlement <= 18 || ageAtEntitlement >= 50)
            days = Math.Max(days, 20);
        return days;
    }

    public static int TotalEntitlement(
        DateOnly hireDate,
        DateOnly birthDate,
        DateOnly asOf)
    {
        var completedYears = CompletedServiceYears(hireDate, asOf);
        var total = 0;
        for (var serviceYear = 1; serviceYear <= completedYears; serviceYear++)
        {
            var entitlementDate = hireDate.AddYears(serviceYear);
            total += EntitlementForServiceYear(
                serviceYear,
                AgeOn(birthDate, entitlementDate));
        }
        return total;
    }
}
