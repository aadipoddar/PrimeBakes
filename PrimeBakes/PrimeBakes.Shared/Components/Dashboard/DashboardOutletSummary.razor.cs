using PrimeBakes.Library.Inventory.Kitchen.Models;
using PrimeBakes.Library.Inventory.Purchase.Models;
using PrimeBakes.Library.Operations.Location;
using PrimeBakes.Library.Restaurant.Bill.Models;
using PrimeBakes.Library.Store.Sale.Models;
using PrimeBakes.Library.Store.StockTransfer.Models;

using Syncfusion.Blazor.Grids;

namespace PrimeBakes.Shared.Components.Dashboard;

public partial class DashboardOutletSummary
{
	private List<OutletSummaryModel> _outletSummaries = [];

	private List<LocationModel> _locations = [];
	private List<PurchaseOverviewModel> _purchases = [];
	private List<PurchaseReturnOverviewModel> _purchasesReturns = [];
	private List<KitchenIssueOverviewModel> _kitchenIssue = [];
	private List<KitchenIssueReturnOverviewModel> _kitchenIssueReturn = [];
	private List<KitchenProductionOverviewModel> _kitchenProduction = [];
	private List<KitchenProductionReturnOverviewModel> _kitchenProductionReturn = [];
	private List<SaleOverviewModel> _sales = [];
	private List<SaleReturnOverviewModel> _saleReturns = [];
	private List<StockTransferOverviewModel> _stockTransfers = [];
	private List<BillOverviewModel> _bills = [];

	private SfGrid<OutletSummaryModel> _sfGrid;

	private readonly List<ContextMenuItemModel> _gridContextMenuItems =
	[
		new() { Text = "Open Report", Id = "OpenReport", IconCss = "e-icons e-eye", Target = ".e-content" }
	];

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		await LoadData();
	}

	private async Task LoadData()
	{
		try
		{
			// Today only. These are the report's own loads — ten *_Overview views — so the
			// single-day window is what keeps them cheap enough to sit on the dashboard.
			var currentDateTime = await CommonData.LoadCurrentDateTime();
			var fromDate = DateOnly.FromDateTime(currentDateTime).ToDateTime(TimeOnly.MinValue);
			var toDate = fromDate;

			_locations = await CommonData.LoadTableDataByStatus<LocationModel>(OperationNames.Location);

			_purchases = await CommonData.LoadTableDataByDate<PurchaseOverviewModel>(InventoryNames.PurchaseOverview, fromDate, toDate);
			_purchasesReturns = await CommonData.LoadTableDataByDate<PurchaseReturnOverviewModel>(InventoryNames.PurchaseReturnOverview, fromDate, toDate);
			_kitchenIssue = await CommonData.LoadTableDataByDate<KitchenIssueOverviewModel>(InventoryNames.KitchenIssueOverview, fromDate, toDate);
			_kitchenIssueReturn = await CommonData.LoadTableDataByDate<KitchenIssueReturnOverviewModel>(InventoryNames.KitchenIssueReturnOverview, fromDate, toDate);
			_kitchenProduction = await CommonData.LoadTableDataByDate<KitchenProductionOverviewModel>(InventoryNames.KitchenProductionOverview, fromDate, toDate);
			_kitchenProductionReturn = await CommonData.LoadTableDataByDate<KitchenProductionReturnOverviewModel>(InventoryNames.KitchenProductionReturnOverview, fromDate, toDate);
			_sales = await CommonData.LoadTableDataByDate<SaleOverviewModel>(StoreNames.SaleOverview, fromDate, toDate);
			_saleReturns = await CommonData.LoadTableDataByDate<SaleReturnOverviewModel>(StoreNames.SaleReturnOverview, fromDate, toDate);
			_stockTransfers = await CommonData.LoadTableDataByDate<StockTransferOverviewModel>(StoreNames.StockTransferOverview, fromDate, toDate);
			_bills = await CommonData.LoadTableDataByDate<BillOverviewModel>(RestaurantNames.BillOverview, fromDate, toDate);

			_purchases = [.. _purchases.Where(_ => _.Status)];
			_purchasesReturns = [.. _purchasesReturns.Where(_ => _.Status)];
			_kitchenIssue = [.. _kitchenIssue.Where(_ => _.Status)];
			_kitchenIssueReturn = [.. _kitchenIssueReturn.Where(_ => _.Status)];
			_kitchenProduction = [.. _kitchenProduction.Where(_ => _.Status)];
			_kitchenProductionReturn = [.. _kitchenProductionReturn.Where(_ => _.Status)];
			_sales = [.. _sales.Where(_ => _.Status)];
			_saleReturns = [.. _saleReturns.Where(_ => _.Status)];
			_stockTransfers = [.. _stockTransfers.Where(_ => _.Status)];
			_bills = [.. _bills.Where(_ => _.Status)];

			await CalculateTotals();
		}
		catch { }
		finally { StateHasChanged(); }
	}

	// Mirrors OutletSummaryReport.CalculateTotals so both screens report the same numbers.
	private async Task CalculateTotals()
	{
		_outletSummaries = [];

		foreach (var location in _locations)
		{
			var outlet = new OutletSummaryModel()
			{
				LocationId = location.Id,
				LocationName = location.Name,
			};

			// HQ owns procurement and the kitchen; the outlets buy from HQ.
			if (location.Id == 1)
			{
				outlet.Purchase = _purchases.Sum(_ => _.TotalAmount);
				outlet.PurchaseReturn = _purchasesReturns.Sum(_ => _.TotalAmount);
				outlet.KitchenIssue = _kitchenIssue.Sum(_ => _.TotalAmount);
				outlet.KitchenIssueReturn = _kitchenIssueReturn.Sum(_ => _.TotalAmount);
				outlet.KitchenProduction = _kitchenProduction.Sum(_ => _.TotalAmount);
				outlet.KitchenProductionReturn = _kitchenProductionReturn.Sum(_ => _.TotalAmount);
				outlet.Sale =
					_sales.Where(_ => _.LocationId == 1).Sum(_ => _.TotalAmount) +
					_stockTransfers.Where(_ => _.LocationId == 1).Sum(_ => _.TotalAmount) +
					_bills.Where(_ => _.LocationId == 1).Sum(_ => _.TotalAmount);
				outlet.SaleReturn = _saleReturns.Where(_ => _.LocationId == 1).Sum(_ => _.TotalAmount);
			}

			else
			{
				var ledgerLocation = await LocationData.LoadLedgerByLocationId(outlet.LocationId);

				outlet.Purchase =
					_sales.Where(_ => _.PartyId == ledgerLocation.Id).Sum(_ => _.TotalAmount) +
					_stockTransfers.Where(_ => _.ToLocationId == outlet.LocationId).Sum(_ => _.TotalAmount);
				outlet.PurchaseReturn = _saleReturns.Where(_ => _.PartyId == ledgerLocation.Id).Sum(_ => _.TotalAmount);
				outlet.KitchenIssue = 0;
				outlet.KitchenIssueReturn = 0;
				outlet.KitchenProduction = 0;
				outlet.KitchenProductionReturn = 0;
				outlet.Sale =
					_sales.Where(_ => _.LocationId == outlet.LocationId).Sum(_ => _.TotalAmount) +
					_stockTransfers.Where(_ => _.LocationId == outlet.LocationId).Sum(_ => _.TotalAmount) +
					_bills.Where(_ => _.LocationId == outlet.LocationId).Sum(_ => _.TotalAmount);
				outlet.SaleReturn = _saleReturns.Where(_ => _.LocationId == outlet.LocationId).Sum(_ => _.TotalAmount);
			}

			var sales = _sales.Where(_ => _.LocationId == outlet.LocationId).ToList();
			var transfers = _stockTransfers.Where(_ => _.LocationId == outlet.LocationId).ToList();
			var bills = _bills.Where(_ => _.LocationId == outlet.LocationId).ToList();

			outlet.Cash = sales.Sum(_ => _.Cash) + transfers.Sum(_ => _.Cash) + bills.Sum(_ => _.Cash);
			outlet.Card = sales.Sum(_ => _.Card) + transfers.Sum(_ => _.Card) + bills.Sum(_ => _.Card);
			outlet.UPI = sales.Sum(_ => _.UPI) + transfers.Sum(_ => _.UPI) + bills.Sum(_ => _.UPI);
			outlet.Credit = sales.Sum(_ => _.Credit) + transfers.Sum(_ => _.Credit) + bills.Sum(_ => _.Credit);

			outlet.TransactionCount = sales.Count + transfers.Count + bills.Count;
			outlet.UnitsSold = sales.Sum(_ => _.TotalQuantity) + transfers.Sum(_ => _.TotalQuantity) + bills.Sum(_ => _.TotalQuantity);

			_outletSummaries.Add(outlet);
		}

		var totalNetSale = _outletSummaries.Sum(_ => _.NetSale);
		foreach (var outlet in _outletSummaries)
			outlet.ContributionPercent = totalNetSale == 0 ? 0 : Math.Round(outlet.NetSale / totalNetSale * 100, 2);

		_outletSummaries = [.. _outletSummaries.OrderByDescending(_ => _.NetSale)];
	}

	private void OnGridContextMenuItemClicked(ContextMenuClickEventArgs<OutletSummaryModel> args)
	{
		if (args.Item.Id == "OpenReport")
			NavigationManager.NavigateTo(StoreRouteNames.OutletSummaryReport);
	}
}
