using System.ComponentModel.DataAnnotations;

namespace FaydamPDKS.Web.Models;

public sealed record AccountNotificationItem(string Title, string Message, DateTimeOffset CreatedAt, bool IsRead);

public sealed record AccountViewModel(
    string ActiveSection,
    Guid Id,
    string FullName,
    string Email,
    string EmployeeNumber,
    string Role,
    string? ProfileImageUrl,
    string? PhoneNumber,
    string? Department,
    string? Workplace,
    DateOnly? HireDate,
    bool IsEmailNotificationEnabled,
    bool IsSmsNotificationEnabled,
    string Theme,
    string Language,
    IReadOnlyList<AccountNotificationItem> Notifications)
{
    public bool IsAdmin => string.Equals(Role, "Yonetici", StringComparison.OrdinalIgnoreCase);
    public int UnreadNotificationCount => Notifications.Count(item => !item.IsRead);
    public string Initials => string.Join("", FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(part => char.ToUpperInvariant(part[0])));
}

public sealed class UpdateOwnProfileModel
{
    [Required, StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Phone, StringLength(30)]
    public string? PhoneNumber { get; set; }
}

public sealed class ChangeOwnPasswordModel
{
    [Required, DataType(DataType.Password)]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required, MinLength(6), DataType(DataType.Password)]
    public string NewPassword { get; set; } = string.Empty;

    [Required, Compare(nameof(NewPassword)), DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public sealed class UpdateOwnPreferencesModel
{
    public string ReturnSection { get; set; } = "appearance";

    [Required]
    public string Theme { get; set; } = "light";

    [Required]
    public string Language { get; set; } = "tr";

    public bool EmailNotifications { get; set; }
    public bool SmsNotifications { get; set; }
}
