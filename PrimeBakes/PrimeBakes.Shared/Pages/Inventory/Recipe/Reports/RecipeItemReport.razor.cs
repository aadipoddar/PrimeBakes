using PrimeBakes.Library.Inventory.Purchase.Data;
using PrimeBakes.Library.Inventory.RawMaterial.Models;
using PrimeBakes.Library.Inventory.Recipe.Data;
using PrimeBakes.Library.Inventory.Recipe.Exports;
using PrimeBakes.Library.Inventory.Recipe.Models;
using PrimeBakes.Library.Operations.Settings;
using PrimeBakes.Library.Operations.User;
using PrimeBakes.Library.Store.Product.Models;
using PrimeBakes.Library.Utils.Exports;
using PrimeBakes.Shared.Components.Dialog;
using PrimeBakes.Shared.Components.Input;

using Syncfusion.Blazor.Grids;

namespace PrimeBakes.Shared.Pages.Inventory.Recipe.Reports;

public partial class RecipeItemReport : IAsyncDisposable
{
	private PeriodicTimer _autoRefreshTimer;
	private CancellationTokenSource _autoRefreshCts;

	private UserModel _user;

	private bool _isLoading = true;
	private bool _isProcessing = false;
	private bool _showDeleted = false;

	private DateTime _costAsOnDateTime = DateTime.Now.Date;
	private DateTime _effectiveDateTime = DateTime.Now.Date;
	private int _deductFilter = YesNoFilterOptions.All;
	private YesNoFilterOption _selectedDeduct;

	private RawMaterialModel? _selectedRawMaterial = null;
	private RawMaterialCategoryModel? _selectedRawMaterialCategory = null;
	private ProductModel? _selectedProduct = null;

	private List<RawMaterialModel> _rawMaterials = [];
	private List<RawMaterialCategoryModel> _rawMaterialCategories = [];
	private List<ProductModel> _products = [];
	private List<RecipeItemOverviewModel> _itemOverviews = [];
	private List<RecipeItemOverviewModel> _allItemOverviews = [];

	private readonly List<ContextMenuItemModel> _gridContextMenuItems =
	[
		new() { Text = "View Recipe (Alt + O)", Id = "View", IconCss = "e-icons e-eye", Target = ".e-content" },
		new() { Text = "Export PDF (Alt + P)", Id = "ExportPDF", IconCss = "e-icons e-export-pdf", Target = ".e-content" },
		new() { Text = "Export Excel (Alt + E)", Id = "ExportExcel", IconCss = "e-icons e-export-excel", Target = ".e-content" },
		new() { Text = "Delete (Del)", Id = "DeleteTransaction", IconCss = "e-icons e-trash", Target = ".e-content" }
	];

	private SfGrid<RecipeItemOverviewModel> _sfGrid;
	private CustomDatePicker _firstFocus;
	private ToastNotification _toastNotification;
	private ConfirmationDialog _confirmationDialog;

	private string _confirmTitle = string.Empty;
	private string _confirmMessage = string.Empty;
	private Func<Task> _confirmAction;

	#region Load Data
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		try
		{
			_user = await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, [UserRoles.Inventory, UserRoles.Reports], true);
			await InitializePage();
		}
		catch { NavigationManager.NavigateTo(OperationRouteNames.Dashboard); }
	}

	private async Task InitializePage()
	{
		await LoadData();
		await LoadItemOverviews();
		await StartAutoRefresh();

		_isLoading = false;
		StateHasChanged();

		if (_firstFocus is not null)
			await _firstFocus.FocusAsync();
	}

	private async Task LoadData()
	{
		_costAsOnDateTime = await CommonData.LoadCurrentDateTime();
		_effectiveDateTime = _costAsOnDateTime;

		_rawMaterials = await PurchaseData.LoadRawMaterialByPartyPurchaseDateTime(0, _costAsOnDateTime);
		_rawMaterialCategories = await CommonData.LoadTableDataByStatus<RawMaterialCategoryModel>(InventoryNames.RawMaterialCategory);
		_products = await CommonData.LoadTableDataByStatus<ProductModel>(StoreNames.Product);

		_rawMaterials = [.. _rawMaterials.OrderBy(r => r.Name)];
		_rawMaterialCategories = [.. _rawMaterialCategories.OrderBy(c => c.Name)];
		_products = [.. _products.OrderBy(p => p.Name)];
	}

	private async Task LoadItemOverviews()
	{
		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();
			await _toastNotification.ShowAsync("Loading", "Fetching recipe items...", ToastType.Info);

			_allItemOverviews = await CommonData.LoadTableData<RecipeItemOverviewModel>(InventoryNames.RecipeItemOverview);

			await ApplyFilters();
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"Failed to load recipe items: {ex.Message}", ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
			StateHasChanged();
			await _toastNotification.HideAllInfoAsync();
		}
	}
	private async Task ApplyFilters()
	{
		var asOn = DateOnly.FromDateTime(_effectiveDateTime);
		var statusFiltered = _allItemOverviews.Where(i => (_showDeleted || i.MasterStatus) && i.FromDate <= asOn &&
			(_deductFilter == YesNoFilterOptions.All ||
				(_deductFilter == YesNoFilterOptions.Yes && i.Deduct) ||
				(_deductFilter == YesNoFilterOptions.No && !i.Deduct))).ToList();
		var effectiveFromDate = statusFiltered
			.GroupBy(i => new { i.ProductId, i.Deduct })
			.ToDictionary(g => g.Key, g => g.Max(i => i.FromDate));

		_itemOverviews = [.. statusFiltered.Where(i =>
				i.FromDate == effectiveFromDate[new { i.ProductId, i.Deduct }] &&
				(_selectedRawMaterial is null || _selectedRawMaterial.Id == 0 || i.ItemId == _selectedRawMaterial.Id) &&
				(_selectedRawMaterialCategory is null || _selectedRawMaterialCategory.Id == 0 || i.ItemCategoryId == _selectedRawMaterialCategory.Id) &&
				(_selectedProduct is null || _selectedProduct.Id == 0 || i.ProductId == _selectedProduct.Id))
			.OrderBy(i => i.ItemName).ThenBy(i => i.ProductName).ThenByDescending(i => i.Deduct)];

		_rawMaterials = await PurchaseData.LoadRawMaterialByPartyPurchaseDateTime(0, _costAsOnDateTime);
		_rawMaterials = [.. _rawMaterials.OrderBy(r => r.Name)];

		foreach (var item in _itemOverviews)
		{
			item.Rate = _rawMaterials.FirstOrDefault(r => r.Id == item.ItemId)?.Rate ?? item.Rate;
			item.Amount = item.Quantity * item.Rate;
			item.PerUnit = item.RecipeQuantity > 0 ? item.Amount / item.RecipeQuantity : 0m;
		}

		if (_sfGrid is not null) await _sfGrid.Refresh();
		StateHasChanged();
	}
	#endregion

	#region Changed Events
	private async Task OnCostAsOnDateChanged(DateTime value)
	{
		_costAsOnDateTime = value;
		await ApplyFilters();
	}

	private async Task OnEffectiveDateChanged(DateTime value)
	{
		_effectiveDateTime = value;
		await ApplyFilters();
	}

	private async Task OnDeductFilterChanged(YesNoFilterOption value)
	{
		_selectedDeduct = value;
		_deductFilter = value?.Id ?? YesNoFilterOptions.All;
		await ApplyFilters();
	}

	private async Task OnRawMaterialChanged(RawMaterialModel value)
	{
		_selectedRawMaterial = value;
		await ApplyFilters();
	}

	private async Task OnRawMaterialCategoryChanged(RawMaterialCategoryModel value)
	{
		_selectedRawMaterialCategory = value;
		await ApplyFilters();
	}

	private async Task OnProductChanged(ProductModel value)
	{
		_selectedProduct = value;
		await ApplyFilters();
	}
	#endregion

	#region Actions
	private async Task ViewSelectedRecipe()
	{
		if (_isProcessing || _sfGrid is null || _sfGrid.SelectedRecords is null || _sfGrid.SelectedRecords.Count == 0)
			return;

		var record = _sfGrid.SelectedRecords.First();
		if (!record.MasterStatus)
		{
			await _toastNotification.ShowAsync("Cannot Open", "The selected recipe is deleted. Please recover it first.", ToastType.Warning);
			return;
		}

		await AuthenticationService.NavigateToRoute($"{InventoryRouteNames.Recipe}/{record.MasterId}", FormFactor, JSRuntime, NavigationManager);
	}

	private async Task DeleteTransaction(int masterId, string productName)
	{
		if (_isProcessing || masterId == 0)
			return;

		try
		{
			if (!_user.Admin)
				throw new UnauthorizedAccessException("You do not have permission for the action.");

			_isProcessing = true;
			StateHasChanged();

			await _toastNotification.ShowAsync("Processing", "Deleting recipe...", ToastType.Info);

			var platform = FormFactor.GetFormFactor() + FormFactor.GetPlatform();

			var recipe = await CommonData.LoadTableDataById<RecipeModel>(InventoryNames.Recipe, masterId)
				?? throw new Exception("Recipe not found.");
			await RecipeData.DeleteTransaction(recipe, _user.Id, platform);

			await _toastNotification.ShowAsync("Success", $"Recipe {productName} has been deleted successfully.", ToastType.Success);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"An error occurred while deleting recipe: {ex.Message}", ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
			StateHasChanged();
			await LoadItemOverviews();
		}
	}

	private async Task DeleteSelectedTransaction()
	{
		if (_sfGrid is null || _sfGrid.SelectedRecords is null || _sfGrid.SelectedRecords.Count == 0)
			return;

		var record = _sfGrid.SelectedRecords.First();

		if (!record.MasterStatus)
		{
			await _toastNotification.ShowAsync("Already Deleted", $"The recipe for {record.ProductName} is already deleted.", ToastType.Warning);
			return;
		}

		await ShowConfirmation("Delete",
			$"Are you sure you want to delete the recipe {record.ProductName}",
			() => DeleteTransaction(record.MasterId, record.ProductName));
	}

	private async Task ShowConfirmation(string title, string message, Func<Task> action)
	{
		_confirmTitle = title;
		_confirmMessage = message;
		_confirmAction = action;
		StateHasChanged();
		await _confirmationDialog.ShowAsync();
	}

	private async Task OnConfirmed()
	{
		await _confirmationDialog.HideAsync();
		if (_confirmAction is not null)
			await _confirmAction();
		_confirmAction = null;
	}

	private async Task OnCancelled()
	{
		_confirmAction = null;
		await _confirmationDialog.HideAsync();
	}
	#endregion

	#region Exporting
	private async Task ExportReport(bool isExcel = false)
	{
		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();
			await _toastNotification.ShowAsync("Processing", "Generating the Export...", ToastType.Info);

			var (stream, fileName) = await RecipeReportExport.ExportItemReport(
				_itemOverviews,
				isExcel ? ReportExportType.Excel : ReportExportType.PDF,
				DateOnly.FromDateTime(_effectiveDateTime),
				DateOnly.FromDateTime(_costAsOnDateTime),
				_selectedDeduct?.Name,
				_selectedRawMaterial?.Name,
				_selectedRawMaterialCategory?.Name,
				_selectedProduct?.Name);
			await SaveAndViewService.SaveAndView(fileName, stream);

			await _toastNotification.ShowAsync("Exported", "The export has been downloaded successfully.", ToastType.Success);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error While Exporting", ex.Message, ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
			StateHasChanged();
		}
	}

	private async Task ExportSelectedTransaction(bool isExcel = false)
	{
		if (_isProcessing || _sfGrid is null || _sfGrid.SelectedRecords is null || _sfGrid.SelectedRecords.Count == 0)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();
			await _toastNotification.ShowAsync("Processing", "Generating the Export...", ToastType.Info);

			var (stream, fileName) = await RecipeInvoiceExport.ExportInvoice(
				_sfGrid.SelectedRecords.First().MasterId,
				isExcel ? InvoiceExportType.Excel : InvoiceExportType.PDF,
				_costAsOnDateTime);
			await SaveAndViewService.SaveAndView(fileName, stream);

			await _toastNotification.ShowAsync("Exported", "The export has been downloaded successfully.", ToastType.Success);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error While Exporting", ex.Message, ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
			StateHasChanged();
		}
	}
	#endregion

	#region Utilities
	private async Task OnGridContextMenuItemClicked(ContextMenuClickEventArgs<RecipeItemOverviewModel> args)
	{
		switch (args.Item.Id)
		{
			case "View": await ViewSelectedRecipe(); break;
			case "ExportPDF": await ExportSelectedTransaction(); break;
			case "ExportExcel": await ExportSelectedTransaction(true); break;
			case "DeleteTransaction": await DeleteSelectedTransaction(); break;
		}
	}

	private async Task ToggleDeleted()
	{
		_showDeleted = !_showDeleted;
		await ApplyFilters();
	}

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
				await LoadItemOverviews();
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
