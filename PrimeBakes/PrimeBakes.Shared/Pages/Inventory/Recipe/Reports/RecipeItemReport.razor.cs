using PrimeBakes.Shared.Components.Dialog;
using PrimeBakes.Shared.Components.Input;

using PrimeBakesLibrary.Inventory.Purchase.Data;
using PrimeBakesLibrary.Inventory.RawMaterial.Models;
using PrimeBakesLibrary.Inventory.Recipe.Data;
using PrimeBakesLibrary.Inventory.Recipe.Exports;
using PrimeBakesLibrary.Inventory.Recipe.Models;
using PrimeBakesLibrary.Operations.Settings;
using PrimeBakesLibrary.Operations.User;
using PrimeBakesLibrary.Store.Product.Models;
using PrimeBakesLibrary.Utils.Exports;

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

	private DateTime _recipeDateTime = DateTime.Now.Date;

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
		new() { Text = "Open Recipe (Alt + O)", Id = "Open", IconCss = "e-icons e-edit", Target = ".e-content" },
		new() { Text = "Export PDF (Alt + P)", Id = "ExportPDF", IconCss = "e-icons e-export-pdf", Target = ".e-content" },
		new() { Text = "Export Excel (Alt + E)", Id = "ExportExcel", IconCss = "e-icons e-export-excel", Target = ".e-content" },
		new() { Text = "Delete (Del)", Id = "Delete", IconCss = "e-icons e-trash", Target = ".e-content" }
	];

	private SfGrid<RecipeItemOverviewModel> _sfGrid;
	private CustomDatePicker _sfFirstFocus;
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

		if (_sfFirstFocus is not null)
			await _sfFirstFocus.FocusAsync();
	}

	private async Task LoadData()
	{
		_recipeDateTime = await CommonData.LoadCurrentDateTime();

		_rawMaterials = await PurchaseData.LoadRawMaterialByPartyPurchaseDateTime(0, _recipeDateTime);
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
		}
	}
	private async Task ApplyFilters()
	{
		foreach (var item in _allItemOverviews)
		{
			item.Rate = _rawMaterials.FirstOrDefault(r => r.Id == item.ItemId)?.Rate ?? item.Rate;
			item.Amount = item.Quantity * item.Rate;
			item.PerUnit = item.RecipeQuantity > 0 ? item.Amount / item.RecipeQuantity : 0m;
		}

		_itemOverviews = [.. _allItemOverviews.Where(i =>
				(_showDeleted || i.MasterStatus) &&
				(_selectedRawMaterial is null || _selectedRawMaterial.Id == 0 || i.ItemId == _selectedRawMaterial.Id) &&
				(_selectedRawMaterialCategory is null || _selectedRawMaterialCategory.Id == 0 || i.ItemCategoryId == _selectedRawMaterialCategory.Id) &&
				(_selectedProduct is null || _selectedProduct.Id == 0 || i.ProductId == _selectedProduct.Id))
			.OrderBy(i => i.ItemName).ThenBy(i => i.ProductName)];

		if (_sfGrid is not null)
			await _sfGrid.Refresh();
		StateHasChanged();
	}
	#endregion

	#region Changed Events
	private async Task OnRecipeDateChanged(DateTime value)
	{
		_recipeDateTime = value;
		_rawMaterials = await PurchaseData.LoadRawMaterialByPartyPurchaseDateTime(0, _recipeDateTime);
		_rawMaterials = [.. _rawMaterials.OrderBy(r => r.Name)];

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
				isExcel ? ReportExportType.Excel : ReportExportType.PDF);
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
				_recipeDateTime);
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
			case "Open": await AuthenticationService.NavigateToRoute(InventoryRouteNames.Recipe, FormFactor, JSRuntime, NavigationManager); break;
			case "ExportPDF": await ExportSelectedTransaction(); break;
			case "ExportExcel": await ExportSelectedTransaction(true); break;
			case "Delete": await DeleteSelectedTransaction(); break;
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
