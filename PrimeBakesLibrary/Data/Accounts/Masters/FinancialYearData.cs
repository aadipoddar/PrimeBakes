using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Models.Accounts.Masters;

namespace PrimeBakesLibrary.Data.Accounts.Masters;

public static class FinancialYearData
{
    public static async Task<int> InsertFinancialYear(FinancialYearModel financialYear) =>
        (await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertFinancialYear, financialYear)).FirstOrDefault();

    public static async Task<FinancialYearModel> LoadFinancialYearByDateTime(DateTime TransactionDateTime, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
        (await SqlDataAccess.LoadData<FinancialYearModel, dynamic>(StoredProcedureNames.LoadFinancialYearByDateTime, new { TransactionDateTime }, sqlDataAccessTransaction)).FirstOrDefault();

    public static async Task ValidateFinancialYear(DateTime TransactionDateTime, SqlDataAccessTransaction sqlDataAccessTransaction = null)
    {
        var financialYear = await LoadFinancialYearByDateTime(TransactionDateTime, sqlDataAccessTransaction) ??
            throw new InvalidOperationException("No financial year found for the given date.");

        if (financialYear.Locked)
            throw new InvalidOperationException("The financial year for the given date is locked");

        if (!financialYear.Status)
            throw new InvalidOperationException("The financial year for the given date is inactive.");
    }

	public static async Task<(DateTime FromDate, DateTime ToDate)> GetDateRange(DateRangeType rangeType, DateTime referenceFromDate, DateTime referenceToDate)
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