using MudBlazor;

using PrimeBakes.Library.Operations.Analysis;

namespace PrimeBakes.Shared.Components.Dashboard;

public partial class DashboardChart
{
	// Row 1 - Revenue line / Purchase bar
	private List<ChartSeries<double>> _revenueSeries = [];
	private List<ChartSeries<double>> _purchaseSeries = [];
	private string[] _labels = [];

	// Row 2 - Outlet bar / Payment mode pie
	private List<ChartSeries<double>> _outletSeries = [];
	private string[] _outletLabels = [];

	private List<ChartSeries<double>> _paymentSeries = [];
	private string[] _paymentLabels = [];

	private readonly LineChartOptions _revenueOptions = new()
	{
		ChartPalette = ["#16a34a"],
		YAxisFormat = "N0",
		MaxNumYAxisTicks = 10,
		ShowLegend = false,
		ShowDataMarkers = true,
		LineStrokeWidth = 2.5,
		InterpolationOption = InterpolationOption.NaturalSpline,
	};

	private readonly BarChartOptions _purchaseOptions = new()
	{
		ChartPalette = ["#dc2626"],
		YAxisFormat = "N0",
		MaxNumYAxisTicks = 10,
		ShowLegend = false,
		YAxisLines = true,
		XAxisLines = false,
	};

	private readonly BarChartOptions _outletOptions = new()
	{
		ChartPalette = ["#2563eb"],
		YAxisFormat = "N0",
		MaxNumYAxisTicks = 10,
		ShowLegend = false,
		YAxisLines = true,
		XAxisLines = false,
		XAxisLabelRotation = 45
	};

	private readonly PieChartOptions _paymentOptions = new()
	{
		ChartPalette = ["#16a34a", "#2563eb", "#8b5cf6", "#f59e0b"],
		ShowValues = true,
	};

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
			// Window: first day of the month 11 months ago → end of this month (12 months total).
			var thisMonthStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
			var windowStart = thisMonthStart.AddMonths(-11);
			var windowEnd = thisMonthStart.AddMonths(1).AddSeconds(-1);

			// SQL does all the grouping and summing; we just read the finished numbers.
			BuildMonthlyTrend(await AnalysisData.LoadDashboardMonthlyTrend(windowStart, windowEnd), thisMonthStart);
			BuildRevenueByOutlet(await AnalysisData.LoadDashboardRevenueByOutlet(windowStart, windowEnd));
			BuildPaymentMix(await AnalysisData.LoadDashboardPaymentMix(windowStart, windowEnd));
		}
		catch { }
		finally { StateHasChanged(); }
	}

	private void BuildMonthlyTrend(List<AnalysisMonthlyTrendModel> rows, DateTime thisMonthStart)
	{
		var buckets = Enumerable.Range(0, 12)
			.Select(i => thisMonthStart.AddMonths(-11 + i))
			.ToList();

		_labels = [.. buckets.Select(b => b.ToString("MMM"))];

		var revenue = new double[12];
		var purchase = new double[12];

		for (int i = 0; i < 12; i++)
		{
			var row = rows.FirstOrDefault(r => r.Year == buckets[i].Year && r.Month == buckets[i].Month);
			revenue[i] = (double)(row?.Revenue ?? 0);
			purchase[i] = (double)(row?.Purchase ?? 0);
		}

		_revenueOptions.YAxisTicks = RoundAxisStep(revenue);
		_purchaseOptions.YAxisTicks = RoundAxisStep(purchase);

		_revenueSeries = [new ChartSeries<double> { Name = "Revenue", Data = revenue }];
		_purchaseSeries = [new ChartSeries<double> { Name = "Purchases", Data = purchase }];
	}

	private void BuildRevenueByOutlet(List<AnalysisOutletModel> rows)
	{
		// Keep the top 7 — SQL already sorted them. The long tail of small outlets only
		// squeezes the bars too thin to read.
		const int topN = 7;
		var top = rows.Take(topN).ToList();

		_outletLabels = [.. top.Select(_ => _.LocationCode)];
		var revenueData = top.Select(_ => (double)_.Revenue).ToArray();

		_outletOptions.YAxisTicks = RoundAxisStep(revenueData);

		_outletSeries = top.Count == 0
			? []
			: [new ChartSeries<double> { Name = "Revenue", Data = revenueData }];
	}

	private void BuildPaymentMix(List<AnalysisPaymentModeModel> rows)
	{
		_paymentLabels = [.. rows.Select(_ => _.PaymentMode)];
		var values = rows.Select(_ => (double)_.Amount).ToArray();

		_paymentSeries = values.Sum() == 0 ? [] : values.AsChartDataSet();
	}

	// Left alone, MudChart starts at a step of 20 and keeps doubling it until the ticks
	// fit, which is how an axis ends up labelled 655,360. Hand it a round step instead:
	// aim for ~8 gridlines, then round that up to the next 1, 2 or 5 times a power of ten.
	private static int RoundAxisStep(double[] values)
	{
		var max = values.Length == 0 ? 0 : values.Max();
		if (max <= 0)
			return 1;

		var rough = max / 8;
		var magnitude = Math.Pow(10, Math.Floor(Math.Log10(rough)));
		var normalized = rough / magnitude;

		var step = normalized <= 1 ? 1 : normalized <= 2 ? 2 : normalized <= 5 ? 5 : 10;
		return (int)(step * magnitude);
	}
}
