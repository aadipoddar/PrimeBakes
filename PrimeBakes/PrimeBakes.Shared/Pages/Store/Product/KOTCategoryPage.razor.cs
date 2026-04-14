using PrimeBakes.Shared.Components.Dialog;
using PrimeBakesLibrary.Data.Store.Product;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Exporting.Store.Product;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Operations;
using PrimeBakesLibrary.Models.Store.Product;

using Syncfusion.Blazor.Grids;

namespace PrimeBakes.Shared.Pages.Store.Product;

public partial class KOTCategoryPage : IAsyncDisposable
{
	private HotKeysContext _hotKeysContext;
	private bool _isLoading = true;
	private bool _isProcessing = false;
	private bool _showDeleted = false;

	private KOTCategoryModel _kotCategory = new();

	private List<KOTCategoryModel> _kotCategories = [];

	private SfGrid<KOTCategoryModel> _sfGrid;
	private DeleteConfirmationDialog _deleteConfirmationDialog;
	private RecoverConfirmationDialog _recoverConfirmationDialog;

	private int _deleteKOTCategoryId = 0;
	private string _deleteKOTCategoryName = string.Empty;

	private int _recoverKOTCategoryId = 0;
	private string _recoverKOTCategoryName = string.Empty;

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
			.Add(ModCode.Ctrl, Code.S, SaveKOTCategory, "Save", Exclude.None)
			.Add(ModCode.Ctrl, Code.E, ExportExcel, "Export Excel", Exclude.None)
			.Add(ModCode.Ctrl, Code.P, ExportPdf, "Export PDF", Exclude.None)
			.Add(ModCode.Ctrl, Code.N, ResetPage, "Reset the page", Exclude.None)
			.Add(ModCode.Ctrl, Code.L, Logout, "Logout", Exclude.None)
			.Add(ModCode.Ctrl, Code.B, NavigateBack, "Back", Exclude.None)
			.Add(ModCode.Ctrl, Code.D, NavigateToDashboard, "Dashboard", Exclude.None)
			.Add(Code.Insert, EditSelectedItem, "Edit selected", Exclude.None)
			.Add(Code.Delete, DeleteSelectedItem, "Delete selected", Exclude.None);

		_kotCategories = await CommonData.LoadTableData<KOTCategoryModel>(TableNames.KOTCategory);

		if (!_showDeleted)
			_kotCategories = [.. _kotCategories.Where(pc => pc.Status)];

		if (_sfGrid is not null)
			await _sfGrid.Refresh();
	}
	#endregion

	#region Actions
	private void OnEditKOTCategory(KOTCategoryModel kotCategory)
	{
		_kotCategory = new()
		{
			Id = kotCategory.Id,
			Name = kotCategory.Name,
			Remarks = kotCategory.Remarks,
			Status = kotCategory.Status
		};

		StateHasChanged();
	}

	private async Task ShowDeleteConfirmation(int id, string name)
	{
		_deleteKOTCategoryId = id;
		_deleteKOTCategoryName = name;
		await _deleteConfirmationDialog.ShowAsync();
	}

	private async Task CancelDelete()
	{
		_deleteKOTCategoryId = 0;
		_deleteKOTCategoryName = string.Empty;
		await _deleteConfirmationDialog.HideAsync();
	}

	private async Task ConfirmDelete()
	{
		try
		{
			_isProcessing = true;
			await _deleteConfirmationDialog.HideAsync();

			var kotCategory = _kotCategories.FirstOrDefault(pc => pc.Id == _deleteKOTCategoryId);
			if (kotCategory == null)
			{
				await _toastNotification.ShowAsync("Error", "Category not found.", ToastType.Error);
				return;
			}

			kotCategory.Status = false;
			await ProductData.InsertKOTCategory(kotCategory);

			await _toastNotification.ShowAsync("Deleted", $"Category '{kotCategory.Name}' removed successfully.", ToastType.Success);
			NavigationManager.NavigateTo(PageRouteNames.KOTCategory, true);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"Failed to delete category: {ex.Message}", ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
			_deleteKOTCategoryId = 0;
			_deleteKOTCategoryName = string.Empty;
		}
	}

	private async Task ShowRecoverConfirmation(int id, string name)
	{
		_recoverKOTCategoryId = id;
		_recoverKOTCategoryName = name;
		await _recoverConfirmationDialog.ShowAsync();
	}

	private async Task CancelRecover()
	{
		_recoverKOTCategoryId = 0;
		_recoverKOTCategoryName = string.Empty;
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

			var kotCategory = _kotCategories.FirstOrDefault(pc => pc.Id == _recoverKOTCategoryId);
			if (kotCategory == null)
			{
				await _toastNotification.ShowAsync("Error", "Category not found.", ToastType.Error);
				return;
			}

			kotCategory.Status = true;
			await ProductData.InsertKOTCategory(kotCategory);

			await _toastNotification.ShowAsync("Recovered", $"Category '{kotCategory.Name}' restored successfully.", ToastType.Success);
			NavigationManager.NavigateTo(PageRouteNames.KOTCategory, true);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"Failed to recover category: {ex.Message}", ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
			_recoverKOTCategoryId = 0;
			_recoverKOTCategoryName = string.Empty;
		}
	}
	#endregion

	#region Saving
	private async Task<bool> ValidateForm()
	{
		_kotCategory.Name = _kotCategory.Name?.Trim() ?? "";
		_kotCategory.Name = _kotCategory.Name?.ToUpper() ?? "";

		_kotCategory.Remarks = _kotCategory.Remarks?.Trim() ?? "";
		_kotCategory.Status = true;

		if (string.IsNullOrWhiteSpace(_kotCategory.Name))
		{
			await _toastNotification.ShowAsync("Validation", "Category name is required.", ToastType.Warning);
			return false;
		}

		if (string.IsNullOrWhiteSpace(_kotCategory.Remarks))
			_kotCategory.Remarks = null;

		if (_kotCategory.Id > 0)
		{
			var existingKOTCategory = _kotCategories.FirstOrDefault(_ => _.Id != _kotCategory.Id && _.Name.Equals(_kotCategory.Name, StringComparison.OrdinalIgnoreCase));
			if (existingKOTCategory is not null)
			{
				await _toastNotification.ShowAsync("Validation", $"Category '{_kotCategory.Name}' already exists.", ToastType.Warning);
				return false;
			}
		}
		else
		{
			var existingKOTCategory = _kotCategories.FirstOrDefault(_ => _.Name.Equals(_kotCategory.Name, StringComparison.OrdinalIgnoreCase));
			if (existingKOTCategory is not null)
			{
				await _toastNotification.ShowAsync("Validation", $"Category '{_kotCategory.Name}' already exists.", ToastType.Warning);
				return false;
			}
		}

		return true;
	}

	private async Task SaveKOTCategory()
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

			await _toastNotification.ShowAsync("Saving", "Processing category...", ToastType.Info);

			await ProductData.InsertKOTCategory(_kotCategory);

			await _toastNotification.ShowAsync("Saved", $"Category '{_kotCategory.Name}' saved successfully.", ToastType.Success);
			NavigationManager.NavigateTo(PageRouteNames.KOTCategory, true);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"Failed to save category: {ex.Message}", ToastType.Error);
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

			var (stream, fileName) = await KOTCategoryExport.ExportMaster(_kotCategories, ReportExportType.Excel);
			await SaveAndViewService.SaveAndView(fileName, stream);

			await _toastNotification.ShowAsync("Success", "KOT category data exported to Excel successfully.", ToastType.Success);
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

			var (stream, fileName) = await KOTCategoryExport.ExportMaster(_kotCategories, ReportExportType.PDF);
			await SaveAndViewService.SaveAndView(fileName, stream);

			await _toastNotification.ShowAsync("Success", "KOT category data exported to PDF successfully.", ToastType.Success);
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
			OnEditKOTCategory(selectedRecords[0]);
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
		NavigationManager.NavigateTo(PageRouteNames.KOTCategory, true);

	private void NavigateBack() =>
		NavigationManager.NavigateTo(PageRouteNames.StoreDashboard);

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