using PrimeBakes.Shared.Components.Dialog;

using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Restaurant.Dining;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Exporting.Restaurant.Dining;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Operations;
using PrimeBakesLibrary.Models.Restuarant.Dining;

using Syncfusion.Blazor.Grids;

namespace PrimeBakes.Shared.Pages.Restaurant.Dining;

public partial class DiningAreaPage : IAsyncDisposable
{
	private HotKeysContext _hotKeysContext;
	private bool _isLoading = true;
	private bool _isProcessing = false;
	private bool _showDeleted = false;

	private DiningAreaModel _diningArea = new();

	private List<DiningAreaModel> _diningAreas = [];
	private List<LocationModel> _locations = [];

	private string _selectedLocationName = string.Empty;

	private SfGrid<DiningAreaModel> _sfGrid;
	private DeleteConfirmationDialog _deleteConfirmationDialog;
	private RecoverConfirmationDialog _recoverConfirmationDialog;

	private int _deleteDiningAreaId = 0;
	private string _deleteDiningAreaName = string.Empty;

	private int _recoverDiningAreaId = 0;
	private string _recoverDiningAreaName = string.Empty;

	private ToastNotification _toastNotification;

	#region Load Data
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, [UserRoles.Admin], true);
		await LoadData();
		_isLoading = false;
		StateHasChanged();
	}

	private async Task LoadData()
	{
		_hotKeysContext = HotKeys.CreateContext()
			.Add(ModCode.Ctrl, Code.S, SaveDiningArea, "Save", Exclude.None)
			.Add(ModCode.Ctrl, Code.E, ExportExcel, "Export Excel", Exclude.None)
			.Add(ModCode.Ctrl, Code.P, ExportPdf, "Export PDF", Exclude.None)
			.Add(ModCode.Ctrl, Code.N, ResetPage, "Reset the page", Exclude.None)
			.Add(ModCode.Ctrl, Code.L, Logout, "Logout", Exclude.None)
			.Add(ModCode.Ctrl, Code.B, NavigateBack, "Back", Exclude.None)
			.Add(ModCode.Ctrl, Code.D, NavigateToDashboard, "Dashboard", Exclude.None)
			.Add(Code.Insert, EditSelectedItem, "Edit selected", Exclude.None)
			.Add(Code.Delete, DeleteSelectedItem, "Delete selected", Exclude.None);

		_locations = await CommonData.LoadTableDataByStatus<LocationModel>(TableNames.Location);
		_diningAreas = await CommonData.LoadTableData<DiningAreaModel>(TableNames.DiningArea);

		if (!_showDeleted)
			_diningAreas = [.. _diningAreas.Where(da => da.Status)];

		if (_sfGrid is not null)
			await _sfGrid.Refresh();
	}
	#endregion

	#region Change Events
	private void OnLocationChange(Syncfusion.Blazor.DropDowns.ChangeEventArgs<string, LocationModel> args)
	{
		if (args.ItemData is not null)
		{
			_diningArea.LocationId = args.ItemData.Id;
			_selectedLocationName = args.ItemData.Name;
		}
		else
		{
			_diningArea.LocationId = 0;
			_selectedLocationName = string.Empty;
		}
	}
	#endregion

	#region Actions
	private void OnEditDiningArea(DiningAreaModel diningArea)
	{
		_diningArea = new()
		{
			Id = diningArea.Id,
			Name = diningArea.Name,
			LocationId = diningArea.LocationId,
			Remarks = diningArea.Remarks,
			Status = diningArea.Status
		};

		var location = _locations.FirstOrDefault(l => l.Id == diningArea.LocationId);
		_selectedLocationName = location?.Name ?? string.Empty;

		StateHasChanged();
	}

	private async Task ShowDeleteConfirmation(int id, string name)
	{
		_deleteDiningAreaId = id;
		_deleteDiningAreaName = name;
		await _deleteConfirmationDialog.ShowAsync();
	}

	private async Task CancelDelete()
	{
		_deleteDiningAreaId = 0;
		_deleteDiningAreaName = string.Empty;
		await _deleteConfirmationDialog.HideAsync();
	}

	private async Task ConfirmDelete()
	{
		try
		{
			_isProcessing = true;
			await _deleteConfirmationDialog.HideAsync();

			var diningArea = _diningAreas.FirstOrDefault(da => da.Id == _deleteDiningAreaId);
			if (diningArea is null)
			{
				await _toastNotification.ShowAsync("Error", "Dining area not found.", ToastType.Error);
				return;
			}

			diningArea.Status = false;
			await DiningAreaData.InsertDiningArea(diningArea);

			await _toastNotification.ShowAsync("Deleted", $"Dining area '{diningArea.Name}' has been deleted successfully.", ToastType.Success);
			NavigationManager.NavigateTo(PageRouteNames.DiningArea, true);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"Failed to delete dining area: {ex.Message}", ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
			_deleteDiningAreaId = 0;
			_deleteDiningAreaName = string.Empty;
		}
	}

	private async Task ShowRecoverConfirmation(int id, string name)
	{
		_recoverDiningAreaId = id;
		_recoverDiningAreaName = name;
		await _recoverConfirmationDialog.ShowAsync();
	}

	private async Task CancelRecover()
	{
		_recoverDiningAreaId = 0;
		_recoverDiningAreaName = string.Empty;
		await _recoverConfirmationDialog.HideAsync();
	}

	private async Task ToggleDeleted()
	{
		_showDeleted = !_showDeleted;
		await LoadData();
		StateHasChanged();
	}

	private async Task ConfirmRecover()
	{
		try
		{
			_isProcessing = true;
			await _recoverConfirmationDialog.HideAsync();

			var diningArea = _diningAreas.FirstOrDefault(da => da.Id == _recoverDiningAreaId);
			if (diningArea is null)
			{
				await _toastNotification.ShowAsync("Error", "Dining area not found.", ToastType.Error);
				return;
			}

			diningArea.Status = true;
			await DiningAreaData.InsertDiningArea(diningArea);

			await _toastNotification.ShowAsync("Recovered", $"Dining area '{diningArea.Name}' has been recovered successfully.", ToastType.Success);
			NavigationManager.NavigateTo(PageRouteNames.DiningArea, true);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"Failed to recover dining area: {ex.Message}", ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
			_recoverDiningAreaId = 0;
			_recoverDiningAreaName = string.Empty;
		}
	}
	#endregion

	#region Saving
	private async Task<bool> ValidateForm()
	{
		_diningArea.Name = _diningArea.Name?.Trim() ?? "";
		_diningArea.Name = _diningArea.Name?.ToUpper() ?? "";
		_diningArea.Remarks = _diningArea.Remarks?.Trim() ?? "";
		_diningArea.Status = true;

		if (string.IsNullOrWhiteSpace(_diningArea.Name))
		{
			await _toastNotification.ShowAsync("Validation", "Dining area name is required.", ToastType.Warning);
			return false;
		}

		if (_diningArea.LocationId <= 0)
		{
			await _toastNotification.ShowAsync("Validation", "Please select a location.", ToastType.Warning);
			return false;
		}

		if (string.IsNullOrWhiteSpace(_diningArea.Remarks))
			_diningArea.Remarks = null;

		var allDiningAreas = await CommonData.LoadTableData<DiningAreaModel>(TableNames.DiningArea);

		if (_diningArea.Id > 0)
		{
			var existing = allDiningAreas.FirstOrDefault(_ => _.Id != _diningArea.Id && _.Name.Equals(_diningArea.Name, StringComparison.OrdinalIgnoreCase));
			if (existing is not null)
			{
				await _toastNotification.ShowAsync("Duplicate", $"Dining area '{_diningArea.Name}' already exists.", ToastType.Warning);
				return false;
			}
		}
		else
		{
			var existing = allDiningAreas.FirstOrDefault(_ => _.Name.Equals(_diningArea.Name, StringComparison.OrdinalIgnoreCase));
			if (existing is not null)
			{
				await _toastNotification.ShowAsync("Duplicate", $"Dining area '{_diningArea.Name}' already exists.", ToastType.Warning);
				return false;
			}
		}

		return true;
	}

	private async Task SaveDiningArea()
	{
		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();

			if (!await ValidateForm())
			{
				_isProcessing = false;
				return;
			}

			await _toastNotification.ShowAsync("Processing", "Saving dining area...", ToastType.Info);

			await DiningAreaData.InsertDiningArea(_diningArea);

			await _toastNotification.ShowAsync("Saved", $"Dining area '{_diningArea.Name}' saved successfully.", ToastType.Success);
			NavigationManager.NavigateTo(PageRouteNames.DiningArea, true);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"Failed to save dining area: {ex.Message}", ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
		}
	}
	#endregion

	#region Exporting
	private async Task ExportExcel()
	{
		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();
			await _toastNotification.ShowAsync("Processing", "Exporting to Excel...", ToastType.Info);

			var exportData = _diningAreas.Select(da =>
			{
				var location = _locations.FirstOrDefault(l => l.Id == da.LocationId);
				return new
				{
					da.Id,
					da.Name,
					LocationName = location?.Name ?? "",
					da.Remarks,
					da.Status
				};
			});

			var enrichedModels = exportData.Select(d => new DiningAreaModel
			{
				Id = d.Id,
				Name = d.Name,
				Remarks = d.Remarks,
				Status = d.Status
			}).ToList();

			var (stream, fileName) = await DiningAreaExport.ExportMaster(enrichedModels, ReportExportType.Excel);
			await SaveAndViewService.SaveAndView(fileName, stream);

			await _toastNotification.ShowAsync("Success", "Dining area data exported to Excel successfully.", ToastType.Success);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"Excel export failed: {ex.Message}", ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
			StateHasChanged();
		}
	}

	private async Task ExportPdf()
	{
		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();
			await _toastNotification.ShowAsync("Processing", "Exporting to PDF...", ToastType.Info);

			var enrichedModels = _diningAreas.ToList();

			var (stream, fileName) = await DiningAreaExport.ExportMaster(enrichedModels, ReportExportType.PDF);
			await SaveAndViewService.SaveAndView(fileName, stream);

			await _toastNotification.ShowAsync("Success", "Dining area data exported to PDF successfully.", ToastType.Success);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"PDF export failed: {ex.Message}", ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
			StateHasChanged();
		}
	}
	#endregion

	#region Utilities
	private async Task EditSelectedItem()
	{
		var selectedRecords = await _sfGrid.GetSelectedRecordsAsync();
		if (selectedRecords.Count > 0)
			OnEditDiningArea(selectedRecords[0]);
	}

	private async Task DeleteSelectedItem()
	{
		var selectedRecords = await _sfGrid.GetSelectedRecordsAsync();
		if (selectedRecords.Count > 0)
		{
			if (selectedRecords[0].Status)
				await ShowDeleteConfirmation(selectedRecords[0].Id, selectedRecords[0].Name);
			else
				await ShowRecoverConfirmation(selectedRecords[0].Id, selectedRecords[0].Name);
		}
	}

	private void ResetPage() =>
		NavigationManager.NavigateTo(PageRouteNames.DiningArea, true);

	private void NavigateBack() =>
		NavigationManager.NavigateTo(PageRouteNames.RestaurantDashboard);

	private void NavigateToDashboard() =>
		NavigationManager.NavigateTo(PageRouteNames.Dashboard);

	private async Task Logout() =>
		await AuthenticationService.Logout(DataStorageService, NavigationManager, NotificationService, VibrationService);

	public async ValueTask DisposeAsync()
	{
		if (_hotKeysContext is not null)
			await _hotKeysContext.DisposeAsync();

		GC.SuppressFinalize(this);
	}
	#endregion
}
