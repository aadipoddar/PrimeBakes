using Microsoft.JSInterop;

using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Operations;
using PrimeBakesLibrary.Data.Store.Masters;
using PrimeBakesLibrary.Data.Store.Sale;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Exporting.Store.Sale;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Operations;
using PrimeBakesLibrary.Models.Store.Masters;
using PrimeBakesLibrary.Models.Store.Product;
using PrimeBakesLibrary.Models.Store.Sale;

using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Inputs;
using Syncfusion.Blazor.Popups;

namespace PrimeBakes.Shared.Pages.Store.Sale;

public partial class SaleMobileCartPage
{
    private UserModel _user;

    private bool _isLoading = true;
    private bool _isProcessing = false;
    private bool _showConfirmDialog = false;
    private bool _showValidationDialog = false;
    private bool _showPaymentDialog = false;

    private CustomerModel _selectedCustomer = new();
    private readonly SaleModel _sale = new();

    private List<SaleItemCartModel> _cart = [];
    private readonly List<(string Field, string Message)> _validationErrors = [];
    private readonly List<PaymentItem> _payments = [];
    private readonly List<PaymentModeModel> _paymentMethods = PaymentModeData.GetPaymentModes();

    private PaymentModeModel _selectedPaymentMethod = new();
    private decimal _paymentAmount = 0;
    private decimal _remainingAmount => _sale.TotalAmount - _payments.Sum(p => p.Amount);

    private SfDialog _sfValidationErrorDialog;
    private SfDialog _sfOrderConfirmationDialog;
    private SfDialog _sfPaymentDialog;
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
        _cart.Clear();

        if (await DataStorageService.LocalExists(StorageFileNames.SaleMobileCartDataFileName))
        {
            var items = System.Text.Json.JsonSerializer.Deserialize<List<SaleItemCartModel>>(await DataStorageService.LocalGetAsync(StorageFileNames.SaleMobileCartDataFileName)) ?? [];
            foreach (var item in items)
                _cart.Add(item);
        }

        _cart = [.. _cart.OrderBy(x => x.ItemName)];

        if (_sfCartGrid is not null)
            await _sfCartGrid.Refresh();

        StateHasChanged();
    }
    #endregion

    #region Changed Events
    private async Task OnCustomerNumberChanged(string args)
    {
        if (string.IsNullOrWhiteSpace(args))
        {
            _selectedCustomer = new();
            _sale.CustomerId = null;
            await SaveTransactionFile();
            return;
        }

        args = args.Trim();
        if (args.Any(c => !char.IsDigit(c)))
            args = new string([.. args.Where(char.IsDigit)]);

        _selectedCustomer = await CustomerData.LoadCustomerByNumber(args);
        _selectedCustomer ??= new()
        {
            Id = 0,
            Name = "",
            Number = args
        };

        _sale.CustomerId = _selectedCustomer.Id;
        await SaveTransactionFile();
    }

    private async Task OnDiscountPercentChanged(ChangeEventArgs<decimal> args)
    {
        _sale.DiscountPercent = args.Value;
        await SaveTransactionFile();
    }

    private async Task OnRoundOffAmountChanged(ChangeEventArgs<decimal> args)
    {
        _sale.RoundOffAmount = args.Value;
        await SaveTransactionFile(true);
    }
    #endregion

    #region Cart
    private async Task UpdateQuantity(SaleItemCartModel item, decimal newQuantity)
    {
        if (item is null || _isProcessing)
            return;

        item.Quantity = Math.Max(0, newQuantity);

        if (item.Quantity == 0)
            _cart.Remove(item);

        await SaveTransactionFile();
    }

    private async Task UpdateRate(SaleItemCartModel item, decimal newRate)
    {
        if (item is null || _isProcessing)
            return;
        item.Rate = Math.Max(0, newRate);
        await SaveTransactionFile();
    }
    #endregion

    #region Payment
    private void AddPayment()
    {
        if (_paymentAmount <= 0 || _selectedPaymentMethod == null || _selectedPaymentMethod.Id <= 0)
            return;

        if (_paymentAmount > _remainingAmount)
        {
            ShowError("Invalid Payment Amount", $"Payment amount cannot exceed remaining amount of ₹{_remainingAmount:N2}");
            return;
        }

        if (_payments.Any(p => p.Method == _selectedPaymentMethod.Name))
            _payments.First(p => p.Method == _selectedPaymentMethod.Name).Amount += _paymentAmount;

        else
            _payments.Add(new()
            {
                Id = _selectedPaymentMethod.Id,
                Method = _selectedPaymentMethod.Name,
                Amount = _paymentAmount
            });

        // Reset for next payment
        _paymentAmount = _remainingAmount;
        _selectedPaymentMethod = _paymentMethods.FirstOrDefault(_ => _.Id != _selectedPaymentMethod.Id);
        StateHasChanged();
    }

    private void RemovePayment(PaymentItem payment)
    {
        _payments.Remove(payment);
        _paymentAmount = _remainingAmount;
        StateHasChanged();
    }

    private async Task ConfirmPayment(bool thermal = false)
    {
        _sale.Cash = _payments.FirstOrDefault(p => p.Id == PaymentModeData.GetPaymentModes().FirstOrDefault(pm => pm.Name == "Cash")?.Id)?.Amount ?? 0;
        _sale.Card = _payments.FirstOrDefault(p => p.Id == PaymentModeData.GetPaymentModes().FirstOrDefault(pm => pm.Name == "Card")?.Id)?.Amount ?? 0;
        _sale.UPI = _payments.FirstOrDefault(p => p.Id == PaymentModeData.GetPaymentModes().FirstOrDefault(pm => pm.Name == "UPI")?.Id)?.Amount ?? 0;
        _sale.Credit = _payments.FirstOrDefault(p => p.Id == PaymentModeData.GetPaymentModes().FirstOrDefault(pm => pm.Name == "Credit")?.Id)?.Amount ?? 0;

        await SaveTransaction(thermal);
    }
    #endregion

    #region Saving
    private async Task UpdateFinancialDetails(bool customRoundOff = false)
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

        _sale.TotalItems = _cart.Count;
        _sale.TotalQuantity = _cart.Sum(x => x.Quantity);
        _sale.BaseTotal = _cart.Sum(x => x.BaseTotal);
        _sale.ItemDiscountAmount = _cart.Sum(x => x.DiscountAmount);
        _sale.TotalAfterItemDiscount = _cart.Sum(x => x.AfterDiscount);
        _sale.TotalInclusiveTaxAmount = _cart.Where(x => x.InclusiveTax).Sum(x => x.TotalTaxAmount);
        _sale.TotalExtraTaxAmount = _cart.Where(x => !x.InclusiveTax).Sum(x => x.TotalTaxAmount);
        _sale.TotalAfterTax = _cart.Sum(x => x.Total);

        _sale.OtherChargesPercent = 0;
        _sale.OtherChargesAmount = _sale.TotalAfterTax * _sale.OtherChargesPercent / 100;
        var totalAfterOtherCharges = _sale.TotalAfterTax + _sale.OtherChargesAmount;

        _sale.DiscountAmount = totalAfterOtherCharges * _sale.DiscountPercent / 100;
        var totalAfterDiscount = totalAfterOtherCharges - _sale.DiscountAmount;

        if (!customRoundOff)
            _sale.RoundOffAmount = Math.Round(totalAfterDiscount) - totalAfterDiscount;

        _sale.TotalAmount = totalAfterDiscount + _sale.RoundOffAmount;
    }

    private async Task SaveTransactionFile(bool customRoundOff = false)
    {
        if (_isProcessing || _isLoading)
            return;

        try
        {
            _isProcessing = true;

            await UpdateFinancialDetails(customRoundOff);

            if (!_cart.Any(x => x.Quantity > 0) && await DataStorageService.LocalExists(StorageFileNames.SaleMobileCartDataFileName))
                await DataStorageService.LocalRemove(StorageFileNames.SaleMobileCartDataFileName);
            else
                await DataStorageService.LocalSaveAsync(StorageFileNames.SaleMobileCartDataFileName, System.Text.Json.JsonSerializer.Serialize(_cart.Where(_ => _.Quantity > 0)));

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

    private async Task<bool> ValidateForm()
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

        if (_sale.TotalItems <= 0)
        {
            ShowError("Total Items", "The total number of items in the cart must be greater than zero.");
            return false;
        }

        if (_sale.TotalQuantity <= 0)
        {
            ShowError("Total Quantity", "The total quantity of items in the cart must be greater than zero.");
            return false;
        }

        if (_sale.TotalAmount < 0)
        {
            ShowError("Total Amount", "The total amount of the transaction cannot be negative.");
            return false;
        }

        if (_user.LocationId <= 0)
        {
            ShowError("Location", "Please select a valid location for the sale.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(_selectedCustomer.Name) && !string.IsNullOrWhiteSpace(_selectedCustomer.Number))
        {
            ShowError("Customer Name Missing", "Please enter a name for the new customer or clear the customer field.");
            return false;
        }

        if (_selectedCustomer.Id > 0)
        {
            _selectedCustomer = await CommonData.LoadTableDataById<CustomerModel>(TableNames.Customer, _selectedCustomer.Id);
            _sale.CustomerId = _selectedCustomer.Id;
        }
        else if (!string.IsNullOrWhiteSpace(_selectedCustomer.Number) && _selectedCustomer.Id == 0)
        {
            _selectedCustomer.Id = await CustomerData.InsertCustomer(_selectedCustomer);
            _sale.CustomerId = _selectedCustomer.Id;
        }
        else
        {
            _selectedCustomer = new();
            _sale.CustomerId = null;
        }

        if (_sale.Cash < 0 || _sale.Card < 0 || _sale.Credit < 0 || _sale.UPI < 0)
        {
            ShowError("Payment Amounts", "Payment amounts (Cash, Card, Credit, UPI) cannot be negative. Please correct the amounts before saving.");
            return false;
        }

        if (_sale.Cash + _sale.Card + _sale.Credit + _sale.UPI != _sale.TotalAmount)
        {
            ShowError("Payment Amount Mismatch", "The sum of payment amounts (Cash, Card, Credit, UPI) must equal the total amount of the transaction. Please correct the amounts before saving.");
            return false;
        }

        _sale.Remarks = _sale.Remarks?.Trim();
        if (string.IsNullOrWhiteSpace(_sale.Remarks))
            _sale.Remarks = null;

        _sale.Status = true;
        _sale.OrderId = null;
        _sale.LocationId = _user.LocationId;
        _sale.CreatedBy = _user.Id;
        _sale.CompanyId = int.Parse((await SettingsData.LoadSettingsByKey(SettingsKeys.PrimaryCompanyLinkingId)).Value);
        _sale.TransactionDateTime = await CommonData.LoadCurrentDateTime();
        _sale.FinancialYearId = (await FinancialYearData.LoadFinancialYearByDateTime(_sale.TransactionDateTime)).Id;
        _sale.CreatedAt = await CommonData.LoadCurrentDateTime();
        _sale.CreatedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform();
        _sale.TransactionNo = await GenerateCodes.GenerateSaleTransactionNo(_sale);

        return true;
    }

    private async Task SaveTransaction(bool thermal = false)
    {
        if (_isProcessing || _isLoading)
            return;

        try
        {
            _isProcessing = true;
            StateHasChanged();

            await SaveTransactionFile(true);

            if (!await ValidateForm())
            {
                _isProcessing = false;
                return;
            }

            _sale.Id = await SaleData.SaveTransaction(_sale, _cart);

            await DataStorageService.LocalRemove(StorageFileNames.SaleMobileCartDataFileName);
            await SendLocalNotification(_sale.Id);

            if (thermal)
            {
                var content = await SaleThermalPrint.GenerateThermalBill(_sale.Id);
                await JSRuntime.InvokeVoidAsync("printToPrinter", content.ToString());

                await Task.Delay(2000);
            }

            else
            {
                var (pdfStream, fileName) = await SaleInvoiceExport.ExportInvoice(_sale.Id, InvoiceExportType.PDF);
                await SaveAndViewService.SaveAndView(fileName, pdfStream);
            }

            NavigationManager.NavigateTo(PageRouteNames.SaleMobileConfirmation, true);
        }
        catch (Exception ex)
        {
            ShowError("An Error Occurred While Saving Sale", ex.Message);
        }
        finally
        {
            _isProcessing = false;
        }
    }
    #endregion

    #region Utilities
    private async Task SendLocalNotification(int saleId)
    {
        var sale = await CommonData.LoadTableDataById<SaleOverviewModel>(ViewNames.SaleOverview, saleId);
        await NotificationService.ShowLocalNotification(
            sale.Id,
            "Sale Placed",
            $"{sale.TransactionNo}",
            $"Your sale #{sale.TransactionNo} has been successfully placed | Total Items: {sale.TotalItems} | Total Qty: {sale.TotalQuantity} | Date: {sale.TransactionDateTime:dd/MM/yy hh:mm tt} | Remarks: {sale.Remarks}");
    }

    private async Task ShowConfirmationDialog()
    {
        await SaveTransactionFile();
        _showConfirmDialog = true;
    }

    private void ShowPaymentDialog()
    {
        // Initialize payment amount to remaining amount
        _paymentAmount = _remainingAmount;
        _selectedPaymentMethod = _paymentMethods.FirstOrDefault();
        _showConfirmDialog = false;
        _showPaymentDialog = true;
        StateHasChanged();
    }

    private void CancelPayment()
    {
        _payments.Clear();
        _paymentAmount = 0;
        _selectedPaymentMethod = _paymentMethods.FirstOrDefault();
        _showPaymentDialog = false;
        _showConfirmDialog = true;
        StateHasChanged();
    }

    private void ShowError(string title, string message)
    {
        _validationErrors.Add((title, message));
        _showValidationDialog = true;
        StateHasChanged();
    }
    #endregion
}