using System.Globalization;

namespace PrimeBakesLibrary.Data.Common;

public static class Helper
{
    public static string RemoveSpace(this string str) =>
        str.Replace(" ", "");

    public static string FormatIndianCurrency(this decimal rate) =>
        string.Format(new CultureInfo("hi-IN"), "{0:C}", rate);

    public static string FormatIndianCurrency(this decimal? rate)
    {
        rate ??= 0;
        return string.Format(new CultureInfo("hi-IN"), "{0:C}", rate);
    }

    public static string FormatIndianCurrency(this int rate) =>
        string.Format(new CultureInfo("hi-IN"), "{0:C}", rate);

    public static string FormatDecimalWithTwoDigits(this decimal value) =>
        value.ToString("0.00", CultureInfo.InvariantCulture);

    /// <summary>
    /// Formats decimal smartly: shows integer if no decimal part (2.0 -> "2"), 
    /// otherwise shows 2 decimal places (2.05 -> "2.05", 2.5666 -> "2.57")
    /// </summary>
    public static string FormatSmartDecimal(this decimal value)
    {
        // Round to 2 decimal places
        decimal rounded = Math.Round(value, 2);

        // Check if the decimal part is zero
        if (rounded == Math.Floor(rounded))
            // No decimal part, show as integer
            return rounded.ToString("0", CultureInfo.InvariantCulture);
        else
            // Has decimal part, show 2 decimal places
            return rounded.ToString("0.##", CultureInfo.InvariantCulture);
    }

    public static bool ValidatePhoneNumber(this string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return false;
        if (phoneNumber.Length != 10)
            return false;
        return long.TryParse(phoneNumber, out _);
    }

    public static bool ValidateEmail(this string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}