using PrimeBakes.Library.Inventory.Kitchen.Models;

using Syncfusion.Blazor.Grids;

namespace PrimeBakes.Shared.Components.Dashboard;

public partial class DashboardKitchenSummary
{
	private List<KitchenSummaryModel> _kitchenSummaries = [];

	private List<KitchenModel> _kitchens = [];
	private List<KitchenIssueOverviewModel> _kitchenIssue = [];
	private List<KitchenIssueReturnOverviewModel> _kitchenIssueReturn = [];
	private List<KitchenProductionOverviewModel> _kitchenProduction = [];
	private List<KitchenProductionReturnOverviewModel> _kitchenProductionReturn = [];

	private SfGrid<KitchenSummaryModel> _sfGrid;

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
			var currentDateTime = await CommonData.LoadCurrentDateTime();
			var fromDate = DateOnly.FromDateTime(currentDateTime).ToDateTime(TimeOnly.MinValue);
			var toDate = fromDate;

			_kitchens = await CommonData.LoadTableDataByStatus<KitchenModel>(InventoryNames.Kitchen);

			_kitchenIssue = await CommonData.LoadTableDataByDate<KitchenIssueOverviewModel>(InventoryNames.KitchenIssueOverview, fromDate, toDate);
			_kitchenIssueReturn = await CommonData.LoadTableDataByDate<KitchenIssueReturnOverviewModel>(InventoryNames.KitchenIssueReturnOverview, fromDate, toDate);
			_kitchenProduction = await CommonData.LoadTableDataByDate<KitchenProductionOverviewModel>(InventoryNames.KitchenProductionOverview, fromDate, toDate);
			_kitchenProductionReturn = await CommonData.LoadTableDataByDate<KitchenProductionReturnOverviewModel>(InventoryNames.KitchenProductionReturnOverview, fromDate, toDate);

			_kitchenIssue = [.. _kitchenIssue.Where(_ => _.Status)];
			_kitchenIssueReturn = [.. _kitchenIssueReturn.Where(_ => _.Status)];
			_kitchenProduction = [.. _kitchenProduction.Where(_ => _.Status)];
			_kitchenProductionReturn = [.. _kitchenProductionReturn.Where(_ => _.Status)];

			CalculateTotals();
		}
		catch { }
		finally { StateHasChanged(); }
	}

	// Mirrors KitchenSummaryReport.CalculateTotals so both screens report the same numbers.
	private void CalculateTotals()
	{
		_kitchenSummaries = [];

		foreach (var kitchen in _kitchens)
		{
			var summary = new KitchenSummaryModel
			{
				KitchenId = kitchen.Id,
				KitchenName = kitchen.Name,
			};

			var kitchenIssues = _kitchenIssue.Where(_ => _.KitchenId == kitchen.Id).ToList();
			var kitchenIssueReturns = _kitchenIssueReturn.Where(_ => _.KitchenId == kitchen.Id).ToList();
			var kitchenProductions = _kitchenProduction.Where(_ => _.KitchenId == kitchen.Id).ToList();
			var kitchenProductionReturns = _kitchenProductionReturn.Where(_ => _.KitchenId == kitchen.Id).ToList();

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

	private void OnGridContextMenuItemClicked(ContextMenuClickEventArgs<KitchenSummaryModel> args)
	{
		if (args.Item.Id == "OpenReport")
			NavigationManager.NavigateTo(InventoryRouteNames.KitchenSummaryReport);
	}
}
