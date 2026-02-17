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

public partial class DiningTablePage : IAsyncDisposable
{
	private HotKeysContext _hotKeysContext;
	private bool _isLoading = true;
	private bool _isProcessing = false;
	private bool _showDeleted = false;

	private DiningTableModel _diningTable = new();

	private List<DiningTableModel> _diningTables = [];
	private List<DiningAreaModel> _diningAreas = [];

	private string _selectedDiningAreaName = string.Empty;

	private SfGrid<DiningTableModel> _sfGrid;
	private DeleteConfirmationDialog _deleteConfirmationDialog;
	private RecoverConfirmationDialog _recoverConfirmationDialog;

	private int _deleteDiningTableId = 0;
	private string _deleteDiningTableName = string.Empty;

	private int _recoverDiningTableId = 0;
	private string _recoverDiningTableName = string.Empty;

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
			.Add(ModCode.Ctrl, Code.S, SaveDiningTable, "Save", Exclude.None)
			.Add(ModCode.Ctrl, Code.E, ExportExcel, "Export Excel", Exclude.None)
			.Add(ModCode.Ctrl, Code.P, ExportPdf, "Export PDF", Exclude.None)
			.Add(ModCode.Ctrl, Code.N, ResetPage, "Reset the page", Exclude.None)
			.Add(ModCode.Ctrl, Code.L, Logout, "Logout", Exclude.None)
			.Add(ModCode.Ctrl, Code.B, NavigateBack, "Back", Exclude.None)
			.Add(ModCode.Ctrl, Code.D, NavigateToDashboard, "Dashboard", Exclude.None)
			.Add(Code.Insert, EditSelectedItem, "Edit selected", Exclude.None)
			.Add(Code.Delete, DeleteSelectedItem, "Delete selected", Exclude.None);

		_diningAreas = await CommonData.LoadTableDataByStatus<DiningAreaModel>(TableNames.DiningArea);
		_diningTables = await CommonData.LoadTableData<DiningTableModel>(TableNames.DiningTable);

		if (!_showDeleted)
			_diningTables = [.. _diningTables.Where(dt => dt.Status)];

		if (_sfGrid is not null)
			await _sfGrid.Refresh();
	}
	#endregion

	#region Change Events
	private void OnDiningAreaChange(Syncfusion.Blazor.DropDowns.ChangeEventArgs<string, DiningAreaModel> args)
	{
		if (args.ItemData is not null)
		{
			_diningTable.DiningAreaId = args.ItemData.Id;
			_selectedDiningAreaName = args.ItemData.Name;
		}
		else
		{
			_diningTable.DiningAreaId = 0;
			_selectedDiningAreaName = string.Empty;
		}
	}
	#endregion

	#region Actions
	private void OnEditDiningTable(DiningTableModel diningTable)
	{
		_diningTable = new()
		{
			Id = diningTable.Id,
			Name = diningTable.Name,
			DiningAreaId = diningTable.DiningAreaId,
			Remarks = diningTable.Remarks,
			Status = diningTable.Status
		};

		var diningArea = _diningAreas.FirstOrDefault(da => da.Id == diningTable.DiningAreaId);
		_selectedDiningAreaName = diningArea?.Name ?? string.Empty;

		StateHasChanged();
	}

	private async Task ShowDeleteConfirmation(int id, string name)
	{
		_deleteDiningTableId = id;
		_deleteDiningTableName = name;
		await _deleteConfirmationDialog.ShowAsync();
	}

	private async Task CancelDelete()
	{
		_deleteDiningTableId = 0;
		_deleteDiningTableName = string.Empty;
		await _deleteConfirmationDialog.HideAsync();
	}

	private async Task ConfirmDelete()
	{
		try
		{
			_isProcessing = true;
			await _deleteConfirmationDialog.HideAsync();

			var diningTable = _diningTables.FirstOrDefault(dt => dt.Id == _deleteDiningTableId);
			if (diningTable is null)
			{
				await _toastNotification.ShowAsync("Error", "Dining table not found.", ToastType.Error);
				return;
			}

			diningTable.Status = false;
			await DiningTableData.InsertDiningTable(diningTable);

			await _toastNotification.ShowAsync("Deleted", $"Dining table '{diningTable.Name}' has been deleted successfully.", ToastType.Success);
			NavigationManager.NavigateTo(PageRouteNames.DiningTable, true);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"Failed to delete dining table: {ex.Message}", ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
			_deleteDiningTableId = 0;
			_deleteDiningTableName = string.Empty;
		}
	}

	private async Task ShowRecoverConfirmation(int id, string name)
	{
		_recoverDiningTableId = id;
		_recoverDiningTableName = name;
		await _recoverConfirmationDialog.ShowAsync();
	}

	private async Task CancelRecover()
	{
		_recoverDiningTableId = 0;
		_recoverDiningTableName = string.Empty;
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

			var diningTable = _diningTables.FirstOrDefault(dt => dt.Id == _recoverDiningTableId);
			if (diningTable is null)
			{
				await _toastNotification.ShowAsync("Error", "Dining table not found.", ToastType.Error);
				return;
			}

			diningTable.Status = true;
			await DiningTableData.InsertDiningTable(diningTable);

			await _toastNotification.ShowAsync("Recovered", $"Dining table '{diningTable.Name}' has been recovered successfully.", ToastType.Success);
			NavigationManager.NavigateTo(PageRouteNames.DiningTable, true);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"Failed to recover dining table: {ex.Message}", ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
			_recoverDiningTableId = 0;
			_recoverDiningTableName = string.Empty;
		}
	}
	#endregion

	#region Saving
	private async Task<bool> ValidateForm()
	{
		_diningTable.Name = _diningTable.Name?.Trim() ?? "";
		_diningTable.Name = _diningTable.Name?.ToUpper() ?? "";
		_diningTable.Remarks = _diningTable.Remarks?.Trim() ?? "";
		_diningTable.Status = true;

		if (string.IsNullOrWhiteSpace(_diningTable.Name))
		{
			await _toastNotification.ShowAsync("Validation", "Dining table name is required.", ToastType.Warning);
			return false;
		}

		if (_diningTable.DiningAreaId <= 0)
		{
			await _toastNotification.ShowAsync("Validation", "Please select a dining area.", ToastType.Warning);
			return false;
		}

		if (string.IsNullOrWhiteSpace(_diningTable.Remarks))
			_diningTable.Remarks = null;

		var allDiningTables = await CommonData.LoadTableData<DiningTableModel>(TableNames.DiningTable);

		if (_diningTable.Id > 0)
		{
			var existing = allDiningTables.FirstOrDefault(_ => _.Id != _diningTable.Id && _.Name.Equals(_diningTable.Name, StringComparison.OrdinalIgnoreCase));
			if (existing is not null)
			{
				await _toastNotification.ShowAsync("Duplicate", $"Dining table '{_diningTable.Name}' already exists.", ToastType.Warning);
				return false;
			}
		}
		else
		{
			var existing = allDiningTables.FirstOrDefault(_ => _.Name.Equals(_diningTable.Name, StringComparison.OrdinalIgnoreCase));
			if (existing is not null)
			{
				await _toastNotification.ShowAsync("Duplicate", $"Dining table '{_diningTable.Name}' already exists.", ToastType.Warning);
				return false;
			}
		}

		return true;
	}

	private async Task SaveDiningTable()
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

			await _toastNotification.ShowAsync("Processing", "Saving dining table...", ToastType.Info);

			await DiningTableData.InsertDiningTable(_diningTable);

			await _toastNotification.ShowAsync("Saved", $"Dining table '{_diningTable.Name}' saved successfully.", ToastType.Success);
			NavigationManager.NavigateTo(PageRouteNames.DiningTable, true);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"Failed to save dining table: {ex.Message}", ToastType.Error);
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

			var enrichedModels = _diningTables.ToList();

			var (stream, fileName) = await DiningTableExport.ExportMaster(enrichedModels, ReportExportType.Excel);
			await SaveAndViewService.SaveAndView(fileName, stream);

			await _toastNotification.ShowAsync("Success", "Dining table data exported to Excel successfully.", ToastType.Success);
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

			var enrichedModels = _diningTables.ToList();

			var (stream, fileName) = await DiningTableExport.ExportMaster(enrichedModels, ReportExportType.PDF);
			await SaveAndViewService.SaveAndView(fileName, stream);

			await _toastNotification.ShowAsync("Success", "Dining table data exported to PDF successfully.", ToastType.Success);
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
			OnEditDiningTable(selectedRecords[0]);
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
		NavigationManager.NavigateTo(PageRouteNames.DiningTable, true);

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
