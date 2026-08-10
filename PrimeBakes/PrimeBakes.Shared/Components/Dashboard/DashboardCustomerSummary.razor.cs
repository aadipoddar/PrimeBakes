using PrimeBakes.Library.Restaurant.Bill.Models;
using PrimeBakes.Library.Store.Customer.Models;
using PrimeBakes.Library.Store.Sale.Models;

using Syncfusion.Blazor.Grids;

namespace PrimeBakes.Shared.Components.Dashboard;

public partial class DashboardCustomerSummary
{
	private List<CustomerSummaryModel> _customerSummaries = [];

	private List<CustomerModel> _customers = [];
	private List<SaleOverviewModel> _sales = [];
	private List<SaleReturnOverviewModel> _saleReturns = [];
	private List<BillOverviewModel> _bills = [];

	private DateTime _referenceDate = DateTime.Now;

	private SfGrid<CustomerSummaryModel> _sfGrid;

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
			// Today only, same as the rest of the summary band.
			_referenceDate = await CommonData.LoadCurrentDateTime();
			var fromDate = DateOnly.FromDateTime(_referenceDate).ToDateTime(TimeOnly.MinValue);
			var toDate = fromDate;

			_customers = await CommonData.LoadTableDataByStatus<CustomerModel>(StoreNames.Customer);

			_sales = await CommonData.LoadTableDataByDate<SaleOverviewModel>(StoreNames.SaleOverview, fromDate, toDate);
			_saleReturns = await CommonData.LoadTableDataByDate<SaleReturnOverviewModel>(StoreNames.SaleReturnOverview, fromDate, toDate);
			_bills = await CommonData.LoadTableDataByDate<BillOverviewModel>(RestaurantNames.BillOverview, fromDate, toDate);

			_sales = [.. _sales.Where(_ => _.Status)];
			_saleReturns = [.. _saleReturns.Where(_ => _.Status)];
			_bills = [.. _bills.Where(_ => _.Status)];

			CalculateTotals();
		}
		catch { }
		finally { StateHasChanged(); }
	}

	// Mirrors CustomerSummaryReport's calculation so both screens report the same numbers.
	private void CalculateTotals()
	{
		_customerSummaries = [];

		foreach (var customer in _customers)
		{
			var customerSales = _sales.Where(_ => _.CustomerId == customer.Id).ToList();
			var customerBills = _bills.Where(_ => _.CustomerId == customer.Id).ToList();
			var customerReturns = _saleReturns.Where(_ => _.CustomerId == customer.Id).ToList();

			// Customers who did nothing today would only pad the grid.
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

	private void OnGridContextMenuItemClicked(ContextMenuClickEventArgs<CustomerSummaryModel> args)
	{
		if (args.Item.Id == "OpenReport")
			NavigationManager.NavigateTo(StoreRouteNames.CustomerSummaryReport);
	}
}
