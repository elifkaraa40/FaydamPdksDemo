namespace FaydamPDKS.Core.Security;

public static class PhoneNumberNormalizer
{
    public static string NormalizeTurkishMobile(string value)
    {
        var digits = new string((value ?? string.Empty).Where(char.IsDigit).ToArray());
        if (digits.StartsWith("90") && digits.Length == 12 && digits[2] == '5') return $"+{digits}";
        if (digits.StartsWith('0') && digits.Length == 11 && digits[1] == '5') return $"+90{digits[1..]}";
        if (digits.Length == 10 && digits.StartsWith('5')) return $"+90{digits}";
        throw new ArgumentException("Geçerli bir Türkiye cep telefonu numarası girin.");
    }

    public static string? NormalizeOptionalTurkishMobile(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : NormalizeTurkishMobile(value);
}
