using System.Globalization;

namespace PrimeBakes.Library.Common;

public static class Helper
{
	#region Formats
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

	// Whole rupees only — for headline figures where the paise are noise.
	public static string FormatIndianCurrencyShort(this decimal rate) =>
		string.Format(new CultureInfo("hi-IN"), "{0:C0}", rate);

	public static string FormatIndianCurrencyShort(this decimal? rate) =>
		string.Format(new CultureInfo("hi-IN"), "{0:C0}", rate ?? 0);

	public static string FormatIndianCurrencyShort(this int rate) =>
		string.Format(new CultureInfo("hi-IN"), "{0:C0}", rate);

	public static string FormatDecimalWithTwoDigits(this decimal value) =>
		value.ToString("0.00", CultureInfo.InvariantCulture);

	// shows integer if no decimal part otherwise shows 2 decimal places (2.0 -> "2", 2.05 -> "2.05", 2.5666 -> "2.57")
	public static string FormatSmartDecimal(this decimal value)
	{
		decimal rounded = Math.Round(value, 2);

		if (rounded == Math.Floor(rounded))
			return rounded.ToString("0", CultureInfo.InvariantCulture);
		else
			return rounded.ToString("0.##", CultureInfo.InvariantCulture);
	}

	public static string FormatMonthlyTrend(decimal current, decimal previous) =>
		FormatTrend(current, previous, "last month");

	public static string FormatTrend(decimal current, decimal previous, string comparedTo)
	{
		if (previous == 0)
			return $"vs {comparedTo}";

		// Divide by the magnitude of the previous value so the ▲/▼ direction stays
		// correct even when the previous value is negative (e.g. a prior-month loss).
		var change = (double)((current - previous) / Math.Abs(previous)) * 100;
		return change switch
		{
			> 0 => $"▲ {change:0}% vs {comparedTo}",
			< 0 => $"▼ {Math.Abs(change):0}% vs {comparedTo}",
			_ => $"same as {comparedTo}"
		};
	}
	#endregion

	#region Validation
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
		catch { return false; }
	}
	#endregion
}