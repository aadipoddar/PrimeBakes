using PrimeBakes.Shared.Components.Dialog;
using PrimeBakes.Shared.Components.Input;

using PrimeBakesLibrary.Operations.Location;
using PrimeBakesLibrary.Operations.User;
using PrimeBakesLibrary.Store.Product.Data;
using PrimeBakesLibrary.Store.Product.Exports;
using PrimeBakesLibrary.Store.Product.Models;
using PrimeBakesLibrary.Utils.Exports;

using Syncfusion.Blazor.Grids;

namespace PrimeBakes.Shared.Pages.Store.Product;

public partial class ProductLocationPage
{
	private UserModel _user;
	private bool _isLoading = true;
	private bool _isProcessing = false;

	private ProductLocationModel _productLocation = new();
	private LocationModel _selectedLocation;
	private ProductModel _selectedProduct;

	private List<ProductLocationModel> _productLocations = [];
	private List<ProductLocationOverviewModel> _productLocationOverviews = [];
	private List<LocationModel> _locations = [];
	private List<ProductModel> _products = [];
	private readonly List<ContextMenuItemModel> _gridContextMenuItems =
	[
		new() { Text = "Edit (Insert)", Id = "EditSelectedItem", IconCss = "e-icons e-edit", Target = ".e-content" },
		new() { Text = "Delete (Del)", Id = "DeleteSelectedItem", IconCss = "e-icons e-trash", Target = ".e-content" }
	];

	private SfGrid<ProductLocationOverviewModel> _sfGrid;
	private CustomAutoComplete<LocationModel> _sfFirstFocus;
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
			_user = await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, [UserRoles.Store], true);
			await LoadData();
		}
		catch { NavigationManager.NavigateTo(OperationRouteNames.Dashboard); }
	}

	private async Task LoadData()
	{
		_productLocations = await CommonData.LoadTableData<ProductLocationModel>(StoreNames.ProductLocation);
		_locations = await CommonData.LoadTableDataByStatus<LocationModel>(OperationNames.Location);
		_products = await CommonData.LoadTableDataByStatus<ProductModel>(StoreNames.Product);

		_locations = [.. _locations.OrderBy(l => l.Name)];
		_products = [.. _products.OrderBy(p => p.Name)];

		await LoadOverviews();

		_isLoading = false;
		StateHasChanged();

		if (_sfFirstFocus is not null)
			await _sfFirstFocus.FocusAsync();
	}

	private async Task LoadOverviews()
	{
		if (_productLocation.LocationId > 0)
			_productLocationOverviews = await ProductLocationData.LoadProductLocationOverviewByProductLocation(LocationId: _productLocation.LocationId);
		else
			_productLocationOverviews = await CommonData.LoadTableData<ProductLocationOverviewModel>(StoreNames.ProductLocationOverview);

		if (_sfGrid is not null)
			await _sfGrid.Refresh();
	}
	#endregion

	#region Changed Events
	private async Task OnLocationChanged()
	{
		_productLocation.LocationId = _selectedLocation?.Id ?? 0;
		await LoadOverviews();
		StateHasChanged();
	}

	private void OnProductChanged()
	{
		_productLocation.ProductId = _selectedProduct?.Id ?? 0;
		if (_selectedProduct is null)
			return;

		if (_productLocation.LocationId <= 0)
			return;

		var existing = _productLocations.FirstOrDefault(pl => pl.LocationId == _productLocation.LocationId && pl.ProductId == _productLocation.ProductId);
		if (existing is not null)
		{
			_productLocation.Id = existing.Id;
			_productLocation.Rate = existing.Rate;
		}
		else if (_productLocation.Rate == 0)
			_productLocation.Rate = _selectedProduct.Rate;
	}
	#endregion

	#region Saving
	private async Task SaveTransaction()
	{
		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();

			if (!_user.Admin)
				throw new Exception("You do not have permission to perform this action.");

			await _toastNotification.ShowAsync("Processing", "Please wait while the transaction is being saved...", ToastType.Info);

			_productLocation.LocationId = _selectedLocation?.Id ?? 0;
			_productLocation.ProductId = _selectedProduct?.Id ?? 0;
			await ProductLocationData.SaveTransaction(_productLocation, _user.Id, FormFactor.GetFormFactor() + FormFactor.GetPlatform());

			await _toastNotification.ShowAsync("Saved", "Transaction has been saved successfully.", ToastType.Success);
			ResetPage();
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error While Saving", ex.Message, ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
		}
	}
	#endregion

	#region Actions
	private async Task EditSelectedItem()
	{
		var selectedRecords = await _sfGrid.GetSelectedRecordsAsync();
		if (selectedRecords.Count == 0)
			return;

		var productLocation = _productLocations.FirstOrDefault(pl => pl.Id == selectedRecords[0].Id);
		if (productLocation is null)
		{
			await _toastNotification.ShowAsync("Error while Editing", "Transaction Not Found.", ToastType.Error);
			return;
		}

		_productLocation = new()
		{
			Id = productLocation.Id,
			ProductId = productLocation.ProductId,
			Rate = productLocation.Rate,
			LocationId = productLocation.LocationId
		};

		_selectedLocation = _locations.FirstOrDefault(l => l.Id == productLocation.LocationId);
		_selectedProduct = _products.FirstOrDefault(p => p.Id == productLocation.ProductId);

		StateHasChanged();
		await _sfFirstFocus.FocusAsync();
	}

	private async Task DeleteTransaction(ProductLocationOverviewModel productLocation)
	{
		try
		{
			if (!_user.Admin)
				throw new Exception("You do not have permission to perform this action.");

			_isProcessing = true;
			StateHasChanged();

			await _toastNotification.ShowAsync("Processing", "Deleting transaction...", ToastType.Info);

			await ProductLocationData.DeleteTransaction(productLocation, _user.Id, FormFactor.GetFormFactor() + FormFactor.GetPlatform());

			await _toastNotification.ShowAsync("Success", $"Transaction {productLocation.Name} has been deleted successfully.", ToastType.Success);
			ResetPage();
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"An error occurred while deleting transaction: {ex.Message}", ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
			StateHasChanged();
		}
	}

	private async Task DeleteSelectedItem()
	{
		var selectedRecords = await _sfGrid.GetSelectedRecordsAsync();
		if (selectedRecords.Count == 0)
			return;

		var record = selectedRecords[0];

		await ShowConfirmation("Delete",
			$"Are you sure you want to delete product location {record.Name}",
			() => DeleteTransaction(record));
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
	private async Task ExportMaster(bool isExcel = false)
	{
		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();
			await _toastNotification.ShowAsync("Processing", "Generating the Export...", ToastType.Info);

			var (stream, fileName) = await ProductLocationExport.ExportMaster(_productLocationOverviews, isExcel ? ReportExportType.Excel : ReportExportType.PDF);
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
	private async Task OnGridContextMenuItemClicked(ContextMenuClickEventArgs<ProductLocationOverviewModel> args)
	{
		switch (args.Item.Id)
		{
			case "EditSelectedItem": await EditSelectedItem(); break;
			case "DeleteSelectedItem": await DeleteSelectedItem(); break;
		}
	}

	private void ResetPage() => PageRefresh.Request();
	#endregion
}
