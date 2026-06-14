namespace Elektrika.Infrastructure.Utilities;

public static class PhoneNormalizer
{
    public static string Normalize(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            throw new ArgumentException("Phone is required.");
        }

        var digits = new string(phone.Where(char.IsDigit).ToArray());

        if (digits.Length == 11 && digits[0] == '8')
        {
            digits = "7" + digits[1..];
        }
        else if (digits.Length == 10)
        {
            digits = "7" + digits;
        }

        if (digits.Length != 11 || digits[0] != '7')
        {
            throw new ArgumentException("Укажите корректный номер телефона.");
        }

        return $"+{digits}";
    }
}
