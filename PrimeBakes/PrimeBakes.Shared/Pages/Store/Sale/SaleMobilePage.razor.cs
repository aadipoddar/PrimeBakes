using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Store.Product;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Models.Operations;
using PrimeBakesLibrary.Models.Store.Product;
using PrimeBakesLibrary.Models.Store.Sale;

using Syncfusion.Blazor.Grids;

namespace PrimeBakes.Shared.Pages.Store.Sale;

public partial class SaleMobilePage
{
    private UserModel _user;

    private bool _isLoading = true;
    private bool _isProcessing = false;

    private ProductCategoryModel _selectedCategory;

    private List<ProductCategoryModel> _productCategories = [];
    private List<SaleItemCartModel> _cart = [];

    private SfGrid<SaleItemCartModel> _sfCartGrid;

    #region Load Data
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
            return;

        _user = await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Sales);
        await LoadData();
        _isLoading = false;
        await SaveTransactionFile();
        StateHasChanged();
    }

    private async Task LoadData()
    {
        await LoadItems();
        await LoadExistingCart();

        if (_sfCartGrid is not null)
            await _sfCartGrid?.Refresh();
    }

    private async Task LoadItems()
    {
        try
        {
            _productCategories = await CommonData.LoadTableDataByStatus<ProductCategoryModel>(TableNames.ProductCategory);
            _productCategories.Add(new()
            {
                Id = 0,
                Name = "All Categories"
            });
            _productCategories = [.. _productCategories.OrderBy(s => s.Name)];
            _selectedCategory = _productCategories.FirstOrDefault(s => s.Id == 0);

            var allProducts = await ProductLocationData.LoadProductLocationOverviewByProductLocation(LocationId: _user.LocationId);

            foreach (var product in allProducts)
                _cart.Add(new()
                {
                    ItemCategoryId = product.ProductCategoryId,
                    ItemId = product.ProductId,
                    ItemName = product.Name,
                    Quantity = 0,
                    Rate = product.Rate,
                    Remarks = null,
                });
            _cart = [.. _cart.OrderBy(s => s.ItemName)];
        }
        catch (Exception)
        {
            NavigationManager.NavigateTo(PageRouteNames.Dashboard);
        }
    }

    private async Task LoadExistingCart()
    {
        try
        {
            if (await DataStorageService.LocalExists(StorageFileNames.SaleMobileCartDataFileName))
            {
                var existingCart = System.Text.Json.JsonSerializer.Deserialize<List<SaleItemCartModel>>(await DataStorageService.LocalGetAsync(StorageFileNames.SaleMobileCartDataFileName));
                foreach (var item in existingCart)
                    _cart.FirstOrDefault(p => p.ItemId == item.ItemId).Quantity = item.Quantity;
            }
        }
        catch (Exception)
        {
            NavigationManager.NavigateTo(PageRouteNames.SaleMobile, true);
            await DataStorageService.LocalRemove(StorageFileNames.SaleMobileCartDataFileName);
        }
        finally
        {
            await SaveTransactionFile();
        }
    }
    #endregion

    #region Cart
    private async Task AddToCart(SaleItemCartModel item)
    {
        if (item is null)
            return;

        item.Quantity = 1;
        await SaveTransactionFile();
    }

    private async Task UpdateQuantity(SaleItemCartModel item, decimal newQuantity)
    {
        if (item is null)
            return;

        item.Quantity = Math.Max(0, newQuantity);
        await SaveTransactionFile();
    }

    private async Task UpdateRate(SaleItemCartModel item, decimal newRate)
    {
        if (item is null)
            return;
        item.Rate = Math.Max(0, newRate);
        await SaveTransactionFile();
    }
    #endregion

    #region Saving
    private async Task UpdateFinancialDetails()
    {
        var taxes = await CommonData.LoadTableData<TaxModel>(TableNames.Tax);
        var items = await CommonData.LoadTableData<ProductModel>(TableNames.Product);

        foreach (var item in _cart.Where(_ => _.Quantity > 0))
        {
            item.DiscountPercent = 0;
            item.DiscountAmount = 0;

            item.BaseTotal = item.Rate * item.Quantity;
            item.AfterDiscount = item.BaseTotal - item.DiscountAmount;

            var selectedItem = items.FirstOrDefault(s => s.Id == item.ItemId);
            var tax = taxes.FirstOrDefault(s => s.Id == selectedItem.TaxId);

            item.CGSTPercent = tax?.CGST ?? 0;
            item.SGSTPercent = tax?.SGST ?? 0;
            item.IGSTPercent = 0;
            item.InclusiveTax = tax?.Inclusive ?? false;

            if (item.InclusiveTax)
            {
                item.CGSTAmount = item.AfterDiscount * (item.CGSTPercent / (100 + item.CGSTPercent));
                item.SGSTAmount = item.AfterDiscount * (item.SGSTPercent / (100 + item.SGSTPercent));
                item.IGSTAmount = item.AfterDiscount * (item.IGSTPercent / (100 + item.IGSTPercent));
                item.TotalTaxAmount = item.CGSTAmount + item.SGSTAmount + item.IGSTAmount;
                item.Total = item.AfterDiscount;
            }
            else
            {
                item.CGSTAmount = item.AfterDiscount * (item.CGSTPercent / 100);
                item.SGSTAmount = item.AfterDiscount * (item.SGSTPercent / 100);
                item.IGSTAmount = item.AfterDiscount * (item.IGSTPercent / 100);
                item.TotalTaxAmount = item.CGSTAmount + item.SGSTAmount + item.IGSTAmount;
                item.Total = item.AfterDiscount + item.TotalTaxAmount;
            }

            item.NetRate = item.Total / item.Quantity;
            item.Remarks = null;
        }
    }

    private async Task SaveTransactionFile()
    {
        if (_isProcessing || _isLoading)
            return;

        try
        {
            _isProcessing = true;

            await UpdateFinancialDetails();

            if (!_cart.Any(x => x.Quantity > 0) && await DataStorageService.LocalExists(StorageFileNames.SaleMobileCartDataFileName))
                await DataStorageService.LocalRemove(StorageFileNames.SaleMobileCartDataFileName);
            else
                await DataStorageService.LocalSaveAsync(StorageFileNames.SaleMobileCartDataFileName, System.Text.Json.JsonSerializer.Serialize(_cart.Where(_ => _.Quantity > 0)));

            VibrationService.VibrateHapticClick();
        }
        catch (Exception)
        {
            NavigationManager.NavigateTo(PageRouteNames.SaleMobile, true);
        }
        finally
        {
            if (_sfCartGrid is not null)
                await _sfCartGrid?.Refresh();

            _isProcessing = false;
            StateHasChanged();
        }
    }

    private async Task GoToCart()
    {
        await SaveTransactionFile();

        if (_cart.Sum(x => x.Quantity) <= 0 || !await DataStorageService.LocalExists(StorageFileNames.SaleMobileCartDataFileName))
            return;

        VibrationService.VibrateWithTime(500);
        _cart.Clear();

        NavigationManager.NavigateTo(PageRouteNames.SaleMobileCart);
    }
    #endregion
}