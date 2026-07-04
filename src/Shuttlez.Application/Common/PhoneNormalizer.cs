namespace Shuttlez.Application.Common;

public static class PhoneNormalizer
{
    /// <summary>
    /// يوحّد أرقام مصر إلى صيغة E.164: +201xxxxxxxxx
    /// </summary>
    public static string Normalize(string phone)
    {
        var digits = new string(phone.Where(char.IsDigit).ToArray());

        if (digits.StartsWith("20") && digits.Length == 12)
        {
            return $"+{digits}";
        }

        if (digits.StartsWith('0') && digits.Length == 11)
        {
            return $"+20{digits[1..]}";
        }

        if (digits.Length == 10 && digits.StartsWith('1'))
        {
            return $"+20{digits}";
        }

        var trimmed = phone.Trim();
        return trimmed.StartsWith('+') ? trimmed : $"+{digits}";
    }
}
