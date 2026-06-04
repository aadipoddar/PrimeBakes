using PrimeBakes.Shared.Components.Dialog;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Exporting.Inventory.Recipe;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Inventory;
using PrimeBakesLibrary.Models.Operations;

using Syncfusion.Blazor.Grids;

namespace PrimeBakes.Shared.Pages.Inventory.Recipe.Reports;

public partial class RecipeReport
{
	private bool _isLoading = true;
	private bool _isProcessing = false;

	private List<RecipeOverviewModel> _recipeOverviews = [];
	private readonly List<ContextMenuItemModel> _gridContextMenuItems =
	[
		new() { Text = "Export PDF (Alt + P)", Id = "ExportPDF", IconCss = "e-icons e-export-pdf", Target = ".e-content" },
		new() { Text = "Export Excel (Alt + E)", Id = "ExportExcel", IconCss = "e-icons e-export-excel", Target = ".e-content" }
	];

	private SfGrid<RecipeOverviewModel> _sfGrid;
	private ToastNotification _toastNotification;

	#region Load Data
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, [UserRoles.Inventory, UserRoles.Reports], true);
		await LoadData();
	}

	private async Task LoadData()
	{
		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();

			_recipeOverviews = await CommonData.LoadTableDataByStatus<RecipeOverviewModel>(ViewNames.RecipeOverview);
			_recipeOverviews = [.. _recipeOverviews.OrderBy(r => r.ProductName)];
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error Loading Data", ex.Message, ToastType.Error);
		}
		finally
		{
			if (_sfGrid is not null)
				await _sfGrid.Refresh();

			_isProcessing = false;
			_isLoading = false;
			StateHasChanged();
		}
	}
	#endregion

	#region Export
	private async Task ExportPdf()
	{
		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();
			await _toastNotification.ShowAsync("Processing", "Generating PDF...", ToastType.Info);

			var (stream, fileName) = await RecipeReportExport.ExportReport(_recipeOverviews, ReportExportType.PDF);
			await SaveAndViewService.SaveAndView(fileName, stream);

			await _toastNotification.ShowAsync("Success", "PDF downloaded successfully.", ToastType.Success);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", ex.Message, ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
			StateHasChanged();
		}
	}

	private async Task ExportExcel()
	{
		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();
			await _toastNotification.ShowAsync("Processing", "Generating Excel...", ToastType.Info);

			var (stream, fileName) = await RecipeReportExport.ExportReport(_recipeOverviews, ReportExportType.Excel);
			await SaveAndViewService.SaveAndView(fileName, stream);

			await _toastNotification.ShowAsync("Success", "Excel downloaded successfully.", ToastType.Success);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", ex.Message, ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
			StateHasChanged();
		}
	}
	#endregion

	#region Actions
	private async Task OnGridContextMenuItemClicked(ContextMenuClickEventArgs<RecipeOverviewModel> args)
	{
		switch (args.Item.Id)
		{
			case "ExportPDF":
				await ExportSelectedPdf(args.RowInfo.RowData);
				break;
			case "ExportExcel":
				await ExportSelectedExcel(args.RowInfo.RowData);
				break;
		}
	}

	private async Task OpenSelectedRecipe()
	{
		if (_sfGrid is null || _sfGrid.SelectedRecords is null || _sfGrid.SelectedRecords.Count == 0)
			return;

		await AuthenticationService.NavigateToRoute(PageRouteNames.Recipe, FormFactor, JSRuntime, NavigationManager);
	}

	private async Task ExportSelectedPdf(RecipeOverviewModel item)
	{
		if (item is null || item.Id == 0)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();
			await _toastNotification.ShowAsync("Processing", "Generating PDF invoice...", ToastType.Info);

			var currentDateTime = await CommonData.LoadCurrentDateTime();
			var (stream, fileName) = await RecipeInvoiceExport.ExportInvoice(item.Id, InvoiceExportType.PDF, currentDateTime);
			await SaveAndViewService.SaveAndView(fileName, stream);

			await _toastNotification.ShowAsync("Success", "PDF invoice downloaded.", ToastType.Success);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", ex.Message, ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
			StateHasChanged();
		}
	}

	private async Task ExportSelectedExcel(RecipeOverviewModel item)
	{
		if (item is null || item.Id == 0)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();
			await _toastNotification.ShowAsync("Processing", "Generating Excel invoice...", ToastType.Info);

			var currentDateTime = await CommonData.LoadCurrentDateTime();
			var (stream, fileName) = await RecipeInvoiceExport.ExportInvoice(item.Id, InvoiceExportType.Excel, currentDateTime);
			await SaveAndViewService.SaveAndView(fileName, stream);

			await _toastNotification.ShowAsync("Success", "Excel invoice downloaded.", ToastType.Success);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", ex.Message, ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
			StateHasChanged();
		}
	}
	#endregion

	#region Utilities
	private async Task OnMenuSelected(Syncfusion.Blazor.Navigations.MenuEventArgs<Syncfusion.Blazor.Navigations.MenuItem> args)
	{
		switch (args.Item.Id)
		{
			case "NewRecipe":
				await NavigateToRecipePage();
				break;
			case "Refresh":
				await LoadData();
				break;
			case "ExportPdf":
				await ExportPdf();
				break;
			case "ExportExcel":
				await ExportExcel();
				break;
		}
	}

	private async Task NavigateToRecipePage() =>
		await AuthenticationService.NavigateToRoute(PageRouteNames.Recipe, FormFactor, JSRuntime, NavigationManager);

	private void NavigateBack() =>
		NavigationManager.NavigateTo(PageRouteNames.InventoryDashboard);
	#endregion
}
