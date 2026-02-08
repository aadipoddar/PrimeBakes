using PrimeBakes.Shared.Components.Dialog;

using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Store.Product;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Exporting.Store.Product;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Operations;
using PrimeBakesLibrary.Models.Store.Product;

using Syncfusion.Blazor.Grids;

namespace PrimeBakes.Shared.Pages.Store.Product;

public partial class ProductCategoryPage : IAsyncDisposable
{
    private HotKeysContext _hotKeysContext;
    private bool _isLoading = true;
    private bool _isProcessing = false;
    private bool _showDeleted = false;

    private ProductCategoryModel _productCategory = new();

    private List<ProductCategoryModel> _productCategories = [];

    private SfGrid<ProductCategoryModel> _sfGrid;
    private DeleteConfirmationDialog _deleteConfirmationDialog;
    private RecoverConfirmationDialog _recoverConfirmationDialog;

    private int _deleteProductCategoryId = 0;
    private string _deleteProductCategoryName = string.Empty;

    private int _recoverProductCategoryId = 0;
    private string _recoverProductCategoryName = string.Empty;

    private ToastNotification _toastNotification;

    #region Load Data
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
            return;

        await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Admin, true);
        await LoadData();
        _isLoading = false;
        StateHasChanged();
    }

    private async Task LoadData()
    {
        _hotKeysContext = HotKeys.CreateContext()
            .Add(ModCode.Ctrl, Code.S, SaveProductCategory, "Save", Exclude.None)
            .Add(ModCode.Ctrl, Code.E, ExportExcel, "Export Excel", Exclude.None)
            .Add(ModCode.Ctrl, Code.P, ExportPdf, "Export PDF", Exclude.None)
            .Add(ModCode.Ctrl, Code.N, ResetPage, "Reset the page", Exclude.None)
            .Add(ModCode.Ctrl, Code.L, Logout, "Logout", Exclude.None)
            .Add(ModCode.Ctrl, Code.B, NavigateBack, "Back", Exclude.None)
            .Add(ModCode.Ctrl, Code.D, NavigateToDashboard, "Dashboard", Exclude.None)
            .Add(Code.Insert, EditSelectedItem, "Edit selected", Exclude.None)
            .Add(Code.Delete, DeleteSelectedItem, "Delete selected", Exclude.None);

        _productCategories = await CommonData.LoadTableData<ProductCategoryModel>(TableNames.ProductCategory);

        if (!_showDeleted)
            _productCategories = [.. _productCategories.Where(pc => pc.Status)];

        if (_sfGrid is not null)
            await _sfGrid.Refresh();
    }
    #endregion

    #region Actions
    private void OnEditProductCategory(ProductCategoryModel productCategory)
    {
        _productCategory = new()
        {
            Id = productCategory.Id,
            Name = productCategory.Name,
            Remarks = productCategory.Remarks,
            Status = productCategory.Status
        };

        StateHasChanged();
    }

    private async Task ShowDeleteConfirmation(int id, string name)
    {
        _deleteProductCategoryId = id;
        _deleteProductCategoryName = name;
        await _deleteConfirmationDialog.ShowAsync();
    }

    private async Task CancelDelete()
    {
        _deleteProductCategoryId = 0;
        _deleteProductCategoryName = string.Empty;
        await _deleteConfirmationDialog.HideAsync();
    }

    private async Task ConfirmDelete()
    {
        try
        {
            _isProcessing = true;
            await _deleteConfirmationDialog.HideAsync();

            var productCategory = _productCategories.FirstOrDefault(pc => pc.Id == _deleteProductCategoryId);
            if (productCategory == null)
            {
                await _toastNotification.ShowAsync("Error", "Category not found.", ToastType.Error);
                return;
            }

            productCategory.Status = false;
            await ProductData.InsertProductCategory(productCategory);

            await _toastNotification.ShowAsync("Deleted", $"Category '{productCategory.Name}' removed successfully.", ToastType.Success);
            NavigationManager.NavigateTo(PageRouteNames.ProductCategory, true);
        }
        catch (Exception ex)
        {
            await _toastNotification.ShowAsync("Error", $"Failed to delete category: {ex.Message}", ToastType.Error);
        }
        finally
        {
            _isProcessing = false;
            _deleteProductCategoryId = 0;
            _deleteProductCategoryName = string.Empty;
        }
    }

    private async Task ShowRecoverConfirmation(int id, string name)
    {
        _recoverProductCategoryId = id;
        _recoverProductCategoryName = name;
        await _recoverConfirmationDialog.ShowAsync();
    }

    private async Task CancelRecover()
    {
        _recoverProductCategoryId = 0;
        _recoverProductCategoryName = string.Empty;
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

            var productCategory = _productCategories.FirstOrDefault(pc => pc.Id == _recoverProductCategoryId);
            if (productCategory == null)
            {
                await _toastNotification.ShowAsync("Error", "Category not found.", ToastType.Error);
                return;
            }

            productCategory.Status = true;
            await ProductData.InsertProductCategory(productCategory);

            await _toastNotification.ShowAsync("Recovered", $"Category '{productCategory.Name}' restored successfully.", ToastType.Success);
            NavigationManager.NavigateTo(PageRouteNames.ProductCategory, true);
        }
        catch (Exception ex)
        {
            await _toastNotification.ShowAsync("Error", $"Failed to recover category: {ex.Message}", ToastType.Error);
        }
        finally
        {
            _isProcessing = false;
            _recoverProductCategoryId = 0;
            _recoverProductCategoryName = string.Empty;
        }
    }
    #endregion

    #region Saving
    private async Task<bool> ValidateForm()
    {
        _productCategory.Name = _productCategory.Name?.Trim() ?? "";
        _productCategory.Name = _productCategory.Name?.ToUpper() ?? "";

        _productCategory.Remarks = _productCategory.Remarks?.Trim() ?? "";
        _productCategory.Status = true;

        if (string.IsNullOrWhiteSpace(_productCategory.Name))
        {
            await _toastNotification.ShowAsync("Validation", "Category name is required.", ToastType.Warning);
            return false;
        }

        if (string.IsNullOrWhiteSpace(_productCategory.Remarks))
            _productCategory.Remarks = null;

        if (_productCategory.Id > 0)
        {
            var existingProductCategory = _productCategories.FirstOrDefault(_ => _.Id != _productCategory.Id && _.Name.Equals(_productCategory.Name, StringComparison.OrdinalIgnoreCase));
            if (existingProductCategory is not null)
            {
                await _toastNotification.ShowAsync("Validation", $"Category '{_productCategory.Name}' already exists.", ToastType.Warning);
                return false;
            }
        }
        else
        {
            var existingProductCategory = _productCategories.FirstOrDefault(_ => _.Name.Equals(_productCategory.Name, StringComparison.OrdinalIgnoreCase));
            if (existingProductCategory is not null)
            {
                await _toastNotification.ShowAsync("Validation", $"Category '{_productCategory.Name}' already exists.", ToastType.Warning);
                return false;
            }
        }

        return true;
    }

    private async Task SaveProductCategory()
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

            await ProductData.InsertProductCategory(_productCategory);

            await _toastNotification.ShowAsync("Saved", $"Category '{_productCategory.Name}' saved successfully.", ToastType.Success);
            NavigationManager.NavigateTo(PageRouteNames.ProductCategory, true);
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

            var (stream, fileName) = await ProductCategoryExport.ExportMaster(_productCategories, ReportExportType.Excel);
            await SaveAndViewService.SaveAndView(fileName, stream);

            await _toastNotification.ShowAsync("Success", "Product category data exported to Excel successfully.", ToastType.Success);
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

            var (stream, fileName) = await ProductCategoryExport.ExportMaster(_productCategories, ReportExportType.PDF);
            await SaveAndViewService.SaveAndView(fileName, stream);

            await _toastNotification.ShowAsync("Success", "Product category data exported to PDF successfully.", ToastType.Success);
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
            OnEditProductCategory(selectedRecords[0]);
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
        NavigationManager.NavigateTo(PageRouteNames.ProductCategory, true);

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