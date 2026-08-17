using PrimeBakes.Data.Common;
using PrimeBakes.Data.Operations.Settings;
using PrimeBakes.Models.Inventory.Purchase;
using PrimeBakes.Models.Operations.Settings;
using PrimeBakes.Models.Restaurant.Bill;
using PrimeBakes.Models.Store.Order;
using PrimeBakes.Models.Store.Sale;

namespace PrimeBakes.Shared.Components.Dashboard;

public partial class DashboardAnalysis : IAsyncDisposable
{
	private PeriodicTimer _autoRefreshTimer;
	private CancellationTokenSource _autoRefreshCts;

	// One timer for the whole tab — every child reloads on the same tick.
	private DashboardChart _chart;
	private DashboardSummaries _summaries;

	// Read from the base tables, not the *_Overview views: those join nine tables per
	// row to resolve names no KPI tile ever shows.
	private List<SaleModel> _sales = [];
	private List<BillModel> _bills = [];
	private List<SaleReturnModel> _saleReturns = [];
	private List<PurchaseModel> _purchases = [];
	private List<OrderModel> _orders = [];

	#region KPI
	private string _todayRevenue = "₹0";
	private string _todayRevenueTrend = "vs yesterday";

	private string _thisMonthRevenue = "₹0";
	private string _thisMonthRevenueTrend = "vs last month";

	private string _thisMonthPurchase = "₹0";
	private string _thisMonthPurchaseTrend = "vs last month";

	private string _thisMonthNet = "₹0";
	private string _thisMonthNetTrend = "vs last month";

	private string _thisMonthReturns = "₹0";
	private string _thisMonthReturnsNote = "of gross sales";

	private string _thisMonthAverageBill = "₹0";
	private string _thisMonthAverageBillTrend = "vs last month";

	private string _thisMonthCredit = "₹0";
	private string _thisMonthCreditTrend = "vs last month";

	private int _openCount = 0;
	private string _openNote = "orders & running bills";
	#endregion

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		await LoadData();
		LoadKPI();

		await StartAutoRefresh();
	}

	private async Task LoadData()
	{
		try
		{
			// Two months in one pull — this month gives the value, last month gives the trend.
			var thisMonthStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
			var thisMonthEnd = thisMonthStart.AddMonths(1).AddSeconds(-1);
			var lastMonthStart = thisMonthStart.AddMonths(-1);

			_sales = await CommonData.LoadTableDataByDate<SaleModel>(StoreNames.Sale, lastMonthStart, thisMonthEnd);
			_bills = await CommonData.LoadTableDataByDate<BillModel>(RestaurantNames.Bill, lastMonthStart, thisMonthEnd);
			_saleReturns = await CommonData.LoadTableDataByDate<SaleReturnModel>(StoreNames.SaleReturn, lastMonthStart, thisMonthEnd);
			_purchases = await CommonData.LoadTableDataByDate<PurchaseModel>(InventoryNames.Purchase, lastMonthStart, thisMonthEnd);
			_orders = await CommonData.LoadTableDataByDate<OrderModel>(StoreNames.Order, lastMonthStart, thisMonthEnd);

			_sales = [.. _sales.Where(_ => _.Status)];
			_bills = [.. _bills.Where(_ => _.Status)];
			_saleReturns = [.. _saleReturns.Where(_ => _.Status)];
			_purchases = [.. _purchases.Where(_ => _.Status)];
			_orders = [.. _orders.Where(_ => _.Status)];
		}
		catch { }
		finally { StateHasChanged(); }
	}

	private void LoadKPI()
	{
		var today = DateTime.Now.Date;
		var tomorrow = today.AddDays(1);
		var yesterday = today.AddDays(-1);

		var thisMonthStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
		var nextMonthStart = thisMonthStart.AddMonths(1);
		var lastMonthStart = thisMonthStart.AddMonths(-1);

		// Daily pulse — a bakery is judged one day at a time.
		var todayRevenue = Revenue(today, tomorrow);
		_todayRevenue = todayRevenue.FormatIndianCurrencyShort();
		_todayRevenueTrend = Helper.FormatTrend(todayRevenue, Revenue(yesterday, today), "yesterday");

		var thisMonthRevenue = Revenue(thisMonthStart, nextMonthStart);
		var lastMonthRevenue = Revenue(lastMonthStart, thisMonthStart);
		_thisMonthRevenue = thisMonthRevenue.FormatIndianCurrencyShort();
		_thisMonthRevenueTrend = Helper.FormatMonthlyTrend(thisMonthRevenue, lastMonthRevenue);

		var thisMonthPurchase = Purchases(thisMonthStart, nextMonthStart);
		var lastMonthPurchase = Purchases(lastMonthStart, thisMonthStart);
		_thisMonthPurchase = thisMonthPurchase.FormatIndianCurrencyShort();
		_thisMonthPurchaseTrend = Helper.FormatMonthlyTrend(thisMonthPurchase, lastMonthPurchase);

		// Cash basis: what came in less what went out on stock. Not matched COGS —
		// stock bought this month may well be sold next month.
		var thisMonthNet = thisMonthRevenue - thisMonthPurchase;
		var lastMonthNet = lastMonthRevenue - lastMonthPurchase;
		_thisMonthNet = thisMonthNet.FormatIndianCurrencyShort();
		_thisMonthNetTrend = Helper.FormatMonthlyTrend(thisMonthNet, lastMonthNet);

		// Returns as a share of gross sales — the wastage / quality signal.
		var thisMonthReturns = Returns(thisMonthStart, nextMonthStart);
		var thisMonthGrossSales = GrossSales(thisMonthStart, nextMonthStart);
		_thisMonthReturns = thisMonthReturns.FormatIndianCurrencyShort();
		_thisMonthReturnsNote = thisMonthGrossSales == 0
			? "of gross sales"
			: $"{thisMonthReturns / thisMonthGrossSales * 100:0.0}% of gross sales";

		// Basket health — revenue spread across the number of bills that produced it.
		var thisMonthCount = TransactionCount(thisMonthStart, nextMonthStart);
		var lastMonthCount = TransactionCount(lastMonthStart, thisMonthStart);
		var thisMonthAverageBill = thisMonthCount == 0 ? 0 : thisMonthRevenue / thisMonthCount;
		var lastMonthAverageBill = lastMonthCount == 0 ? 0 : lastMonthRevenue / lastMonthCount;
		_thisMonthAverageBill = thisMonthAverageBill.FormatIndianCurrencyShort();
		_thisMonthAverageBillTrend = Helper.FormatMonthlyTrend(thisMonthAverageBill, lastMonthAverageBill);

		// Credit billed this month. Nothing records settlement against it, so this is
		// credit issued — not a receivables balance.
		var thisMonthCredit = CreditSales(thisMonthStart, nextMonthStart);
		var lastMonthCredit = CreditSales(lastMonthStart, thisMonthStart);
		_thisMonthCredit = thisMonthCredit.FormatIndianCurrencyShort();
		_thisMonthCreditTrend = Helper.FormatMonthlyTrend(thisMonthCredit, lastMonthCredit);

		// Still open right now: orders never billed, and bills still on a table.
		var pendingOrders = _orders.Count(_ => _.SaleId is null);
		var runningBills = _bills.Count(_ => _.Running);
		_openCount = pendingOrders + runningBills;
		_openNote = $"{pendingOrders} orders & {runningBills} running bills";

		StateHasChanged();
	}

	#region Calculations
	// Every window below is [from, to) so month and day boundaries never double count.

	// Gross takings: store sales plus settled restaurant bills. A running bill is still
	// open on the table, so it has not been earned yet.
	private decimal GrossSales(DateTime from, DateTime to) =>
		_sales.Where(_ => _.TransactionDateTime >= from && _.TransactionDateTime < to).Sum(_ => _.TotalAmount) +
		_bills.Where(_ => !_.Running && _.TransactionDateTime >= from && _.TransactionDateTime < to).Sum(_ => _.TotalAmount);

	private decimal Returns(DateTime from, DateTime to) =>
		_saleReturns.Where(_ => _.TransactionDateTime >= from && _.TransactionDateTime < to).Sum(_ => _.TotalAmount);

	// Revenue is what the business actually kept — gross takings net of what came back.
	private decimal Revenue(DateTime from, DateTime to) =>
		GrossSales(from, to) - Returns(from, to);

	private decimal Purchases(DateTime from, DateTime to) =>
		_purchases.Where(_ => _.TransactionDateTime >= from && _.TransactionDateTime < to).Sum(_ => _.TotalAmount);

	private decimal CreditSales(DateTime from, DateTime to) =>
		_sales.Where(_ => _.TransactionDateTime >= from && _.TransactionDateTime < to).Sum(_ => _.Credit) +
		_bills.Where(_ => !_.Running && _.TransactionDateTime >= from && _.TransactionDateTime < to).Sum(_ => _.Credit);

	private int TransactionCount(DateTime from, DateTime to) =>
		_sales.Count(_ => _.TransactionDateTime >= from && _.TransactionDateTime < to) +
		_bills.Count(_ => !_.Running && _.TransactionDateTime >= from && _.TransactionDateTime < to);
	#endregion

	#region Auto Refresh
	private async Task StartAutoRefresh()
	{
		var timerSetting = await SettingsData.LoadSettingsByKey(SettingsKeys.AutoRefreshReportTimer);
		var refreshMinutes = int.TryParse(timerSetting?.Value, out var minutes) ? minutes : 5;

		_autoRefreshCts = new CancellationTokenSource();
		_autoRefreshTimer = new PeriodicTimer(TimeSpan.FromMinutes(refreshMinutes));
		_ = AutoRefreshLoop(_autoRefreshCts.Token);
	}

	private async Task AutoRefreshLoop(CancellationToken cancellationToken)
	{
		try
		{
			while (await _autoRefreshTimer.WaitForNextTickAsync(cancellationToken))
			{
				await LoadData();
				LoadKPI();

				await _chart.LoadData();
				await _summaries.LoadData();
			}
		}
		catch (OperationCanceledException) { }
	}

	async ValueTask IAsyncDisposable.DisposeAsync()
	{
		if (_autoRefreshCts is not null)
		{
			await _autoRefreshCts.CancelAsync();
			_autoRefreshCts.Dispose();
		}

		_autoRefreshTimer?.Dispose();
		GC.SuppressFinalize(this);
	}
	#endregion
}
