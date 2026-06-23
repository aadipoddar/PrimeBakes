using PrimeBakes.Library.Accounts.Masters.Data;
using PrimeBakes.Library.Common;
using PrimeBakes.Library.Inventory.Kitchen.Exports;
using PrimeBakes.Library.Inventory.Kitchen.Models;
using PrimeBakes.Library.Inventory.Stock.Data;
using PrimeBakes.Library.Inventory.Stock.Models;
using PrimeBakes.Library.Operations.AuditTrail;
using PrimeBakes.Library.Operations.User;
using PrimeBakes.Library.Utils.Exports;
using PrimeBakes.Library.Utils.Mail;

namespace PrimeBakes.Library.Inventory.Kitchen.Data;

public static class KitchenProductionData
{
	private static async Task<int> InsertKitchenProduction(KitchenProductionModel kitchenProduction, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(InventoryNames.InsertKitchenProduction, kitchenProduction, sqlDataAccessTransaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert Kitchen Production.");

	private static async Task<int> InsertKitchenProductionDetail(KitchenProductionDetailModel kitchenProductionDetail, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(InventoryNames.InsertKitchenProductionDetail, kitchenProductionDetail, sqlDataAccessTransaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert Kitchen Production Detail.");

	public static List<KitchenProductionDetailModel> ConvertCartToDetails(List<KitchenProductionProductCartModel> cart, int masterId = 0) =>
		[.. cart.Select(item => new KitchenProductionDetailModel
		{
			Id = 0,
			MasterId = masterId,
			ProductId = item.ProductId,
			Quantity = item.Quantity,
			Rate = item.Rate,
			Total = item.Total,
			Remarks = item.Remarks,
			Status = true
		})];

	#region Delete
	public static async Task DeleteTransaction(KitchenProductionModel kitchenProduction, SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		if (sqlDataAccessTransaction is null)
		{
			await SqlDataAccessTransaction.Run(transaction => DeleteTransaction(kitchenProduction, transaction));
			await KitchenProductionNotify.Notify(kitchenProduction.Id, NotifyType.Deleted);
			return;
		}

		await FinancialYearData.ValidateFinancialYear(kitchenProduction.TransactionDateTime, sqlDataAccessTransaction);

		kitchenProduction.Status = false;
		await InsertKitchenProduction(kitchenProduction, sqlDataAccessTransaction);
		await ProductStockData.DeleteProductStockByTransactionNo(kitchenProduction.TransactionNo, sqlDataAccessTransaction);

		await AuditTrailData.SaveAuditTrail(new()
		{
			Action = AuditTrailActionTypes.Delete.ToString(),
			TableName = InventoryNames.KitchenProduction,
			RecordNo = kitchenProduction.TransactionNo,
			CreatedBy = kitchenProduction.LastModifiedBy.Value,
			CreatedFromPlatform = kitchenProduction.LastModifiedFromPlatform
		}, sqlDataAccessTransaction);
	}

	public static async Task RecoverTransaction(KitchenProductionModel kitchenProduction)
	{
		kitchenProduction.Status = true;
		var kitchenProductionDetails = await CommonData.LoadTableDataByMasterId<KitchenProductionDetailModel>(InventoryNames.KitchenProductionDetail, kitchenProduction.Id);
		await SaveTransaction(kitchenProduction, kitchenProductionDetails, true);

		await KitchenProductionNotify.Notify(kitchenProduction.Id, NotifyType.Recovered);
	}
	#endregion

	#region Save
	private static async Task<KitchenProductionModel> ValidateTransaction(KitchenProductionModel kitchenProduction, bool update, SqlDataAccessTransaction sqlDataAccessTransaction)
	{
		kitchenProduction.Remarks = string.IsNullOrWhiteSpace(kitchenProduction.Remarks) ? null : kitchenProduction.Remarks.Trim();

		if (kitchenProduction.CompanyId <= 0)
			throw new InvalidOperationException("Please select a company for the transaction.");

		if (kitchenProduction.KitchenId <= 0)
			throw new InvalidOperationException("Please select a kitchen for the transaction.");

		if (kitchenProduction.TotalItems <= 0)
			throw new InvalidOperationException("The total number of items in the transaction must be greater than zero.");

		if (kitchenProduction.TotalQuantity <= 0)
			throw new InvalidOperationException("The total quantity of items in the transaction must be greater than zero.");

		if (kitchenProduction.TotalAmount < 0)
			throw new InvalidOperationException("The total amount of the transaction cannot be negative.");

		if (!update)
			kitchenProduction.TransactionNo = await GenerateCodes.GenerateKitchenProductionTransactionNo(kitchenProduction, sqlDataAccessTransaction);

		await FinancialYearData.ValidateFinancialYear(kitchenProduction.TransactionDateTime, sqlDataAccessTransaction);

		if (update)
		{
			var existingKitchenProduction = await CommonData.LoadTableDataById<KitchenProductionModel>(InventoryNames.KitchenProduction, kitchenProduction.Id, sqlDataAccessTransaction)
				?? throw new InvalidOperationException("The transaction to be updated does not exist.");

			await FinancialYearData.ValidateFinancialYear(existingKitchenProduction.TransactionDateTime, sqlDataAccessTransaction);

			var user = await CommonData.LoadTableDataById<UserModel>(OperationNames.User, kitchenProduction.LastModifiedBy.Value, sqlDataAccessTransaction);
			if (!user.Admin || user.LocationId != 1)
				throw new InvalidOperationException("Only admin users are allowed to modify transactions.");

			kitchenProduction.TransactionNo = existingKitchenProduction.TransactionNo;
		}

		return kitchenProduction;
	}

	private static void ValidateItemDetails(KitchenProductionModel kitchenProduction, List<KitchenProductionDetailModel> kitchenProductionDetails)
	{
		if (kitchenProductionDetails is null || kitchenProductionDetails.Count == 0)
			throw new InvalidOperationException("Please add at least one item detail for the transaction.");

		if (kitchenProductionDetails.Count != kitchenProduction.TotalItems)
			throw new InvalidOperationException("Total items must be equal to the number of item details.");

		if (kitchenProductionDetails.Sum(ed => ed.Quantity) != kitchenProduction.TotalQuantity)
			throw new InvalidOperationException("Total quantity must be equal to the sum of item quantities.");

		foreach (var item in kitchenProductionDetails)
			item.Remarks = string.IsNullOrWhiteSpace(item.Remarks) ? null : item.Remarks.Trim();
	}

	public static async Task<int> SaveTransaction(
		KitchenProductionModel kitchenProduction,
		List<KitchenProductionDetailModel> kitchenProductionDetails,
		bool recover = false,
		SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		bool update = kitchenProduction.Id > 0;

		if (sqlDataAccessTransaction is null)
		{
			(MemoryStream, string)? previousInvoice = update && !recover ? await KitchenProductionInvoiceExport.ExportInvoice(kitchenProduction.Id, InvoiceExportType.PDF) : null;

			kitchenProduction.Id = await SqlDataAccessTransaction.Run(transaction => SaveTransaction(kitchenProduction, kitchenProductionDetails, recover, transaction));

			if (!recover)
				await KitchenProductionNotify.Notify(kitchenProduction.Id, update ? NotifyType.Updated : NotifyType.Created, previousInvoice);

			return kitchenProduction.Id;
		}

		kitchenProduction = await ValidateTransaction(kitchenProduction, update, sqlDataAccessTransaction);
		ValidateItemDetails(kitchenProduction, kitchenProductionDetails);

		var previousKitchenProduction = update && !recover ? await CommonData.LoadTableDataById<KitchenProductionOverviewModel>(InventoryNames.KitchenProductionOverview, kitchenProduction.Id, sqlDataAccessTransaction) : new();
		var previousKitchenProductionDetails = update && !recover ? await CommonData.LoadTableDataByMasterId<KitchenProductionItemOverviewModel>(InventoryNames.KitchenProductionItemOverview, kitchenProduction.Id, sqlDataAccessTransaction) : [];

		kitchenProduction.Id = await InsertKitchenProduction(kitchenProduction, sqlDataAccessTransaction);
		await SaveTransactionDetail(kitchenProduction, kitchenProductionDetails, update, sqlDataAccessTransaction);
		await SaveProductStock(kitchenProduction, kitchenProductionDetails, sqlDataAccessTransaction);
		await SaveAuditTrail(kitchenProduction, update, recover, previousKitchenProduction, previousKitchenProductionDetails, sqlDataAccessTransaction);

		return kitchenProduction.Id;
	}

	private static async Task SaveTransactionDetail(KitchenProductionModel kitchenProduction, List<KitchenProductionDetailModel> kitchenProductionDetails, bool update, SqlDataAccessTransaction sqlDataAccessTransaction)
	{
		if (update)
		{
			var existingKitchenProductionDetails = await CommonData.LoadTableDataByMasterId<KitchenProductionDetailModel>(InventoryNames.KitchenProductionDetail, kitchenProduction.Id, sqlDataAccessTransaction);
			foreach (var item in existingKitchenProductionDetails)
			{
				item.Status = false;
				await InsertKitchenProductionDetail(item, sqlDataAccessTransaction);
			}
		}

		foreach (var item in kitchenProductionDetails)
		{
			item.MasterId = kitchenProduction.Id;
			await InsertKitchenProductionDetail(item, sqlDataAccessTransaction);
		}
	}

	private static async Task SaveProductStock(KitchenProductionModel kitchenProduction, List<KitchenProductionDetailModel> kitchenProductionDetails, SqlDataAccessTransaction sqlDataAccessTransaction)
	{
		await ProductStockData.DeleteProductStockByTransactionNo(kitchenProduction.TransactionNo, sqlDataAccessTransaction);

		foreach (var item in kitchenProductionDetails)
			await ProductStockData.InsertProductStock(new()
			{
				Id = 0,
				ProductId = item.ProductId,
				Quantity = item.Quantity,
				NetRate = item.Rate,
				Type = nameof(StockType.KitchenProduction),
				TransactionId = kitchenProduction.Id,
				TransactionNo = kitchenProduction.TransactionNo,
				TransactionDateTime = kitchenProduction.TransactionDateTime,
				LocationId = 1, // Main Location
			}, sqlDataAccessTransaction);
	}

	private static async Task SaveAuditTrail(
		KitchenProductionModel kitchenProduction,
		bool update,
		bool recover,
		KitchenProductionOverviewModel previousKitchenProduction = null,
		List<KitchenProductionItemOverviewModel> previousKitchenProductionDetails = null,
		SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		string difference = null;

		if (update && !recover)
		{
			var currentKitchenProduction = await CommonData.LoadTableDataById<KitchenProductionOverviewModel>(InventoryNames.KitchenProductionOverview, kitchenProduction.Id, sqlDataAccessTransaction);
			var currentKitchenProductionDetails = await CommonData.LoadTableDataByMasterId<KitchenProductionItemOverviewModel>(InventoryNames.KitchenProductionItemOverview, kitchenProduction.Id, sqlDataAccessTransaction);

			var headerDiff = AuditTrailData.GetDifference(previousKitchenProduction, currentKitchenProduction);
			var detailsDiff = AuditTrailData.GetDifference(previousKitchenProductionDetails, currentKitchenProductionDetails, typeof(KitchenProductionOverviewModel));

			difference = AuditTrailData.CombineDifferences(
				(null, headerDiff),
				("Items", detailsDiff));
		}

		await AuditTrailData.SaveAuditTrail(new()
		{
			Action = recover ? AuditTrailActionTypes.Recover.ToString() : update ? AuditTrailActionTypes.Update.ToString() : AuditTrailActionTypes.Insert.ToString(),
			TableName = InventoryNames.KitchenProduction,
			RecordNo = kitchenProduction.TransactionNo,
			RecordValue = difference,
			CreatedBy = update ? kitchenProduction.LastModifiedBy.Value : kitchenProduction.CreatedBy,
			CreatedFromPlatform = update ? kitchenProduction.LastModifiedFromPlatform : kitchenProduction.CreatedFromPlatform
		}, sqlDataAccessTransaction);
	}
	#endregion
}
