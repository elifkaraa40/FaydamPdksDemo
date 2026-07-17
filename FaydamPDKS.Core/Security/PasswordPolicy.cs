namespace FaydamPDKS.Core.Security;

public static class PasswordPolicy
{
    public const int MinimumLength = 8;
    public const int MaximumLength = 72;
    public const string RequirementMessage = "Parola en az 8 karakter olmalıdır.";
}
