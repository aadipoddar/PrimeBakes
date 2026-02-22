using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Operations;
using PrimeBakesLibrary.Data.Restaurant.Bill;
using PrimeBakesLibrary.Data.Store.Masters;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Exporting.Restaurant.Bill;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Operations;
using PrimeBakesLibrary.Models.Restuarant.Bill;
using PrimeBakesLibrary.Models.Restuarant.Dining;
using PrimeBakesLibrary.Models.Store.Masters;
using PrimeBakesLibrary.Models.Store.Product;

using Syncfusion.Blazor.Inputs;

namespace PrimeBakes.Shared.Pages.Restaurant.Bill.Mobile;

public partial class BillMobilePaymentPage
{
	[Parameter] public int? DiningTableId { get; set; }

	private UserModel _user;

	private bool _isLoading = true;
	private bool _isProcessing = false;
	private bool _showValidationDialog = false;

	private DiningTableModel _diningTable;
	private CustomerModel _selectedCustomer = new();
	private BillModel _bill = new();

	private List<BillItemCartModel> _cart = [];
	private List<BillDetailModel> _finalCart = [];
	private List<BillDetailModel> _previousCart = [];
	private readonly List<(string Field, string Message)> _validationErrors = [];
	private readonly List<PaymentItem> _payments = [];
	private readonly List<PaymentModeModel> _paymentMethods = PaymentModeData.GetPaymentModes();

	private PaymentModeModel _selectedPaymentMethod = new();
	private decimal _paymentAmount = 0;
	private decimal _remainingAmount => _bill.TotalAmount - _payments.Sum(p => p.Amount);

	#region Load Data
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		_user = await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, [UserRoles.Restaurant]);
		await LoadData();
		_isLoading = false;
		await UpdateFinancialDetails();
		StateHasChanged();
	}

	private async Task LoadData()
	{
		if (!await ResolveBillContext())
			return;

		await LoadDiningTable();
		await LoadRunningBill();
		await LoadCart();

		StateHasChanged();
	}

	private async Task<bool> ResolveBillContext()
	{
		if (!DiningTableId.HasValue)
		{
			NavigationManager.NavigateTo(PageRouteNames.DiningMobileDashboard, true);
			return false;
		}

		try
		{
			var diningTable = await CommonData.LoadTableDataById<DiningTableModel>(TableNames.DiningTable, DiningTableId.Value);
			if (diningTable is null)
			{
				NavigationManager.NavigateTo(PageRouteNames.DiningMobileDashboard, true);
				return false;
			}

			var diningArea = await CommonData.LoadTableDataById<DiningAreaModel>(TableNames.DiningArea, diningTable.DiningAreaId);
			if (diningArea is null)
			{
				NavigationManager.NavigateTo(PageRouteNames.DiningMobileDashboard, true);
				return false;
			}

			if (_user.LocationId != 1 && diningArea.LocationId != _user.LocationId)
			{
				NavigationManager.NavigateTo(PageRouteNames.DiningMobileDashboard, true);
				return false;
			}

			return true;
		}
		catch (Exception)
		{
			NavigationManager.NavigateTo(PageRouteNames.DiningMobileDashboard, true);
			return false;
		}
	}

	private async Task LoadDiningTable()
	{
		if (!DiningTableId.HasValue)
			return;

		_diningTable = await CommonData.LoadTableDataById<DiningTableModel>(TableNames.DiningTable, DiningTableId.Value);
		_bill.DiningTableId = _diningTable?.Id ?? 0;
	}

	private async Task LoadRunningBill()
	{
		_previousCart.Clear();

		var runningBills = await BillData.LoadRunningBillByLocationId(_user.LocationId);
		var runningBill = runningBills.FirstOrDefault(b => b.DiningTableId == DiningTableId && b.Running);
		if (runningBill is null)
			return;

		_bill = runningBill;

		_previousCart = await CommonData.LoadTableDataByMasterId<BillDetailModel>(TableNames.BillDetail, runningBill.Id);

		if (_bill.CustomerId.HasValue)
			_selectedCustomer = await CommonData.LoadTableDataById<CustomerModel>(TableNames.Customer, _bill.CustomerId.Value);
	}

	private async Task LoadCart()
	{
		_cart.Clear();

		if (await DataStorageService.LocalExists(StorageFileNames.BillMobileCartDataFileName))
		{
			var items = System.Text.Json.JsonSerializer.Deserialize<List<BillItemCartModel>>(await DataStorageService.LocalGetAsync(StorageFileNames.BillMobileCartDataFileName)) ?? [];
			foreach (var item in items)
				_cart.Add(item);
		}

		_cart = [.. _cart.OrderBy(x => x.ItemName)];

		_selectedPaymentMethod = _paymentMethods.FirstOrDefault();
	}
	#endregion

	#region Changed Events
	private async Task OnCustomerNumberChanged(string args)
	{
		if (string.IsNullOrWhiteSpace(args))
		{
			_selectedCustomer = new();
			_bill.CustomerId = null;
			await UpdateFinancialDetails();
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

		_bill.CustomerId = _selectedCustomer.Id;
		await UpdateFinancialDetails();
	}

	private async Task OnDiscountPercentChanged(ChangeEventArgs<decimal> args)
	{
		_bill.DiscountPercent = args.Value;
		await UpdateFinancialDetails();
	}

	private async Task OnRoundOffAmountChanged(ChangeEventArgs<decimal> args)
	{
		_bill.RoundOffAmount = args.Value;
		await UpdateFinancialDetails(true);
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

	private void ConfirmPayment()
	{
		_bill.Cash = _payments.FirstOrDefault(p => p.Id == PaymentModeData.GetPaymentModes().FirstOrDefault(pm => pm.Name == "Cash")?.Id)?.Amount ?? 0;
		_bill.Card = _payments.FirstOrDefault(p => p.Id == PaymentModeData.GetPaymentModes().FirstOrDefault(pm => pm.Name == "Card")?.Id)?.Amount ?? 0;
		_bill.UPI = _payments.FirstOrDefault(p => p.Id == PaymentModeData.GetPaymentModes().FirstOrDefault(pm => pm.Name == "UPI")?.Id)?.Amount ?? 0;
		_bill.Credit = _payments.FirstOrDefault(p => p.Id == PaymentModeData.GetPaymentModes().FirstOrDefault(pm => pm.Name == "Credit")?.Id)?.Amount ?? 0;
	}
	#endregion

	#region Saving
	private async Task UpdateFinancialDetails(bool customRoundOff = false)
	{
		if (_isProcessing || _isLoading)
			return;

		try
		{
			_isProcessing = true;

			_finalCart.Clear();
			_finalCart = BillData.ConvertCartToDetails(_cart, _bill.Id);
			_finalCart.AddRange(_previousCart);

			var taxes = await CommonData.LoadTableData<TaxModel>(TableNames.Tax);
			var items = await CommonData.LoadTableData<ProductModel>(TableNames.Product);

			foreach (var item in _finalCart.Where(_ => _.Quantity > 0))
			{
				item.Id = 0;
				item.DiscountPercent = 0;
				item.DiscountAmount = 0;

				item.BaseTotal = item.Rate * item.Quantity;
				item.AfterDiscount = item.BaseTotal - item.DiscountAmount;

				var selectedItem = items.FirstOrDefault(s => s.Id == item.ProductId);
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

				item.NetRate = item.Total / item.Quantity * (1 - _bill.DiscountPercent / 100);
				item.Remarks = string.IsNullOrWhiteSpace(item.Remarks) ? null : item.Remarks.Trim();
			}

			_bill.TotalItems = _finalCart.Count(x => x.Quantity > 0);
			_bill.TotalQuantity = _finalCart.Sum(x => x.Quantity);
			_bill.BaseTotal = _finalCart.Sum(x => x.BaseTotal);
			_bill.ItemDiscountAmount = _finalCart.Sum(x => x.DiscountAmount);
			_bill.TotalAfterItemDiscount = _finalCart.Sum(x => x.AfterDiscount);
			_bill.TotalInclusiveTaxAmount = _finalCart.Where(x => x.InclusiveTax).Sum(x => x.TotalTaxAmount);
			_bill.TotalExtraTaxAmount = _finalCart.Where(x => !x.InclusiveTax).Sum(x => x.TotalTaxAmount);
			_bill.TotalAfterTax = _finalCart.Sum(x => x.Total);

			_bill.DiscountAmount = _bill.TotalAfterTax * _bill.DiscountPercent / 100;
			var totalAfterDiscount = _bill.TotalAfterTax - _bill.DiscountAmount;

			_bill.ServiceChargeAmount = totalAfterDiscount * _bill.ServiceChargePercent / 100;
			var totalAfterServiceCharge = totalAfterDiscount + _bill.ServiceChargeAmount;

			if (!customRoundOff)
				_bill.RoundOffAmount = Math.Round(totalAfterServiceCharge) - totalAfterServiceCharge;

			_bill.TotalAmount = totalAfterServiceCharge + _bill.RoundOffAmount;

			VibrationService.VibrateHapticClick();
		}
		catch (Exception ex)
		{
			ShowError("An Error Occurred While Saving Cart Data", ex.Message);
		}
		finally
		{
			_paymentAmount = Math.Max(0, _remainingAmount);
			_isProcessing = false;
			StateHasChanged();
		}
	}

	private async Task<bool> ValidateForm()
	{
		_validationErrors.Clear();

		if (_finalCart.Count == 0)
		{
			ShowError("Cart", "The cart is empty. Please add items to the cart before saving the order.");
			return false;
		}

		if (_finalCart.Any(item => item.Quantity <= 0))
		{
			ShowError("Quantity", "All items in the cart must have a quantity greater than zero.");
			return false;
		}

		if (_bill.TotalItems <= 0)
		{
			ShowError("Total Items", "The total number of items in the cart must be greater than zero.");
			return false;
		}

		if (_bill.TotalQuantity <= 0)
		{
			ShowError("Total Quantity", "The total quantity of items in the cart must be greater than zero.");
			return false;
		}

		if (_bill.TotalAmount < 0)
		{
			ShowError("Total Amount", "The total amount of the transaction cannot be negative.");
			return false;
		}

		if (_user.LocationId <= 0)
		{
			ShowError("Location", "Please select a valid location for the bill.");
			return false;
		}

		if (_bill.DiningTableId <= 0)
		{
			ShowError("Dining Table", "Please select a valid dining table for this bill.");
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
			_bill.CustomerId = _selectedCustomer.Id;
		}
		else if (!string.IsNullOrWhiteSpace(_selectedCustomer.Number) && _selectedCustomer.Id == 0)
		{
			_selectedCustomer.Id = await CustomerData.InsertCustomer(_selectedCustomer);
			_bill.CustomerId = _selectedCustomer.Id;
		}
		else
		{
			_selectedCustomer = new();
			_bill.CustomerId = null;
		}

		if (_bill.Cash < 0 || _bill.Card < 0 || _bill.Credit < 0 || _bill.UPI < 0)
		{
			ShowError("Payment Amounts", "Payment amounts (Cash, Card, Credit, UPI) cannot be negative. Please correct the amounts before saving.");
			return false;
		}

		_bill.Remarks = _bill.Remarks?.Trim();
		if (string.IsNullOrWhiteSpace(_bill.Remarks))
			_bill.Remarks = null;

		_bill.Status = true;
		_bill.LocationId = _user.LocationId;
		_bill.CreatedBy = _user.Id;
		_bill.LastModifiedBy = _user.Id;
		_bill.CompanyId = int.Parse((await SettingsData.LoadSettingsByKey(SettingsKeys.PrimaryCompanyLinkingId)).Value);
		_bill.TransactionDateTime = await CommonData.LoadCurrentDateTime();
		_bill.FinancialYearId = (await FinancialYearData.LoadFinancialYearByDateTime(_bill.TransactionDateTime)).Id;
		_bill.CreatedAt = await CommonData.LoadCurrentDateTime();
		_bill.LastModifiedAt = _bill.CreatedAt;
		_bill.CreatedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform();
		_bill.LastModifiedFromPlatform = _bill.CreatedFromPlatform;
		if (_bill.Id == 0)
			_bill.TransactionNo = await GenerateCodes.GenerateBillTransactionNo(_bill);

		return true;
	}

	private void HandleBillSettlement()
	{
		_bill.Running = _bill.Cash + _bill.Card + _bill.UPI + _bill.Credit != _bill.TotalAmount;

		if (!_bill.Running)
			foreach (var item in _finalCart)
				item.KOTPrint = false;

		else
		{
			_bill.Cash = 0;
			_bill.Card = 0;
			_bill.UPI = 0;
			_bill.Credit = 0;
		}
	}

	private async Task HandleKOTPrint()
	{
		if (!_finalCart.Any(item => item.KOTPrint))
			return;

		var kotPrintStream = await KOTThermalPrint.GenerateThermalBill(_bill.Id);
		await JSRuntime.InvokeVoidAsync("printToPrinter", kotPrintStream.ToString());
		await Task.Delay(2000);
	}

	private async Task SaveTransaction(bool thermal = false)
	{
		if (_isProcessing || _isLoading)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();

			await UpdateFinancialDetails(true);
			ConfirmPayment();

			if (!await ValidateForm())
			{
				_isProcessing = false;
				return;
			}

			HandleBillSettlement();

			_bill.Id = await BillData.SaveTransaction(_bill, billDetails: _finalCart);
			await HandleKOTPrint();

			await DataStorageService.LocalRemove(StorageFileNames.BillMobileCartDataFileName);
			await SendLocalNotification(_bill.Id);

			if (thermal)
			{
				var content = await BillThermalPrint.GenerateThermalBill(_bill.Id);
				await JSRuntime.InvokeVoidAsync("printToPrinter", content.ToString());
				await Task.Delay(2000);
			}

			else if (!_bill.Running)
			{
				var (pdfStream, fileName) = await BillInvoiceExport.ExportInvoice(_bill.Id, InvoiceExportType.PDF);
				await SaveAndViewService.SaveAndView(fileName, pdfStream);
			}

			if (_bill.Running)
				NavigationManager.NavigateTo(PageRouteNames.DiningMobileDashboard, true);
			else
				NavigationManager.NavigateTo(PageRouteNames.BillMobileConfirmation, true);
		}
		catch (Exception ex)
		{
			ShowError("An Error Occurred While Saving Bill", ex.Message);
		}
		finally
		{
			_isProcessing = false;
		}
	}
	#endregion

	#region Utilities
	private async Task SendLocalNotification(int billId)
	{
		var bill = await CommonData.LoadTableDataById<BillOverviewModel>(ViewNames.BillOverview, billId);
		await NotificationService.ShowLocalNotification(
			bill.Id,
			"Bill Placed",
			$"{bill.TransactionNo}",
			$"Your bill #{bill.TransactionNo} has been successfully placed | Total Items: {bill.TotalItems} | Total Qty: {bill.TotalQuantity} | Date: {bill.TransactionDateTime:dd/MM/yy hh:mm tt} | Remarks: {bill.Remarks}");
	}

	private void ShowError(string title, string message)
	{
		_validationErrors.Add((title, message));
		_showValidationDialog = true;
		StateHasChanged();
	}
	#endregion
}