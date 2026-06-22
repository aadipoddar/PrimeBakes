using PrimeBakesLibrary.Accounts.Masters.Data;
using PrimeBakesLibrary.Operations.Settings;
using PrimeBakesLibrary.Operations.User;
using PrimeBakesLibrary.Store.Customer.Data;
using PrimeBakesLibrary.Store.Customer.Models;
using PrimeBakesLibrary.Store.PaymentMode;
using PrimeBakesLibrary.Store.Product.Models;
using PrimeBakesLibrary.Store.Sale.Data;
using PrimeBakesLibrary.Store.Sale.Models;

using System.Text.Json;

namespace PrimeBakes.Shared.Pages.Store.Sale.Mobile;

public partial class SaleMobilePaymentPage
{
	private UserModel _user;

	private bool _isLoading = true;
	private bool _isProcessing = false;
	private bool _showValidationDialog = false;

	private CustomerModel _selectedCustomer = new();
	private readonly SaleModel _sale = new();

	private List<ProductModel> _products = [];
	private List<TaxModel> _taxes = [];
	private List<SaleItemCartModel> _cart = [];
	private readonly List<(string Field, string Message)> _validationErrors = [];
	private readonly List<PaymentItem> _payments = [];
	private readonly List<PaymentModeModel> _paymentMethods = [.. PaymentModeData.GetPaymentModes().Where(m => m.Name != "Credit")];

	private PaymentModeModel _selectedPaymentMethod = new();
	private decimal _paymentAmount = 0;
	private decimal _remainingAmount => _sale.TotalAmount - _payments.Sum(p => p.Amount);

	#region Load Data
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		_user = await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, [UserRoles.Store]);
		await LoadData();
	}

	private async Task LoadData()
	{
		_products = await CommonData.LoadTableData<ProductModel>(StoreNames.Product);
		_taxes = await CommonData.LoadTableData<TaxModel>(StoreNames.Tax);

		if (await DataStorageService.LocalExists(StorageFileNames.SaleMobileCartDataFileName))
			_cart = JsonSerializer.Deserialize<List<SaleItemCartModel>>(await DataStorageService.LocalGetAsync(StorageFileNames.SaleMobileCartDataFileName)) ?? [];

		if (_cart.Count == 0)
			NavigationManager.NavigateTo(StoreRouteNames.SaleMobile, true);

		_cart = [.. _cart.OrderBy(x => x.ItemName)];

		_selectedPaymentMethod = _paymentMethods.FirstOrDefault();

		_isLoading = false;
		await SaveTransactionFile();
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

		if (args.Length > 10)
			args = args[..10];

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

	private async Task OnDiscountPercentChanged(string raw)
	{
		_sale.DiscountPercent = Math.Clamp(ParseDecimal(raw, allowNegative: false), 0, 100);
		await SaveTransactionFile();
	}

	private async Task AdjustDiscountPercent(decimal delta)
	{
		_sale.DiscountPercent = Math.Clamp(_sale.DiscountPercent + delta, 0, 100);
		await SaveTransactionFile();
	}

	private async Task OnOtherChargesPercentChanged(string raw)
	{
		_sale.OtherChargesPercent = Math.Clamp(ParseDecimal(raw, allowNegative: false), 0, 100);
		await SaveTransactionFile();
	}

	private async Task AdjustOtherChargesPercent(decimal delta)
	{
		_sale.OtherChargesPercent = Math.Clamp(_sale.OtherChargesPercent + delta, 0, 100);
		await SaveTransactionFile();
	}

	private async Task OnRoundOffAmountChanged(string raw)
	{
		_sale.RoundOffAmount = ParseDecimal(raw, allowNegative: true);
		await SaveTransactionFile(true);
	}

	private async Task AdjustRoundOff(decimal delta)
	{
		_sale.RoundOffAmount += delta;
		await SaveTransactionFile(true);
	}

	private static decimal ParseDecimal(string raw, bool allowNegative)
	{
		if (string.IsNullOrWhiteSpace(raw))
			return 0;

		var allowed = allowNegative ? "0123456789.-" : "0123456789.";
		var cleaned = new string([.. raw.Where(c => allowed.Contains(c))]);
		var negative = allowNegative && cleaned.StartsWith('-');
		cleaned = cleaned.Replace("-", "");

		if (!decimal.TryParse(cleaned, out var value))
			return 0;

		return negative ? -value : value;
	}
	#endregion

	#region Payment
	private void SelectPaymentMethod(PaymentModeModel method)
	{
		if (method is null || _isProcessing)
			return;

		_selectedPaymentMethod = method;
	}

	private void FillRemaining()
	{
		if (_remainingAmount <= 0)
			return;

		_paymentAmount = _remainingAmount;
	}

	private void OnPaymentAmountChanged(string raw) => _paymentAmount = ParseDecimal(raw, allowNegative: false);

	private void AddPayment()
	{
		if (_paymentAmount <= 0 || _selectedPaymentMethod is null || _selectedPaymentMethod.Id <= 0)
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
		_selectedPaymentMethod = _paymentMethods.FirstOrDefault(_ => _.Id != _selectedPaymentMethod.Id) ?? _selectedPaymentMethod;
		StateHasChanged();
	}

	private void RemovePayment(PaymentItem payment)
	{
		_payments.Remove(payment);
		_paymentAmount = _remainingAmount;
		StateHasChanged();
	}

	private void ConfirmPayment()
	{
		_sale.Cash = _payments.FirstOrDefault(p => p.Id == PaymentModeData.GetPaymentModes().FirstOrDefault(pm => pm.Name == "Cash")?.Id)?.Amount ?? 0;
		_sale.Card = _payments.FirstOrDefault(p => p.Id == PaymentModeData.GetPaymentModes().FirstOrDefault(pm => pm.Name == "Card")?.Id)?.Amount ?? 0;
		_sale.UPI = _payments.FirstOrDefault(p => p.Id == PaymentModeData.GetPaymentModes().FirstOrDefault(pm => pm.Name == "UPI")?.Id)?.Amount ?? 0;
		_sale.Credit = _payments.FirstOrDefault(p => p.Id == PaymentModeData.GetPaymentModes().FirstOrDefault(pm => pm.Name == "Credit")?.Id)?.Amount ?? 0;
	}
	#endregion

	#region Saving
	private async Task UpdateFinancialDetails(bool customRoundOff = false)
	{
		SaleData.ApplyItemFinancialDetails(_cart, _products, _taxes);

		foreach (var item in _cart.Where(i => i.Quantity > 0))
		{
			var perUnitCost = item.Total / item.Quantity;
			var withOtherCharges = perUnitCost * (1 + _sale.OtherChargesPercent / 100);
			item.NetRate = withOtherCharges * (1 - _sale.DiscountPercent / 100);
		}

		_sale.TotalItems = _cart.Count;
		_sale.TotalQuantity = _cart.Sum(x => x.Quantity);
		_sale.BaseTotal = _cart.Sum(x => x.BaseTotal);
		_sale.ItemDiscountAmount = _cart.Sum(x => x.DiscountAmount);
		_sale.TotalAfterItemDiscount = _cart.Sum(x => x.AfterDiscount);
		_sale.TotalInclusiveTaxAmount = _cart.Where(x => x.InclusiveTax).Sum(x => x.TotalTaxAmount);
		_sale.TotalExtraTaxAmount = _cart.Where(x => !x.InclusiveTax).Sum(x => x.TotalTaxAmount);
		_sale.TotalAfterTax = _cart.Sum(x => x.Total);

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
				await DataStorageService.LocalSaveAsync(StorageFileNames.SaleMobileCartDataFileName, JsonSerializer.Serialize(_cart.Where(_ => _.Quantity > 0)));

			VibrationService.VibrateHapticClick();
		}
		catch (Exception ex)
		{
			ShowError("An Error Occurred While Saving Cart Data", ex.Message);
		}
		finally
		{
			if (_paymentAmount <= 0)
				_paymentAmount = Math.Max(0, _remainingAmount);

			_isProcessing = false;
			StateHasChanged();
		}
	}

	private async Task SaveTransaction()
	{
		if (_isProcessing || _isLoading)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();

			await SaveTransactionFile(true);
			ConfirmPayment();

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

			_sale.Id = await SaleData.SaveTransaction(_sale, SaleData.ConvertCartToDetails(_cart), _selectedCustomer);
			await NotificationNavigate(_sale.Id);
		}
		catch (Exception ex)
		{
			ShowError("An Error Occurred While Saving Sale", ex.Message);
		}
		finally
		{
			_isProcessing = false;
			StateHasChanged();
		}
	}
	#endregion

	#region Utilities
	private async Task NotificationNavigate(int saleId)
	{
		var overview = await CommonData.LoadTableDataById<SaleOverviewModel>(StoreNames.SaleOverview, saleId);
		await DataStorageService.LocalSaveAsync(StorageFileNames.SaleMobileDataFileName, JsonSerializer.Serialize(overview));
		await DataStorageService.LocalSaveAsync(StorageFileNames.SaleMobileCartDataFileName, JsonSerializer.Serialize(_cart.Where(_ => _.Quantity > 0)));

		await SendLocalNotification(overview);
		VibrationService.VibrateWithTime(500);
		NavigationManager.NavigateTo(StoreRouteNames.SaleMobileConfirmation, true);
	}

	private async Task SendLocalNotification(SaleOverviewModel sale) =>
		await NotificationService.ShowLocalNotification(
			sale.Id,
			"Sale Placed",
			$"{sale.TransactionNo}",
			$"Your sale #{sale.TransactionNo} has been successfully placed | Total Items: {sale.TotalItems} | Total Qty: {sale.TotalQuantity} | Date: {sale.TransactionDateTime:dd/MM/yy hh:mm tt} | Remarks: {sale.Remarks}");

	private void ShowError(string title, string message)
	{
		_validationErrors.Clear();
		_validationErrors.Add((title, message));
		_showValidationDialog = true;
		StateHasChanged();
	}
	#endregion
}
