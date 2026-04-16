using System.Globalization;
using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Models.Accounts.Masters;

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

    public static async Task<(DateTime FromDate, DateTime ToDate)> ResolveDateRange(DateRangeType rangeType, DateTime referenceFromDate, DateTime referenceToDate)
    {
        var today = await CommonData.LoadCurrentDateTime();
        var currentYear = today.Year;
        var currentMonth = today.Month;

        DateTime newFromDate = referenceFromDate;
        DateTime newToDate = referenceToDate;

        switch (rangeType)
        {
            case DateRangeType.Today:
                newFromDate = today;
                newToDate = today;
                break;

            case DateRangeType.Yesterday:
                newFromDate = referenceFromDate.AddDays(-1);
                newToDate = referenceToDate.AddDays(-1);
                break;

            case DateRangeType.NextDay:
                newFromDate = referenceFromDate.AddDays(1);
                newToDate = referenceToDate.AddDays(1);
                break;

            case DateRangeType.CurrentMonth:
                newFromDate = new DateTime(currentYear, currentMonth, 1);
                newToDate = newFromDate.AddMonths(1).AddDays(-1);
                break;

            case DateRangeType.PreviousMonth:
                newFromDate = new DateTime(newFromDate.Year, newFromDate.Month, 1).AddMonths(-1);
                newToDate = newFromDate.AddMonths(1).AddDays(-1);
                break;

            case DateRangeType.NextMonth:
                newFromDate = new DateTime(newFromDate.Year, newFromDate.Month, 1).AddMonths(1);
                newToDate = newFromDate.AddMonths(1).AddDays(-1);
                break;

            case DateRangeType.CurrentFinancialYear:
                var currentFY = await FinancialYearData.LoadFinancialYearByDateTime(today);
                newFromDate = currentFY.StartDate.ToDateTime(TimeOnly.MinValue);
                newToDate = currentFY.EndDate.ToDateTime(TimeOnly.MinValue);
                break;

            case DateRangeType.PreviousFinancialYear:
                var currentFY2 = await FinancialYearData.LoadFinancialYearByDateTime(newFromDate);
                if (currentFY2 is null)
                    return (referenceFromDate, referenceToDate);

                var financialYears = await CommonData.LoadTableDataByStatus<FinancialYearModel>(TableNames.FinancialYear);
                var previousFY = financialYears
                    .Where(fy => fy.EndDate < currentFY2.StartDate)
                    .OrderByDescending(fy => fy.StartDate)
                    .FirstOrDefault();

                if (previousFY is null)
                    return (referenceFromDate, referenceToDate);

                newFromDate = previousFY.StartDate.ToDateTime(TimeOnly.MinValue);
                newToDate = previousFY.EndDate.ToDateTime(TimeOnly.MinValue);
                break;

            case DateRangeType.NextFinancialYear:
                var currentFY3 = await FinancialYearData.LoadFinancialYearByDateTime(newFromDate);
                if (currentFY3 is null)
                    return (referenceFromDate, referenceToDate);

                var financialYears2 = await CommonData.LoadTableDataByStatus<FinancialYearModel>(TableNames.FinancialYear);
                var nextFY = financialYears2
                    .Where(fy => fy.StartDate > currentFY3.EndDate)
                    .OrderBy(fy => fy.StartDate)
                    .FirstOrDefault();

                if (nextFY is null)
                    return (referenceFromDate, referenceToDate);

                newFromDate = nextFY.StartDate.ToDateTime(TimeOnly.MinValue);
                newToDate = nextFY.EndDate.ToDateTime(TimeOnly.MinValue);
                break;

            case DateRangeType.AllTime:
                newFromDate = new DateTime(2000, 1, 1);
                newToDate = new DateTime(2100, 1, 1);
                break;
        }

        return (newFromDate, newToDate);
    }
}