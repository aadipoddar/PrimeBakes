using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Operations;
using PrimeBakesLibrary.Data.Store.Order;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Exporting.Store.Order;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Operations;
using PrimeBakesLibrary.Models.Store.Order;

using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Popups;

namespace PrimeBakes.Shared.Pages.Store.Order;

public partial class OrderMobileCartPage
{
    private UserModel _user;

    private bool _isLoading = true;
    private bool _isProcessing = false;
    private bool _showConfirmDialog = false;
    private bool _showValidationDialog = false;

    private string _orderRemarks = string.Empty;

    private List<OrderItemCartModel> _cart = [];
    private readonly List<(string Field, string Message)> _validationErrors = [];

    private SfDialog _sfOrderConfirmationDialog;
    private SfGrid<OrderItemCartModel> _sfCartGrid;

    #region Load Data
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
            return;

        _user = await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, [UserRoles.Store]);
        await LoadData();
        _isLoading = false;
        StateHasChanged();
    }

    private async Task LoadData()
    {
        _cart.Clear();

        if (await DataStorageService.LocalExists(StorageFileNames.OrderMobileCartDataFileName))
        {
            var items = System.Text.Json.JsonSerializer.Deserialize<List<OrderItemCartModel>>(await DataStorageService.LocalGetAsync(StorageFileNames.OrderMobileCartDataFileName)) ?? [];
            foreach (var item in items)
                _cart.Add(item);
        }

        _cart = [.. _cart.OrderBy(x => x.ItemName)];

        if (_sfCartGrid is not null)
            await _sfCartGrid.Refresh();

        StateHasChanged();
    }
    #endregion

    #region Products
    private async Task UpdateQuantity(OrderItemCartModel item, decimal newQuantity)
    {
        if (item is null || _isProcessing)
            return;

        item.Quantity = Math.Max(0, newQuantity);

        if (item.Quantity == 0)
            _cart.Remove(item);

        await SaveOrderFile();
    }
    #endregion

    #region Saving
    private async Task SaveOrderFile()
    {
        if (_isProcessing || _isLoading)
            return;

        try
        {
            _isProcessing = true;

            if (!_cart.Any(x => x.Quantity > 0) && await DataStorageService.LocalExists(StorageFileNames.OrderMobileCartDataFileName))
                await DataStorageService.LocalRemove(StorageFileNames.OrderMobileCartDataFileName);
            else
                await DataStorageService.LocalSaveAsync(StorageFileNames.OrderMobileCartDataFileName, System.Text.Json.JsonSerializer.Serialize(_cart.Where(_ => _.Quantity > 0)));

            VibrationService.VibrateHapticClick();
        }
        catch (Exception ex)
        {
            ShowError("An Error Occurred While Saving Cart Data", ex.Message);
        }
        finally
        {
            if (_sfCartGrid is not null)
                await _sfCartGrid?.Refresh();

            _isProcessing = false;
            StateHasChanged();
        }
    }

    private bool ValidateForm()
    {
        _validationErrors.Clear();

        if (_cart.Count == 0)
        {
            ShowError("Cart", "The cart is empty. Please add items to the cart before saving the order.");
            return false;
        }

        if (_cart.Any(item => item.Quantity <= 0))
        {
            ShowError("Quantity", "All items in the cart must have a quantity greater than zero.");
            return false;
        }

        if (_user.LocationId <= 1)
        {
            ShowError("Location", "Please select a valid location for the order.");
            return false;
        }

        return true;
    }

    private async Task SaveOrder(DateTime animationStart, double animationTime)
    {
        try
        {
            await SaveOrderFile();

            if (!ValidateForm())
            {
                _isProcessing = false;
                StateHasChanged();
                return;
            }

            var order = new OrderModel
            {
                Id = 0,
                LocationId = _user.LocationId,
                CompanyId = int.Parse((await SettingsData.LoadSettingsByKey(SettingsKeys.PrimaryCompanyLinkingId)).Value),
                TransactionDateTime = await CommonData.LoadCurrentDateTime(),
                FinancialYearId = (await FinancialYearData.LoadFinancialYearByDateTime(await CommonData.LoadCurrentDateTime())).Id,
                CreatedAt = await CommonData.LoadCurrentDateTime(),
                CreatedBy = _user.Id,
                TotalItems = _cart.Count,
                TotalQuantity = _cart.Sum(x => x.Quantity),
                CreatedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform(),
                Remarks = string.IsNullOrWhiteSpace(_orderRemarks.Trim()) ? null : _orderRemarks,
                SaleId = null,
                Status = true,
            };

            order.Id = await OrderData.SaveTransaction(order, _cart);

            await DataStorageService.LocalRemove(StorageFileNames.OrderMobileCartDataFileName);
            await SendLocalNotification(order.Id);

            var (pdfStream, fileName) = await OrderInvoiceExport.ExportInvoice(order.Id, InvoiceExportType.PDF);
            await SaveAndViewService.SaveAndView(fileName, pdfStream);

            // Wait for animation to complete before navigating
            var elapsed = (DateTime.Now - animationStart).TotalSeconds;
            var remaining = (animationTime * 1000) - (elapsed * 1000);
            if (remaining > 0)
                await Task.Delay((int)remaining);

            NavigationManager.NavigateTo(PageRouteNames.OrderMobileConfirmation, true);
        }
        catch (Exception ex)
        {
            ShowError("An Error Occurred While Saving Order", ex.Message);
            _isProcessing = false;
        }
    }
    #endregion

    #region Utilities
    private async Task OnPlaceOrderClick()
    {
        if (_isProcessing || _isLoading)
            return;

        _isProcessing = true;
        StateHasChanged();

        // await SaveOrder(DateTime.Now, 6.5);
    }

    private async Task SendLocalNotification(int orderId)
    {
        var order = await CommonData.LoadTableDataById<OrderOverviewModel>(ViewNames.OrderOverview, orderId);
        await NotificationService.ShowLocalNotification(
            order.Id,
            "Order Placed",
            $"{order.TransactionNo}",
            $"Your order #{order.TransactionNo} has been successfully placed | Total Items: {order.TotalItems} | Total Qty: {order.TotalQuantity} | Date: {order.TransactionDateTime:dd/MM/yy hh:mm tt} | Remarks: {order.Remarks}");
    }

    private void ShowError(string title, string message)
    {
        _validationErrors.Add((title, message));
        _showValidationDialog = true;
        StateHasChanged();
    }
    #endregion
}