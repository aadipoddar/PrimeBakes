using PrimeBakes.Shared.Components.Dialog;

using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Sales.Product;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Exporting.Sales.Product;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Sales.Product;

using Syncfusion.Blazor.DropDowns;
using Syncfusion.Blazor.Grids;

namespace PrimeBakes.Shared.Pages.Admin.Products;

public partial class ProductPage : IAsyncDisposable
{
    private HotKeysContext _hotKeysContext;
    private bool _isLoading = true;
    private bool _isProcessing = false;
    private bool _showDeleted = false;

    private ProductModel _product = new();

    private List<ProductModel> _products = [];
    private List<ProductCategoryModel> _categories = [];
    private List<TaxModel> _taxes = [];

    private string _selectedCategoryName = string.Empty;
    private string _selectedTaxCode = string.Empty;

    private SfGrid<ProductModel> _sfGrid;
    private DeleteConfirmationDialog _deleteConfirmationDialog;
    private RecoverConfirmationDialog _recoverConfirmationDialog;

    private int _deleteProductId = 0;
    private string _deleteProductName = string.Empty;

    private int _recoverProductId = 0;
    private string _recoverProductName = string.Empty;

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
            .Add(ModCode.Ctrl, Code.S, SaveProduct, "Save", Exclude.None)
            .Add(ModCode.Ctrl, Code.E, ExportExcel, "Export Excel", Exclude.None)
            .Add(ModCode.Ctrl, Code.P, ExportPdf, "Export PDF", Exclude.None)
            .Add(ModCode.Ctrl, Code.N, ResetPage, "Reset the page", Exclude.None)
            .Add(ModCode.Ctrl, Code.L, Logout, "Logout", Exclude.None)
            .Add(ModCode.Ctrl, Code.B, NavigateBack, "Back", Exclude.None)
            .Add(ModCode.Ctrl, Code.D, NavigateToDashboard, "Dashboard", Exclude.None)
            .Add(Code.Insert, EditSelectedItem, "Edit selected", Exclude.None)
            .Add(Code.Delete, DeleteSelectedItem, "Delete selected", Exclude.None);

        _products = await CommonData.LoadTableData<ProductModel>(TableNames.Product);
        _categories = await CommonData.LoadTableData<ProductCategoryModel>(TableNames.ProductCategory);
        _taxes = await CommonData.LoadTableData<TaxModel>(TableNames.Tax);

        if (!_showDeleted)
            _products = [.. _products.Where(p => p.Status)];

        if (_sfGrid is not null)
            await _sfGrid.Refresh();
    }
    #endregion

    #region Changed Events
    private void OnCategoryChange(ChangeEventArgs<string, ProductCategoryModel> args)
    {
        if (args.ItemData != null)
        {
            _product.ProductCategoryId = args.ItemData.Id;
            _selectedCategoryName = args.ItemData.Name;
        }
        else
        {
            _product.ProductCategoryId = 0;
            _selectedCategoryName = string.Empty;
        }
    }

    private void OnTaxChange(ChangeEventArgs<string, TaxModel> args)
    {
        if (args.ItemData != null)
        {
            _product.TaxId = args.ItemData.Id;
            _selectedTaxCode = args.ItemData.Code;
        }
        else
        {
            _product.TaxId = 0;
            _selectedTaxCode = string.Empty;
        }
    }
    #endregion

    #region Actions
    private void OnEditProduct(ProductModel product)
    {
        _product = new()
        {
            Id = product.Id,
            Name = product.Name,
            Code = product.Code,
            ProductCategoryId = product.ProductCategoryId,
            Rate = product.Rate,
            TaxId = product.TaxId,
            Remarks = product.Remarks,
            Status = product.Status
        };

        // Set autocomplete values
        var category = _categories.FirstOrDefault(c => c.Id == product.ProductCategoryId);
        _selectedCategoryName = category?.Name ?? string.Empty;

        var tax = _taxes.FirstOrDefault(t => t.Id == product.TaxId);
        _selectedTaxCode = tax?.Code ?? string.Empty;

        StateHasChanged();
    }

    private async Task ShowDeleteConfirmation(int id, string name)
    {
        _deleteProductId = id;
        _deleteProductName = name;
        await _deleteConfirmationDialog.ShowAsync();
    }

    private async Task CancelDelete()
    {
        _deleteProductId = 0;
        _deleteProductName = string.Empty;
        await _deleteConfirmationDialog.HideAsync();
    }

    private async Task ConfirmDelete()
    {
        try
        {
            _isProcessing = true;
            await _deleteConfirmationDialog.HideAsync();

            var product = _products.FirstOrDefault(p => p.Id == _deleteProductId);
            if (product == null)
            {
                await _toastNotification.ShowAsync("Error", "Product not found.", ToastType.Error);
                return;
            }

            product.Status = false;
            await ProductData.InsertProduct(product);

            await _toastNotification.ShowAsync("Deleted", $"Product '{product.Name}' removed successfully.", ToastType.Success);
            NavigationManager.NavigateTo(PageRouteNames.AdminProduct, true);
        }
        catch (Exception ex)
        {
            await _toastNotification.ShowAsync("Error", $"Failed to delete product: {ex.Message}", ToastType.Error);
        }
        finally
        {
            _isProcessing = false;
            _deleteProductId = 0;
            _deleteProductName = string.Empty;
        }
    }

    private async Task ShowRecoverConfirmation(int id, string name)
    {
        _recoverProductId = id;
        _recoverProductName = name;
        await _recoverConfirmationDialog.ShowAsync();
    }

    private async Task CancelRecover()
    {
        _recoverProductId = 0;
        _recoverProductName = string.Empty;
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

            var product = _products.FirstOrDefault(p => p.Id == _recoverProductId);
            if (product == null)
            {
                await _toastNotification.ShowAsync("Error", "Product not found.", ToastType.Error);
                return;
            }

            product.Status = true;
            await ProductData.InsertProduct(product);

            await _toastNotification.ShowAsync("Recovered", $"Product '{product.Name}' restored successfully.", ToastType.Success);
            NavigationManager.NavigateTo(PageRouteNames.AdminProduct, true);
        }
        catch (Exception ex)
        {
            await _toastNotification.ShowAsync("Error", $"Failed to recover product: {ex.Message}", ToastType.Error);
        }
        finally
        {
            _isProcessing = false;
            _recoverProductId = 0;
            _recoverProductName = string.Empty;
        }
    }
    #endregion

    #region Saving
    private async Task<bool> ValidateForm()
    {
        _product.Name = _product.Name?.Trim() ?? "";
        _product.Code = _product.Code?.Trim() ?? "";
        _product.Remarks = _product.Remarks?.Trim() ?? "";

        _product.Code = _product.Code?.ToUpper() ?? "";

        _product.Status = true;

        if (string.IsNullOrWhiteSpace(_product.Name))
        {
            await _toastNotification.ShowAsync("Validation", "Product name is required.", ToastType.Warning);
            return false;
        }

        // Code is auto-generated, no need to validate

        if (_product.ProductCategoryId <= 0)
        {
            await _toastNotification.ShowAsync("Validation", "Please select a category.", ToastType.Warning);
            return false;
        }

        if (_product.Rate < 0)
        {
            await _toastNotification.ShowAsync("Validation", "Rate must be 0 or greater.", ToastType.Warning);
            return false;
        }

        if (_product.TaxId <= 0)
        {
            await _toastNotification.ShowAsync("Validation", "Please select a tax.", ToastType.Warning);
            return false;
        }

        if (string.IsNullOrWhiteSpace(_product.Remarks))
            _product.Remarks = null;

        if (_product.Id > 0)
        {
            var existingByName = _products.FirstOrDefault(p => p.Id != _product.Id && p.Name.Equals(_product.Name, StringComparison.OrdinalIgnoreCase));
            if (existingByName is not null)
            {
                await _toastNotification.ShowAsync("Validation", $"Product name '{_product.Name}' already exists.", ToastType.Warning);
                return false;
            }

            // Code is preserved when editing, no need to check for duplicates
        }
        else
        {
            var existingByName = _products.FirstOrDefault(p => p.Name.Equals(_product.Name, StringComparison.OrdinalIgnoreCase));
            if (existingByName is not null)
            {
                await _toastNotification.ShowAsync("Validation", $"Product name '{_product.Name}' already exists.", ToastType.Warning);
                return false;
            }

            // Code is auto-generated and unique, no need to check for duplicates
        }

        return true;
    }

    private async Task SaveProduct()
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

            await _toastNotification.ShowAsync("Saving", "Processing product...", ToastType.Info);

            if (_product.Id == 0)
                _product.Code = await GenerateCodes.GenerateProductCode();

            var isNewProduct = _product.Id == 0;

            _product.Id = await ProductData.InsertProduct(_product);
            if (isNewProduct)
                await InsertProductLocations();

            await _toastNotification.ShowAsync("Saved", $"Product '{_product.Name}' saved successfully.", ToastType.Success);
            NavigationManager.NavigateTo(PageRouteNames.AdminProduct, true);
        }
        catch (Exception ex)
        {
            await _toastNotification.ShowAsync("Error", $"Failed to save product: {ex.Message}", ToastType.Error);
        }
        finally
        {
            _isProcessing = false;
        }
    }

    private async Task InsertProductLocations()
    {
        var locations = await CommonData.LoadTableDataByStatus<LocationModel>(TableNames.Location);
        foreach (var location in locations)
            await ProductData.InsertProductLocation(new()
            {
                Id = 0,
                Rate = _product.Rate,
                ProductId = _product.Id,
                LocationId = location.Id,
                Status = true,
            });
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

            // Enrich data with category and tax names
            var enrichedData = _products.Select(p => new
            {
                p.Id,
                p.Name,
                p.Code,
                Category = _categories.FirstOrDefault(c => c.Id == p.ProductCategoryId)?.Name ?? "N/A",
                p.Rate,
                Tax = _taxes.FirstOrDefault(t => t.Id == p.TaxId)?.Code ?? "N/A",
                p.Remarks,
                p.Status
            }).ToList();

            var (stream, fileName) = await ProductExport.ExportMaster(enrichedData, ReportExportType.Excel);
            await SaveAndViewService.SaveAndView(fileName, stream);

            await _toastNotification.ShowAsync("Success", "Product data exported to Excel successfully.", ToastType.Success);
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

            // Enrich data with category and tax names
            var enrichedData = _products.Select(p => new
            {
                p.Id,
                p.Name,
                p.Code,
                Category = _categories.FirstOrDefault(c => c.Id == p.ProductCategoryId)?.Name ?? "N/A",
                p.Rate,
                Tax = _taxes.FirstOrDefault(t => t.Id == p.TaxId)?.Code ?? "N/A",
                p.Remarks,
                p.Status
            }).ToList();

            var (stream, fileName) = await ProductExport.ExportMaster(enrichedData, ReportExportType.PDF);
            await SaveAndViewService.SaveAndView(fileName, stream);

            await _toastNotification.ShowAsync("Success", "Product data exported to PDF successfully.", ToastType.Success);
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
            OnEditProduct(selectedRecords[0]);
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
        NavigationManager.NavigateTo(PageRouteNames.AdminProduct, true);

    private void NavigateBack() =>
        NavigationManager.NavigateTo(PageRouteNames.SalesDashboard);

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