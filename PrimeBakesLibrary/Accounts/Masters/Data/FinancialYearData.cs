using PrimeBakesLibrary.Accounts.Masters.Models;
using PrimeBakesLibrary.Common;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Operations.AuditTrail.Data;
using PrimeBakesLibrary.Operations.AuditTrail.Models;

namespace PrimeBakesLibrary.Accounts.Masters.Data;

public static class FinancialYearData
{
	private static async Task<int> InsertFinancialYear(FinancialYearModel financialYear, SqlDataAccessTransaction transaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(AccountNames.InsertFinancialYear, financialYear, transaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert Financial Year.");

	public static async Task DeleteTransaction(FinancialYearModel financialYear, int userId, string platform) =>
		await SqlDataAccessTransaction.Run(async transaction =>
		{
			financialYear.Status = false;
			await InsertFinancialYear(financialYear, transaction);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Delete.ToString(),
				TableName = AccountNames.FinancialYear,
				RecordNo = $"FY{financialYear.YearNo}",
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
		});

	public static async Task RecoverTransaction(FinancialYearModel financialYear, int userId, string platform) =>
		await SqlDataAccessTransaction.Run(async transaction =>
		{
			financialYear.Status = true;
			await InsertFinancialYear(financialYear, transaction);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Recover.ToString(),
				TableName = AccountNames.FinancialYear,
				RecordNo = $"FY{financialYear.YearNo}",
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
		});

	public static async Task<FinancialYearModel> LoadFinancialYearByDateTime(DateTime TransactionDateTime, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		(await SqlDataAccess.LoadData<FinancialYearModel, dynamic>(AccountNames.LoadFinancialYearByDateTime, new { TransactionDateTime }, sqlDataAccessTransaction)).FirstOrDefault();

	public static async Task ValidateFinancialYear(DateTime TransactionDateTime, SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		var financialYear = await LoadFinancialYearByDateTime(TransactionDateTime, sqlDataAccessTransaction) ??
			throw new InvalidOperationException("No financial year found for the given date.");

		if (financialYear.Locked)
			throw new InvalidOperationException("The financial year for the given date is locked");

		if (!financialYear.Status)
			throw new InvalidOperationException("The financial year for the given date is inactive.");
	}

	private static async Task ValidateTransaction(FinancialYearModel item)
	{
		item.Remarks = string.IsNullOrWhiteSpace(item.Remarks) ? null : item.Remarks.Trim();
		item.Status = true;

		if (item.StartDate == default)
			throw new Exception("Start date is required. Please select a valid start date.");

		if (item.EndDate == default)
			throw new Exception("End date is required. Please select a valid end date.");

		if (item.EndDate <= item.StartDate)
			throw new Exception("End date must be after start date. Please select a valid end date.");

		if (item.YearNo <= 0)
			throw new Exception("Year number must be greater than 0. Please enter a valid year number.");

		var allFinancialYears = await CommonData.LoadTableData<FinancialYearModel>(AccountNames.FinancialYear);

		var overlapping = allFinancialYears.FirstOrDefault(x =>
			x.Id != item.Id &&
			((x.StartDate <= item.StartDate && x.EndDate >= item.StartDate) ||
			 (x.StartDate <= item.EndDate && x.EndDate >= item.EndDate) ||
			 (item.StartDate <= x.StartDate && item.EndDate >= x.EndDate)));

		if (overlapping is not null)
			throw new Exception($"Date range overlaps with existing financial year ({overlapping.StartDate:dd-MMM-yyyy} to {overlapping.EndDate:dd-MMM-yyyy}).");
	}

	public static async Task<int> SaveTransaction(FinancialYearModel financialYear, int userId, string platform)
	{
		await ValidateTransaction(financialYear);

		var isUpdate = financialYear.Id > 0;
		var previous = isUpdate
			? await CommonData.LoadTableDataById<FinancialYearModel>(AccountNames.FinancialYear, financialYear.Id)
			: null;

		return await SqlDataAccessTransaction.Run(async transaction =>
		{
			var id = await InsertFinancialYear(financialYear, transaction);
			var diff = AuditTrailData.GetDifference(previous, financialYear);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = isUpdate ? AuditTrailActionTypes.Update.ToString() : AuditTrailActionTypes.Insert.ToString(),
				TableName = AccountNames.FinancialYear,
				RecordNo = $"FY{financialYear.YearNo}",
				RecordValue = diff,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
			return id;
		});
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
				var currentFY = await LoadFinancialYearByDateTime(today);
				newFromDate = currentFY.StartDate.ToDateTime(TimeOnly.MinValue);
				newToDate = currentFY.EndDate.ToDateTime(TimeOnly.MinValue);
				break;

			case DateRangeType.PreviousFinancialYear:
				var currentFY2 = await LoadFinancialYearByDateTime(newFromDate);
				if (currentFY2 is null)
					return (referenceFromDate, referenceToDate);

				var financialYears = await CommonData.LoadTableDataByStatus<FinancialYearModel>(AccountNames.FinancialYear);
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
				var currentFY3 = await LoadFinancialYearByDateTime(newFromDate);
				if (currentFY3 is null)
					return (referenceFromDate, referenceToDate);

				var financialYears2 = await CommonData.LoadTableDataByStatus<FinancialYearModel>(AccountNames.FinancialYear);
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
