using PrimeBakes.Data.Operations.Location;
using PrimeBakes.Models.Inventory.Kitchen;
using PrimeBakes.Models.Inventory.Kitchen.KitchenIssue;
using PrimeBakes.Models.Inventory.Kitchen.KitchenProduction;
using PrimeBakes.Models.Inventory.Purchase;
using PrimeBakes.Models.Inventory.Summary;
using PrimeBakes.Models.Operations.Location;
using PrimeBakes.Models.Restaurant.Bill;
using PrimeBakes.Models.Store.Customer;
using PrimeBakes.Models.Store.Sale;
using PrimeBakes.Models.Store.StockTransfer;
using PrimeBakes.Models.Store.Summary;

using Syncfusion.Blazor.Grids;

using System.Globalization;
using System.Text;

namespace PrimeBakes.Shared.Components.Dashboard;

public partial class DashboardSummaries
{
	#region State
	private List<OutletSummaryModel> _outletSummaries = [];
	private List<KitchenSummaryModel> _kitchenSummaries = [];
	private List<CustomerSummaryModel> _customerSummaries = [];

	private SfGrid<OutletSummaryModel> _outletGrid;
	private SfGrid<KitchenSummaryModel> _kitchenGrid;
	private SfGrid<CustomerSummaryModel> _customerGrid;

	private List<LocationModel> _locations = [];
	private List<KitchenModel> _kitchens = [];
	private List<CustomerModel> _customers = [];

	// Month to date — what the kitchen and customer grids report on.
	private List<SaleOverviewModel> _monthSales = [];
	private List<SaleReturnOverviewModel> _monthSaleReturns = [];
	private List<BillOverviewModel> _monthBills = [];
	private List<KitchenIssueOverviewModel> _monthKitchenIssue = [];
	private List<KitchenIssueReturnOverviewModel> _monthKitchenIssueReturn = [];
	private List<KitchenProductionOverviewModel> _monthKitchenProduction = [];
	private List<KitchenProductionReturnOverviewModel> _monthKitchenProductionReturn = [];

	// Today's slice, for the outlet grid. Everything except purchases and transfers is
	// sieved out of the month pull above rather than fetched a second time.
	private List<PurchaseOverviewModel> _purchases = [];
	private List<PurchaseReturnOverviewModel> _purchaseReturns = [];
	private List<StockTransferOverviewModel> _stockTransfers = [];
	private List<SaleOverviewModel> _sales = [];
	private List<SaleReturnOverviewModel> _saleReturns = [];
	private List<BillOverviewModel> _bills = [];
	private List<KitchenIssueOverviewModel> _kitchenIssue = [];
	private List<KitchenIssueReturnOverviewModel> _kitchenIssueReturn = [];
	private List<KitchenProductionOverviewModel> _kitchenProduction = [];
	private List<KitchenProductionReturnOverviewModel> _kitchenProductionReturn = [];

	private DateTime _referenceDate = DateTime.Now;

	private readonly List<ContextMenuItemModel> _gridContextMenuItems =
	[
		new() { Text = "Open Report", Id = "OpenReport", IconCss = "e-icons e-eye", Target = ".e-content" }
	];
	#endregion

	#region Load Data
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		await LoadData();
	}

	public async Task LoadData()
	{
		try
		{
			_referenceDate = await CommonData.LoadCurrentDateTime();

			var today = DateOnly.FromDateTime(_referenceDate).ToDateTime(TimeOnly.MinValue);
			var monthStart = new DateTime(today.Year, today.Month, 1);

			_locations = await CommonData.LoadTableDataByStatus<LocationModel>(OperationNames.Location, useLocalDB: true);
			_kitchens = await CommonData.LoadTableDataByStatus<KitchenModel>(InventoryNames.Kitchen, useLocalDB: true);
			_customers = await CommonData.LoadTableData<CustomerModel>(StoreNames.Customer, useLocalDB: true);

			_purchases = await CommonData.LoadTableDataByDate<PurchaseOverviewModel>(InventoryNames.PurchaseOverview, today, today, useLocalDB: true);
			_purchaseReturns = await CommonData.LoadTableDataByDate<PurchaseReturnOverviewModel>(InventoryNames.PurchaseReturnOverview, today, today, useLocalDB: true);
			_stockTransfers = await CommonData.LoadTableDataByDate<StockTransferOverviewModel>(StoreNames.StockTransferOverview, today, today, useLocalDB: true);

			_monthSales = await CommonData.LoadTableDataByDate<SaleOverviewModel>(StoreNames.SaleOverview, monthStart, today, useLocalDB: true);
			_monthSaleReturns = await CommonData.LoadTableDataByDate<SaleReturnOverviewModel>(StoreNames.SaleReturnOverview, monthStart, today, useLocalDB: true);
			_monthBills = await CommonData.LoadTableDataByDate<BillOverviewModel>(RestaurantNames.BillOverview, monthStart, today, useLocalDB: true);
			_monthKitchenIssue = await CommonData.LoadTableDataByDate<KitchenIssueOverviewModel>(InventoryNames.KitchenIssueOverview, monthStart, today, useLocalDB: true);
			_monthKitchenIssueReturn = await CommonData.LoadTableDataByDate<KitchenIssueReturnOverviewModel>(InventoryNames.KitchenIssueReturnOverview, monthStart, today, useLocalDB: true);
			_monthKitchenProduction = await CommonData.LoadTableDataByDate<KitchenProductionOverviewModel>(InventoryNames.KitchenProductionOverview, monthStart, today, useLocalDB: true);
			_monthKitchenProductionReturn = await CommonData.LoadTableDataByDate<KitchenProductionReturnOverviewModel>(InventoryNames.KitchenProductionReturnOverview, monthStart, today, useLocalDB: true);

			// Soft-deleted rows still come back from the views.
			_purchases = [.. _purchases.Where(_ => _.Status)];
			_purchaseReturns = [.. _purchaseReturns.Where(_ => _.Status)];
			_stockTransfers = [.. _stockTransfers.Where(_ => _.Status)];

			_monthSales = [.. _monthSales.Where(_ => _.Status)];
			_monthSaleReturns = [.. _monthSaleReturns.Where(_ => _.Status)];
			_monthBills = [.. _monthBills.Where(_ => _.Status)];
			_monthKitchenIssue = [.. _monthKitchenIssue.Where(_ => _.Status)];
			_monthKitchenIssueReturn = [.. _monthKitchenIssueReturn.Where(_ => _.Status)];
			_monthKitchenProduction = [.. _monthKitchenProduction.Where(_ => _.Status)];
			_monthKitchenProductionReturn = [.. _monthKitchenProductionReturn.Where(_ => _.Status)];

			// Today lives inside the month already pulled, so sieve it out here instead of
			// asking SQL for the same rows twice.
			_sales = [.. _monthSales.Where(_ => _.TransactionDateTime >= today)];
			_saleReturns = [.. _monthSaleReturns.Where(_ => _.TransactionDateTime >= today)];
			_bills = [.. _monthBills.Where(_ => _.TransactionDateTime >= today)];
			_kitchenIssue = [.. _monthKitchenIssue.Where(_ => _.TransactionDateTime >= today)];
			_kitchenIssueReturn = [.. _monthKitchenIssueReturn.Where(_ => _.TransactionDateTime >= today)];
			_kitchenProduction = [.. _monthKitchenProduction.Where(_ => _.TransactionDateTime >= today)];
			_kitchenProductionReturn = [.. _monthKitchenProductionReturn.Where(_ => _.TransactionDateTime >= today)];
		}
		catch { }

		// Each grid calculates on its own: one bad section must not blank the other two.
		try { await CalculateOutletTotals(); } catch { }
		try { CalculateKitchenTotals(); } catch { }
		try { CalculateCustomerTotals(); } catch { }

		StateHasChanged();

		// The grids are already on screen when the auto-refresh reloads, so they need telling.
		if (_outletGrid is not null) await _outletGrid.Refresh();
		if (_kitchenGrid is not null) await _kitchenGrid.Refresh();
		if (_customerGrid is not null) await _customerGrid.Refresh();
	}
	#endregion

	#region Outlet
	// Mirrors OutletSummaryReport.CalculateTotals so both screens report the same numbers.
	private async Task CalculateOutletTotals()
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
				outlet.PurchaseReturn = _purchaseReturns.Sum(_ => _.TotalAmount);
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

			outlet.LastSaleDateTime = sales.Select(_ => _.TransactionDateTime)
				.Concat(transfers.Select(_ => _.TransactionDateTime))
				.Concat(bills.Select(_ => _.TransactionDateTime))
				.DefaultIfEmpty()
				.Max();

			_outletSummaries.Add(outlet);
		}

		var totalNetSale = _outletSummaries.Sum(_ => _.NetSale);
		foreach (var outlet in _outletSummaries)
			outlet.ContributionPercent = totalNetSale == 0 ? 0 : Math.Round(outlet.NetSale / totalNetSale * 100, 2);

		_outletSummaries = [.. _outletSummaries.OrderByDescending(_ => _.LastSaleDateTime)];
	}

	private void OnOutletContextMenuItemClicked(ContextMenuClickEventArgs<OutletSummaryModel> args)
	{
		if (args.Item.Id == "OpenReport")
			NavigationManager.NavigateTo(StoreRouteNames.OutletSummaryReport);
	}
	#endregion

	#region Kitchen
	// Mirrors KitchenSummaryReport.CalculateTotals so both screens report the same numbers.
	private void CalculateKitchenTotals()
	{
		_kitchenSummaries = [];

		foreach (var kitchen in _kitchens)
		{
			var summary = new KitchenSummaryModel
			{
				KitchenId = kitchen.Id,
				KitchenName = kitchen.Name,
			};

			var kitchenIssues = _monthKitchenIssue.Where(_ => _.KitchenId == kitchen.Id).ToList();
			var kitchenIssueReturns = _monthKitchenIssueReturn.Where(_ => _.KitchenId == kitchen.Id).ToList();
			var kitchenProductions = _monthKitchenProduction.Where(_ => _.KitchenId == kitchen.Id).ToList();
			var kitchenProductionReturns = _monthKitchenProductionReturn.Where(_ => _.KitchenId == kitchen.Id).ToList();

			summary.KitchenIssue = kitchenIssues.Sum(_ => _.TotalAmount);
			summary.KitchenIssueReturn = kitchenIssueReturns.Sum(_ => _.TotalAmount);
			summary.KitchenProduction = kitchenProductions.Sum(_ => _.TotalAmount);
			summary.KitchenProductionReturn = kitchenProductionReturns.Sum(_ => _.TotalAmount);

			summary.TransactionCount = kitchenIssues.Count + kitchenIssueReturns.Count + kitchenProductions.Count + kitchenProductionReturns.Count;
			summary.UnitsProduced = kitchenProductions.Sum(_ => _.TotalQuantity) - kitchenProductionReturns.Sum(_ => _.TotalQuantity);

			_kitchenSummaries.Add(summary);
		}

		var totalNetProduction = _kitchenSummaries.Sum(_ => _.NetProduction);
		foreach (var summary in _kitchenSummaries)
			summary.ContributionPercent = totalNetProduction == 0 ? 0 : Math.Round(summary.NetProduction / totalNetProduction * 100, 2);

		_kitchenSummaries = [.. _kitchenSummaries.OrderByDescending(_ => _.NetProduction)];
	}

	private void OnKitchenContextMenuItemClicked(ContextMenuClickEventArgs<KitchenSummaryModel> args)
	{
		if (args.Item.Id == "OpenReport")
			NavigationManager.NavigateTo(InventoryRouteNames.KitchenSummaryReport);
	}
	#endregion

	#region Customer
	// Mirrors CustomerSummaryReport's calculation so both screens report the same numbers.
	private void CalculateCustomerTotals()
	{
		_customerSummaries = [];

		foreach (var customer in _customers)
		{
			var customerSales = _monthSales.Where(_ => _.CustomerId == customer.Id).ToList();
			var customerBills = _monthBills.Where(_ => _.CustomerId == customer.Id).ToList();
			var customerReturns = _monthSaleReturns.Where(_ => _.CustomerId == customer.Id).ToList();

			// Customers who did nothing this month would only pad the grid.
			if (customerSales.Count == 0 && customerBills.Count == 0 && customerReturns.Count == 0)
				continue;

			var summary = new CustomerSummaryModel
			{
				CustomerId = customer.Id,
				Name = customer.Name,
				Number = customer.Number,

				SaleCount = customerSales.Count,
				BillCount = customerBills.Count,
				ReturnCount = customerReturns.Count,

				SaleAmount = customerSales.Sum(_ => _.TotalAmount),
				BillAmount = customerBills.Sum(_ => _.TotalAmount),
				ReturnAmount = customerReturns.Sum(_ => _.TotalAmount),

				TotalQuantity = customerSales.Sum(_ => _.TotalQuantity) + customerBills.Sum(_ => _.TotalQuantity),

				Cash = customerSales.Sum(_ => _.Cash) + customerBills.Sum(_ => _.Cash),
				Card = customerSales.Sum(_ => _.Card) + customerBills.Sum(_ => _.Card),
				UPI = customerSales.Sum(_ => _.UPI) + customerBills.Sum(_ => _.UPI),
				Credit = customerSales.Sum(_ => _.Credit) + customerBills.Sum(_ => _.Credit),
			};

			var purchaseDates = customerSales.Select(_ => _.TransactionDateTime).Concat(customerBills.Select(_ => _.TransactionDateTime)).ToList();
			if (purchaseDates.Count > 0)
			{
				summary.FirstPurchase = purchaseDates.Min();
				summary.LastPurchase = purchaseDates.Max();
				summary.DaysSinceLastVisit = Math.Max(0, (_referenceDate.Date - summary.LastPurchase.Value.Date).Days);
			}

			_customerSummaries.Add(summary);
		}

		var totalNetBusiness = _customerSummaries.Sum(_ => _.NetBusiness);
		foreach (var summary in _customerSummaries)
			summary.ContributionPercent = totalNetBusiness == 0 ? 0 : Math.Round(summary.NetBusiness / totalNetBusiness * 100, 2);

		_customerSummaries = [.. _customerSummaries.OrderByDescending(_ => _.NetBusiness)];
	}

	private void OnCustomerContextMenuItemClicked(ContextMenuClickEventArgs<CustomerSummaryModel> args)
	{
		if (args.Item.Id == "OpenReport")
			NavigationManager.NavigateTo(StoreRouteNames.CustomerSummaryReport);
	}
	#endregion

	#region AI Context
	public string ToAiText()
	{
		var sb = new StringBuilder();

		// Most locations are idle on a given day; list only the active ones and count the rest.
		sb.AppendLine("OUTLETS TODAY SO FAR (sales recorded in the software so far today)");
		var active = _outletSummaries.Where(_ => _.TransactionCount > 0 || _.NetSale != 0).ToList();
		foreach (var outlet in active)
			sb.AppendLine($"- {outlet.LocationName} [{LocationCategory(outlet.LocationId)}]: net sales ₹{FormatAiNumber(outlet.NetSale)}, {outlet.TransactionCount} transactions, {FormatAiNumber(outlet.UnitsSold)} units, cash ₹{FormatAiNumber(outlet.Cash)}, card ₹{FormatAiNumber(outlet.Card)}, UPI ₹{FormatAiNumber(outlet.UPI)}, credit ₹{FormatAiNumber(outlet.Credit)}, returns ₹{FormatAiNumber(outlet.SaleReturn)}, {FormatAiNumber(outlet.ContributionPercent)}% of today's sales");

		if (active.Count == 0)
			sb.AppendLine("- No sales recorded anywhere yet today.");

		var idle = _outletSummaries.Count - active.Count;
		if (idle > 0)
			sb.AppendLine($"- {idle} other locations have no sales recorded yet today.");
		sb.AppendLine();

		sb.AppendLine("KITCHENS THIS MONTH TO DATE (issue = raw material issued to the kitchen, production = finished products made, the return lines are damages)");
		foreach (var kitchen in _kitchenSummaries)
			sb.AppendLine($"- {kitchen.KitchenName}: issue ₹{FormatAiNumber(kitchen.KitchenIssue)}, issue returns (damage) ₹{FormatAiNumber(kitchen.KitchenIssueReturn)}, production ₹{FormatAiNumber(kitchen.KitchenProduction)}, production returns (damage) ₹{FormatAiNumber(kitchen.KitchenProductionReturn)}, net production ₹{FormatAiNumber(kitchen.NetProduction)}, {FormatAiNumber(kitchen.UnitsProduced)} units produced");
		sb.AppendLine();

		sb.AppendLine("TOP 10 CUSTOMERS THIS MONTH TO DATE by net business");
		foreach (var customer in _customerSummaries.Take(10))
		{
			// A customer with only a return this month has no purchase date, so no visit to report.
			var lastVisit = customer.LastPurchase.HasValue
				? $"{customer.DaysSinceLastVisit} days since last visit"
				: "no purchases this month";

			sb.AppendLine($"- {customer.Name}: {customer.PurchaseCount} purchases, net business ₹{FormatAiNumber(customer.NetBusiness)}, {lastVisit}");
		}

		if (_customerSummaries.Count == 0)
			sb.AppendLine("- No sales this month are linked to a named customer, so there is no customer ranking.");

		return sb.ToString();
	}

	// Location 1 is the main outlet; the rest are Coco (company owned), Fofo (franchise) or Supply.
	private string LocationCategory(int locationId)
	{
		var location = _locations.FirstOrDefault(_ => _.Id == locationId);

		if (location is null)
			return "unknown";

		if (location.Id == 1)
			return "main outlet";

		if (location.COCO)
			return "Coco, company owned";

		if (location.FOFO)
			return "Fofo, franchise";

		return "Supply";
	}
	private static string FormatAiNumber(decimal value) => value.ToString("0.##", CultureInfo.InvariantCulture);
	#endregion
}