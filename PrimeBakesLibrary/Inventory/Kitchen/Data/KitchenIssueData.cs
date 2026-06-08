using PrimeBakesLibrary.Accounts.Masters.Data;
using PrimeBakesLibrary.Common;
using PrimeBakesLibrary.Inventory.Kitchen.Exports;
using PrimeBakesLibrary.Inventory.Kitchen.Models;
using PrimeBakesLibrary.Inventory.Stock.Data;
using PrimeBakesLibrary.Inventory.Stock.Models;
using PrimeBakesLibrary.Operations.AuditTrail;
using PrimeBakesLibrary.Operations.User;
using PrimeBakesLibrary.Utils.Exports;
using PrimeBakesLibrary.Utils.Mail;

namespace PrimeBakesLibrary.Inventory.Kitchen.Data;

public static class KitchenIssueData
{
	private static async Task<int> InsertKitchenIssue(KitchenIssueModel kitchenIssue, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(InventoryNames.InsertKitchenIssue, kitchenIssue, sqlDataAccessTransaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert Kitchen Issue.");

	private static async Task<int> InsertKitchenIssueDetail(KitchenIssueDetailModel kitchenIssueDetail, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(InventoryNames.InsertKitchenIssueDetail, kitchenIssueDetail, sqlDataAccessTransaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert Kitchen Issue Detail.");

	public static List<KitchenIssueDetailModel> ConvertCartToDetails(List<KitchenIssueItemCartModel> cart, int masterId = 0) =>
		[.. cart.Select(item => new KitchenIssueDetailModel
		{
			Id = 0,
			MasterId = masterId,
			RawMaterialId = item.ItemId,
			Quantity = item.Quantity,
			UnitOfMeasurement = item.UnitOfMeasurement,
			Rate = item.Rate,
			Total = item.Total,
			Remarks = item.Remarks,
			Status = true
		})];

	#region Delete
	public static async Task DeleteTransaction(KitchenIssueModel kitchenIssue, SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		if (sqlDataAccessTransaction is null)
		{
			await SqlDataAccessTransaction.Run(transaction => DeleteTransaction(kitchenIssue, transaction));
			await KitchenIssueNotify.Notify(kitchenIssue.Id, NotifyType.Deleted);
			return;
		}

		await FinancialYearData.ValidateFinancialYear(kitchenIssue.TransactionDateTime, sqlDataAccessTransaction);

		kitchenIssue.Status = false;
		await InsertKitchenIssue(kitchenIssue, sqlDataAccessTransaction);
		await RawMaterialStockData.DeleteRawMaterialStockByTypeTransactionId(nameof(StockType.KitchenIssue), kitchenIssue.Id, sqlDataAccessTransaction);

		await AuditTrailData.SaveAuditTrail(new()
		{
			Action = AuditTrailActionTypes.Delete.ToString(),
			TableName = InventoryNames.KitchenIssue,
			RecordNo = kitchenIssue.TransactionNo,
			CreatedBy = kitchenIssue.LastModifiedBy.Value,
			CreatedFromPlatform = kitchenIssue.LastModifiedFromPlatform
		}, sqlDataAccessTransaction);
	}
	#endregion

	public static async Task RecoverTransaction(KitchenIssueModel kitchenIssue)
	{
		kitchenIssue.Status = true;
		var kitchenIssueDetails = await CommonData.LoadTableDataByMasterId<KitchenIssueDetailModel>(InventoryNames.KitchenIssueDetail, kitchenIssue.Id);
		await SaveTransaction(kitchenIssue, kitchenIssueDetails, true);

		await KitchenIssueNotify.Notify(kitchenIssue.Id, NotifyType.Recovered);
	}

	#region Save
	private static async Task<KitchenIssueModel> ValidateTransaction(KitchenIssueModel kitchenIssue, bool update, SqlDataAccessTransaction sqlDataAccessTransaction)
	{
		kitchenIssue.Remarks = string.IsNullOrWhiteSpace(kitchenIssue.Remarks) ? null : kitchenIssue.Remarks.Trim();

		if (kitchenIssue.CompanyId <= 0)
			throw new InvalidOperationException("Please select a company for the transaction.");

		if (kitchenIssue.KitchenId <= 0)
			throw new InvalidOperationException("Please select a kitchen for the transaction.");

		if (kitchenIssue.TotalItems <= 0)
			throw new InvalidOperationException("The total number of items in the transaction must be greater than zero.");

		if (kitchenIssue.TotalQuantity <= 0)
			throw new InvalidOperationException("The total quantity of items in the transaction must be greater than zero.");

		if (kitchenIssue.TotalAmount < 0)
			throw new InvalidOperationException("The total amount of the transaction cannot be negative.");

		if (!update)
			kitchenIssue.TransactionNo = await GenerateCodes.GenerateKitchenIssueTransactionNo(kitchenIssue, sqlDataAccessTransaction);

		await FinancialYearData.ValidateFinancialYear(kitchenIssue.TransactionDateTime, sqlDataAccessTransaction);

		if (update)
		{
			var existingKitchenIssue = await CommonData.LoadTableDataById<KitchenIssueModel>(InventoryNames.KitchenIssue, kitchenIssue.Id, sqlDataAccessTransaction)
				?? throw new InvalidOperationException("The kitchen issue transaction does not exist.");

			await FinancialYearData.ValidateFinancialYear(existingKitchenIssue.TransactionDateTime, sqlDataAccessTransaction);

			var user = await CommonData.LoadTableDataById<UserModel>(OperationNames.User, kitchenIssue.LastModifiedBy.Value, sqlDataAccessTransaction);
			if (!user.Admin)
				throw new InvalidOperationException("Only admin users can update a kitchen issue transaction.");

			kitchenIssue.TransactionNo = existingKitchenIssue.TransactionNo;
		}

		return kitchenIssue;
	}

	private static void ValidateItemDetails(KitchenIssueModel kitchenIssue, List<KitchenIssueDetailModel> kitchenIssueDetails)
	{
		if (kitchenIssueDetails is null || kitchenIssueDetails.Count == 0)
			throw new InvalidOperationException("Please add at least one item detail for the transaction.");

		if (kitchenIssueDetails.Count != kitchenIssue.TotalItems)
			throw new InvalidOperationException("Total items must be equal to the number of item details.");

		if (kitchenIssueDetails.Sum(ed => ed.Quantity) != kitchenIssue.TotalQuantity)
			throw new InvalidOperationException("Total quantity must be equal to the sum of item quantities.");

		foreach (var item in kitchenIssueDetails)
			item.Remarks = string.IsNullOrWhiteSpace(item.Remarks) ? null : item.Remarks.Trim();
	}

	public static async Task<int> SaveTransaction(
		KitchenIssueModel kitchenIssue,
		List<KitchenIssueDetailModel> kitchenIssueDetails,
		bool recover = false,
		SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		bool update = kitchenIssue.Id > 0;

		if (sqlDataAccessTransaction is null)
		{
			(MemoryStream, string)? previousInvoice = update && !recover ? await KitchenIssueInvoiceExport.ExportInvoice(kitchenIssue.Id, InvoiceExportType.PDF) : null;

			kitchenIssue.Id = await SqlDataAccessTransaction.Run(transaction => SaveTransaction(kitchenIssue, kitchenIssueDetails, recover, transaction));

			if (!recover)
				await KitchenIssueNotify.Notify(kitchenIssue.Id, update ? NotifyType.Updated : NotifyType.Created, previousInvoice);

			return kitchenIssue.Id;
		}

		kitchenIssue = await ValidateTransaction(kitchenIssue, update, sqlDataAccessTransaction);
		ValidateItemDetails(kitchenIssue, kitchenIssueDetails);

		var previousKitchenIssue = update && !recover ? await CommonData.LoadTableDataById<KitchenIssueOverviewModel>(InventoryNames.KitchenIssueOverview, kitchenIssue.Id, sqlDataAccessTransaction) : null;
		var previousKitchenIssueDetails = update && !recover ? await CommonData.LoadTableDataByMasterId<KitchenIssueItemOverviewModel>(InventoryNames.KitchenIssueItemOverview, kitchenIssue.Id, sqlDataAccessTransaction) : null;

		kitchenIssue.Id = await InsertKitchenIssue(kitchenIssue, sqlDataAccessTransaction);
		await SaveTransactionDetail(kitchenIssue, kitchenIssueDetails, update, sqlDataAccessTransaction);
		await SaveRawMaterialStock(kitchenIssue, kitchenIssueDetails, update, sqlDataAccessTransaction);
		await SaveAuditTrail(kitchenIssue, update, recover, previousKitchenIssue, previousKitchenIssueDetails, sqlDataAccessTransaction);

		return kitchenIssue.Id;
	}

	private static async Task SaveTransactionDetail(KitchenIssueModel kitchenIssue, List<KitchenIssueDetailModel> kitchenIssueDetails, bool update, SqlDataAccessTransaction sqlDataAccessTransaction)
	{
		if (update)
		{
			var existingKitchenIssueDetails = await CommonData.LoadTableDataByMasterId<KitchenIssueDetailModel>(InventoryNames.KitchenIssueDetail, kitchenIssue.Id, sqlDataAccessTransaction);
			foreach (var item in existingKitchenIssueDetails)
			{
				item.Status = false;
				await InsertKitchenIssueDetail(item, sqlDataAccessTransaction);
			}
		}

		foreach (var item in kitchenIssueDetails)
		{
			item.MasterId = kitchenIssue.Id;
			await InsertKitchenIssueDetail(item, sqlDataAccessTransaction);
		}
	}

	private static async Task SaveRawMaterialStock(KitchenIssueModel kitchenIssue, List<KitchenIssueDetailModel> kitchenIssueDetails, bool update, SqlDataAccessTransaction sqlDataAccessTransaction)
	{
		if (update)
			await RawMaterialStockData.DeleteRawMaterialStockByTypeTransactionId(nameof(StockType.KitchenIssue), kitchenIssue.Id, sqlDataAccessTransaction);

		foreach (var item in kitchenIssueDetails)
			await RawMaterialStockData.InsertRawMaterialStock(new()
			{
				Id = 0,
				RawMaterialId = item.RawMaterialId,
				Quantity = -item.Quantity,
				NetRate = item.Rate,
				Type = nameof(StockType.KitchenIssue),
				TransactionId = kitchenIssue.Id,
				TransactionNo = kitchenIssue.TransactionNo,
				TransactionDateTime = kitchenIssue.TransactionDateTime
			}, sqlDataAccessTransaction);
	}

	private static async Task SaveAuditTrail(
		KitchenIssueModel kitchenIssue,
		bool update,
		bool recover,
		KitchenIssueOverviewModel previousKitchenIssue = null,
		List<KitchenIssueItemOverviewModel> previousKitchenIssueDetails = null,
		SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		string difference = null;

		if (update && !recover)
		{
			var currentKitchenIssue = await CommonData.LoadTableDataById<KitchenIssueOverviewModel>(InventoryNames.KitchenIssueOverview, kitchenIssue.Id, sqlDataAccessTransaction);
			var currentKitchenIssueDetails = await CommonData.LoadTableDataByMasterId<KitchenIssueItemOverviewModel>(InventoryNames.KitchenIssueItemOverview, kitchenIssue.Id, sqlDataAccessTransaction);

			var headerDiff = AuditTrailData.GetDifference(previousKitchenIssue, currentKitchenIssue);
			var detailsDiff = AuditTrailData.GetDifference(previousKitchenIssueDetails, currentKitchenIssueDetails, typeof(KitchenIssueOverviewModel));

			difference = AuditTrailData.CombineDifferences(
				(null, headerDiff),
				("Items", detailsDiff));
		}

		await AuditTrailData.SaveAuditTrail(new()
		{
			Action = recover ? AuditTrailActionTypes.Recover.ToString() : update ? AuditTrailActionTypes.Update.ToString() : AuditTrailActionTypes.Insert.ToString(),
			TableName = InventoryNames.KitchenIssue,
			RecordNo = kitchenIssue.TransactionNo,
			RecordValue = difference,
			CreatedBy = update ? kitchenIssue.LastModifiedBy.Value : kitchenIssue.CreatedBy,
			CreatedFromPlatform = update ? kitchenIssue.LastModifiedFromPlatform : kitchenIssue.CreatedFromPlatform
		}, sqlDataAccessTransaction);
	}
	#endregion
}
