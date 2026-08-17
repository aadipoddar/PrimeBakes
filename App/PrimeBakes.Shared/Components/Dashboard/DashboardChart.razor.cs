using MudBlazor;

using PrimeBakes.Data.Operations.Analysis;
using PrimeBakes.Models.Operations.Analysis;

namespace PrimeBakes.Shared.Components.Dashboard;

public partial class DashboardChart
{
	// Revenue line / Purchase bar
	private List<ChartSeries<double>> _revenueSeries = [];
	private List<ChartSeries<double>> _purchaseSeries = [];
	private string[] _labels = [];

	// Top products bar
	private List<ChartSeries<double>> _productSeries = [];
	private string[] _productLabels = [];

	// Top raw materials bar
	private List<ChartSeries<double>> _materialSeries = [];
	private string[] _materialLabels = [];

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

	private readonly BarChartOptions _productOptions = new()
	{
		ChartPalette = ["#7c3aed"],
		YAxisFormat = "N0",
		MaxNumYAxisTicks = 10,
		ShowLegend = false,
		YAxisLines = true,
		XAxisLines = false,
		XAxisLabelRotation = 45,
	};

	private readonly BarChartOptions _materialOptions = new()
	{
		ChartPalette = ["#ea580c"],
		YAxisFormat = "N0",
		MaxNumYAxisTicks = 10,
		ShowLegend = false,
		YAxisLines = true,
		XAxisLines = false,
		XAxisLabelRotation = 45,
	};

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
			// Window: first day of the month 11 months ago → end of this month (12 months total).
			var thisMonthStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
			var windowStart = thisMonthStart.AddMonths(-11);
			var windowEnd = thisMonthStart.AddMonths(1).AddSeconds(-1);

			// SQL does all the grouping and summing; we just read the finished numbers.
			BuildMonthlyTrend(await AnalysisData.LoadDashboardMonthlyTrend(windowStart, windowEnd), thisMonthStart);
			BuildTopProducts(await AnalysisData.LoadDashboardTopProducts(thisMonthStart, DateTime.Now));
			BuildTopRawMaterials(await AnalysisData.LoadDashboardTopRawMaterials(thisMonthStart, DateTime.Now));
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

	private void BuildTopProducts(List<AnalysisTopProductModel> rows)
	{
		// SQL already trimmed this to the top ten by units and sorted it.
		_productLabels = [.. rows.Select(_ => _.ItemName)];
		var quantities = rows.Select(_ => (double)_.Quantity).ToArray();

		_productOptions.YAxisTicks = RoundAxisStep(quantities);

		_productSeries = rows.Count == 0
			? []
			: [new ChartSeries<double> { Name = "Units", Data = quantities }];
	}

	private void BuildTopRawMaterials(List<AnalysisTopRawMaterialModel> rows)
	{
		// SQL already trimmed this to the top ten by value and sorted it.
		_materialLabels = [.. rows.Select(_ => _.ItemName)];
		var amounts = rows.Select(_ => (double)_.Amount).ToArray();

		_materialOptions.YAxisTicks = RoundAxisStep(amounts);

		_materialSeries = rows.Count == 0
			? []
			: [new ChartSeries<double> { Name = "Consumed", Data = amounts }];
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
