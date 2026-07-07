using Microsoft.AspNetCore.Components;

using PrimeBakes.Library.Accounts.Masters.Data;
using PrimeBakes.Library.Accounts.Masters.Models;
using PrimeBakes.Library.Operations.Location;
using PrimeBakes.Library.Operations.Settings;
using PrimeBakes.Library.Operations.User;
using PrimeBakes.Library.Store.Customer.Data;
using PrimeBakes.Library.Store.Customer.Models;
using PrimeBakes.Library.Store.PaymentMode;
using PrimeBakes.Library.Store.Product.Data;
using PrimeBakes.Library.Store.Product.Models;
using PrimeBakes.Library.Store.Sale.Data;
using PrimeBakes.Library.Store.Sale.Models;
using PrimeBakes.Shared.Components.Dialog;
using PrimeBakes.Shared.Components.Input;

using Syncfusion.Blazor.Grids;

using System.Text.Json;

namespace PrimeBakes.Shared.Pages.Store.Sale;

public partial class SaleReturnPage
{
	[Parameter] public int? Id { get; set; }

	private UserModel _user;

	private bool _isLoading = true;
	private bool _isProcessing = false;

	private CompanyModel _selectedCompany = new();
	private LocationModel _selectedLocation = new();
	private LedgerModel _selectedParty = null;
	private CustomerModel _selectedCustomer = new();
	private FinancialYearModel _selectedFinancialYear = new();
	private ProductLocationOverviewModel _selectedProduct = null;
	private SaleReturnItemCartModel _selectedCart = new();
	private SaleReturnModel _saleReturn = new();

	private List<CompanyModel> _companies = [];
	private List<LocationModel> _locations = [];
	private List<LedgerModel> _parties = [];
	private List<ProductLocationOverviewModel> _products = [];
	private List<TaxModel> _taxes = [];
	private List<SaleReturnItemCartModel> _cart = [];
	private readonly List<ContextMenuItemModel> _cartGridContextMenuItems =
	[
		new() { Text = "Edit (Insert)", Id = "EditCart", IconCss = "e-icons e-edit", Target = ".e-content" },
		new() { Text = "Delete (Del)", Id = "DeleteCart", IconCss = "e-icons e-trash", Target = ".e-content" }
	];

	private readonly List<PaymentItem> _paymentsCart = [];
	private readonly List<PaymentModeModel> _paymentMethods = PaymentModeData.GetPaymentModes();
	private PaymentModeModel _selectedPaymentMethod = new();
	private PaymentItem _selectedPaymentCart = new();
	private decimal _remainingAmount => _saleReturn.TotalAmount - _paymentsCart.Sum(p => p.Amount);

	private readonly List<ContextMenuItemModel> _paymentsCartGridContextMenuItems =
	[
		new() { Text = "Edit (Insert)", Id = "EditCart", IconCss = "e-icons e-edit", Target = ".e-content" },
		new() { Text = "Delete (Del)", Id = "DeleteCart", IconCss = "e-icons e-trash", Target = ".e-content" }
	];

	private CustomAutoComplete<CompanyModel> _firstFocus;
	private CustomAutoComplete<ProductLocationOverviewModel> _itemAutoComplete;
	private CustomNumericField<decimal> _otherChargesPercentField;
	private CustomAutoComplete<PaymentModeModel> _paymentModeAutoComplete;
	private SfGrid<SaleReturnItemCartModel> _sfCartGrid;
	private SfGrid<PaymentItem> _sfPaymentsCartGrid;

	private ToastNotification _toastNotification;

	#region Load Data
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		try
		{
			_user = await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, [UserRoles.Store]);
			await InitializePage();
		}
		catch { await ResetPage(); }
	}

	private async Task InitializePage()
	{
		await LoadData();
		await ResolveTransaction();
		await LoadSelections();
		await LoadItems();
		await ResolveCart();

		_isLoading = false;
		StateHasChanged();

		await SaveTransactionFile(true);

		if (_firstFocus is not null) await _firstFocus.FocusAsync();
	}

	private async Task LoadData()
	{
		_locations = await CommonData.LoadTableDataByStatus<LocationModel>(OperationNames.Location);
		_companies = await CommonData.LoadTableDataByStatus<CompanyModel>(AccountNames.Company);
		_parties = await CommonData.LoadTableDataByStatus<LedgerModel>(AccountNames.Ledger);

		_locations = [.. _locations.OrderBy(s => s.Name)];
		_companies = [.. _companies.OrderBy(s => s.Name)];
		_parties = [.. _parties.OrderBy(s => s.Name)];

		var mainCompanyId = await SettingsData.LoadSettingsByKey(SettingsKeys.PrimaryCompanyLinkingId);
		_selectedCompany = _companies.FirstOrDefault(s => s.Id.ToString() == mainCompanyId.Value) ?? _companies.FirstOrDefault();
		_selectedLocation = _locations.FirstOrDefault(s => s.Id == _user.LocationId);
	}

	private async Task ResolveTransaction()
	{
		try
		{
			if (await LoadExistingTransaction())
				return;

			if (await TryRestoreFromLocalStorage())
				return;

			await CreateNewTransaction();
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("An Error Occurred While Loading Transaction Data", ex.Message, ToastType.Error);
			await ResetPage();
		}
	}

	private async Task<bool> LoadExistingTransaction()
	{
		if (!Id.HasValue)
			return false;

		_saleReturn = await CommonData.LoadTableDataById<SaleReturnModel>(StoreNames.SaleReturn, Id.Value);
		if (_saleReturn is null || _saleReturn.Id == 0)
		{
			await _toastNotification.ShowAsync("Transaction Not Found", "The requested transaction could not be found.", ToastType.Error);
			await ResetPage();
		}

		return true;
	}

	private async Task<bool> TryRestoreFromLocalStorage()
	{
		if (!await DataStorageService.LocalExists(StorageFileNames.SaleReturnDataFileName))
			return false;

		try
		{
			_saleReturn = JsonSerializer.Deserialize<SaleReturnModel>(await DataStorageService.LocalGetAsync(StorageFileNames.SaleReturnDataFileName));
			return _saleReturn is not null;
		}
		catch
		{
			await DeleteLocalFiles();
			return false;
		}
	}

	private async Task CreateNewTransaction()
	{
		var currentDateTime = await CommonData.LoadCurrentDateTime();
		var financialYear = await FinancialYearData.LoadFinancialYearByDateTime(currentDateTime);

		_saleReturn = new()
		{
			Id = 0,
			TransactionNo = string.Empty,
			CompanyId = _selectedCompany.Id,
			LocationId = _user.LocationId,
			PartyId = null,
			CustomerId = null,
			TransactionDateTime = currentDateTime,
			FinancialYearId = financialYear is null ? 0 : financialYear.Id,
			CreatedBy = _user.Id,
			TotalItems = 0,
			ItemDiscountAmount = 0,
			TotalAfterItemDiscount = 0,
			TotalInclusiveTaxAmount = 0,
			TotalExtraTaxAmount = 0,
			TotalAfterTax = 0,
			DiscountPercent = 0,
			DiscountAmount = 0,
			OtherChargesPercent = 0,
			OtherChargesAmount = 0,
			RoundOffAmount = 0,
			TotalAmount = 0,
			Card = 0,
			Cash = 0,
			Credit = 0,
			UPI = 0,
			Remarks = null,
			FinancialAccountingId = null,
			CreatedAt = DateTime.Now,
			CreatedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform(),
			Status = true,
			LastModifiedAt = null,
			LastModifiedBy = null,
			LastModifiedFromPlatform = null
		};

		await DeleteLocalFiles();
	}

	private async Task LoadSelections()
	{
		_selectedLocation = _user.LocationId == 1
			? _locations.FirstOrDefault(s => s.Id == _saleReturn.LocationId) ?? _locations.FirstOrDefault()
			: _locations.FirstOrDefault(s => s.Id == _user.LocationId);
		_saleReturn.LocationId = _selectedLocation?.Id ?? 0;

		var mainCompanyId = await SettingsData.LoadSettingsByKey(SettingsKeys.PrimaryCompanyLinkingId);
		_selectedCompany = _user.LocationId == 1 && _saleReturn.CompanyId > 0
			? _companies.FirstOrDefault(s => s.Id == _saleReturn.CompanyId) ?? _companies.FirstOrDefault(s => s.Id.ToString() == mainCompanyId.Value)
			: _companies.FirstOrDefault(s => s.Id.ToString() == mainCompanyId.Value);
		_saleReturn.CompanyId = _selectedCompany?.Id ?? 0;

		if (_saleReturn.PartyId is not null && _saleReturn.LocationId == 1 && _saleReturn.PartyId > 0)
		{
			_selectedParty = _parties.FirstOrDefault(s => s.Id == _saleReturn.PartyId);

			var location = _locations.FirstOrDefault(s => s.LedgerId == _selectedParty?.Id);
			if (location is not null && Id is null)
				_saleReturn.DiscountPercent = location.Discount;
		}
		else
		{
			_selectedParty = null;
			_saleReturn.PartyId = null;
		}

		if (_saleReturn.CustomerId is not null && _saleReturn.CustomerId > 0)
			_selectedCustomer = await CommonData.LoadTableDataById<CustomerModel>(StoreNames.Customer, _saleReturn.CustomerId.Value);
		else
		{
			_selectedCustomer = new();
			_saleReturn.CustomerId = null;
		}

		_selectedFinancialYear = await CommonData.LoadTableDataById<FinancialYearModel>(AccountNames.FinancialYear, _saleReturn.FinancialYearId);
		SyncPaymentsFromSaleReturn();
	}

	private async Task LoadItems()
	{
		var saleReturnDate = DateOnly.FromDateTime(_saleReturn.TransactionDateTime);
		var products = await ProductLocationData.LoadProductLocationOverviewByProductLocationDate(null, _saleReturn.LocationId, saleReturnDate);

		var toLocation = _locations.FirstOrDefault(s => s.LedgerId == _selectedParty?.Id);
		if (toLocation is not null)
		{
			var toLocationProducts = await ProductLocationData.LoadProductLocationOverviewByProductLocationDate(null, toLocation.Id, saleReturnDate);
			products = [.. products.Where(x => toLocationProducts.Any(y => y.ProductId == x.ProductId))];
		}

		_products = [.. products.OrderBy(s => s.Name)];
		_taxes = await CommonData.LoadTableDataByStatus<TaxModel>(StoreNames.Tax);
	}

	private async Task ResolveCart()
	{
		try
		{
			_cart.Clear();

			if (await LoadExistingCart())
				return;

			if (await DataStorageService.LocalExists(StorageFileNames.SaleReturnCartDataFileName))
				_cart = JsonSerializer.Deserialize<List<SaleReturnItemCartModel>>(await DataStorageService.LocalGetAsync(StorageFileNames.SaleReturnCartDataFileName));
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("An Error Occurred While Loading Existing Cart", ex.Message, ToastType.Error);
			await ResetPage();
		}
	}

	private async Task<bool> LoadExistingCart()
	{
		if (_saleReturn.Id <= 0)
			return false;

		var existingCart = await CommonData.LoadTableDataByMasterId<SaleReturnDetailModel>(StoreNames.SaleReturnDetail, _saleReturn.Id);

		foreach (var item in existingCart)
		{
			var product = _products.FirstOrDefault(s => s.ProductId == item.ProductId);
			if (product is null)
			{
				var missing = await CommonData.LoadTableDataById<ProductModel>(StoreNames.Product, item.ProductId);
				await _toastNotification.ShowAsync("Item Not Found", $"The item {missing?.Name} (ID: {item.ProductId}) in the existing transaction cart was not found in the available items list. It may have been deleted or is inaccessible.", ToastType.Error);
				continue;
			}

			_cart.Add(new()
			{
				ItemId = item.ProductId,
				ItemName = product.Name,
				Quantity = item.Quantity,
				Rate = item.Rate,
				BaseTotal = item.BaseTotal,
				DiscountPercent = item.DiscountPercent,
				DiscountAmount = item.DiscountAmount,
				AfterDiscount = item.AfterDiscount,
				CGSTPercent = item.CGSTPercent,
				CGSTAmount = item.CGSTAmount,
				SGSTPercent = item.SGSTPercent,
				SGSTAmount = item.SGSTAmount,
				IGSTPercent = item.IGSTPercent,
				IGSTAmount = item.IGSTAmount,
				TotalTaxAmount = item.TotalTaxAmount,
				Total = item.Total,
				InclusiveTax = item.InclusiveTax,
				NetRate = item.NetRate,
				Remarks = item.Remarks
			});
		}

		return true;
	}
	#endregion

	#region Payment
	private void SyncPaymentsFromSaleReturn()
	{
		_paymentsCart.Clear();

		AddPaymentFromSaleReturn("Cash", _saleReturn.Cash);
		AddPaymentFromSaleReturn("Card", _saleReturn.Card);
		AddPaymentFromSaleReturn("UPI", _saleReturn.UPI);
		AddPaymentFromSaleReturn("Credit", _saleReturn.Credit);

		_selectedPaymentMethod = _selectedLocation?.Id == 1
			? _paymentMethods.FirstOrDefault(pm => pm.Name == "Credit") ?? _paymentMethods.FirstOrDefault()
			: _paymentMethods.FirstOrDefault();
		_selectedPaymentCart = new() { Amount = Math.Max(0, _remainingAmount) };
	}

	private void AddPaymentFromSaleReturn(string modeName, decimal amount)
	{
		if (amount <= 0)
			return;

		var mode = _paymentMethods.FirstOrDefault(pm => pm.Name == modeName);
		if (mode is null)
			return;

		_paymentsCart.Add(new()
		{
			Id = mode.Id,
			Method = mode.Name,
			Amount = amount
		});
	}

	private void ApplyPaymentsToSaleReturn()
	{
		_saleReturn.Cash = _paymentsCart.FirstOrDefault(p => p.Method == "Cash")?.Amount ?? 0;
		_saleReturn.Card = _paymentsCart.FirstOrDefault(p => p.Method == "Card")?.Amount ?? 0;
		_saleReturn.UPI = _paymentsCart.FirstOrDefault(p => p.Method == "UPI")?.Amount ?? 0;
		_saleReturn.Credit = _paymentsCart.FirstOrDefault(p => p.Method == "Credit")?.Amount ?? 0;
	}

	private void OnPaymentMethodChanged(PaymentModeModel value)
	{
		if (value is null || value.Id == 0)
		{
			_selectedPaymentMethod = new();
			_selectedPaymentCart = new();
			return;
		}

		_selectedPaymentMethod = value;

		_selectedPaymentCart.Id = value.Id;
		_selectedPaymentCart.Method = value.Name;
		_selectedPaymentCart.Amount = Math.Max(0, _saleReturn.TotalAmount - _paymentsCart.Where(p => p.Id != value.Id).Sum(p => p.Amount));
	}

	private async Task AddPaymentToCart()
	{
		if (_selectedPaymentMethod is null || _selectedPaymentMethod.Id <= 0 || _selectedPaymentCart.Amount <= 0)
		{
			await _toastNotification.ShowAsync("Invalid Payment Details", "Please ensure all payment details are correctly filled before adding to the cart.", ToastType.Error);
			return;
		}

		if (_paymentsCart.Where(p => p.Id != _selectedPaymentMethod.Id).Sum(p => p.Amount) + _selectedPaymentCart.Amount > _saleReturn.TotalAmount)
		{
			await _toastNotification.ShowAsync("Payment Amount Exceeds Total", $"The total payment amount cannot exceed the total amount of ₹{_saleReturn.TotalAmount:N2}.", ToastType.Error);
			return;
		}

		if (_selectedPaymentMethod.Name == "Credit" && _selectedLocation?.Id != 1)
		{
			await _toastNotification.ShowAsync("Invalid Payment Method for Location", "Credit payment method can only be used at the main location.", ToastType.Error);
			return;
		}

		var existingItem = _paymentsCart.FirstOrDefault(p => p.Id == _selectedPaymentMethod.Id);
		if (existingItem is not null)
			existingItem.Amount += _selectedPaymentCart.Amount;
		else
			_paymentsCart.Add(new()
			{
				Id = _selectedPaymentMethod.Id,
				Method = _selectedPaymentMethod.Name,
				Amount = _selectedPaymentCart.Amount
			});

		ApplyPaymentsToSaleReturn();

		_selectedPaymentMethod = new();
		_selectedPaymentCart = new();

		if (_paymentModeAutoComplete is not null) await _paymentModeAutoComplete.FocusAsync();
		await SaveTransactionFile(false, true);
	}

	private async Task EditSelectedPaymentCartItem()
	{
		if (_sfPaymentsCartGrid is null || _sfPaymentsCartGrid.SelectedRecords is null || _sfPaymentsCartGrid.SelectedRecords.Count == 0)
			return;

		var selectedCartItem = _sfPaymentsCartGrid.SelectedRecords.First();

		_selectedPaymentMethod = _paymentMethods.FirstOrDefault(pm => pm.Id == selectedCartItem.Id);
		if (_selectedPaymentMethod is null)
			return;

		_selectedPaymentCart = new()
		{
			Id = selectedCartItem.Id,
			Method = selectedCartItem.Method,
			Amount = selectedCartItem.Amount
		};

		if (_paymentModeAutoComplete is not null)
			await _paymentModeAutoComplete.FocusAsync();
		await RemoveSelectedPaymentCartItem();
	}

	private async Task RemoveSelectedPaymentCartItem()
	{
		if (_sfPaymentsCartGrid is null || _sfPaymentsCartGrid.SelectedRecords is null || _sfPaymentsCartGrid.SelectedRecords.Count == 0)
			return;

		var selectedCartItem = _sfPaymentsCartGrid.SelectedRecords.First();
		_paymentsCart.Remove(selectedCartItem);
		ApplyPaymentsToSaleReturn();
		await SaveTransactionFile(false, true);
	}
	#endregion

	#region Changed Events
	private async Task OnCompanyChanged(CompanyModel value)
	{
		if (value is null || value.Id == 0)
			return;

		_selectedCompany = value;
		_saleReturn.CompanyId = value.Id;
		await SaveTransactionFile();
	}

	private async Task OnLocationChanged(LocationModel value)
	{
		if (value is null || value.Id == 0)
			return;

		_selectedLocation = value;
		_saleReturn.LocationId = value.Id;

		if (_saleReturn.LocationId != 1)
		{
			_selectedParty = null;
			_saleReturn.PartyId = null;

			_cart.Clear();
			await _toastNotification.ShowAsync("Party Cleared", "The party has been cleared as the selected location is not the main location.", ToastType.Warning);
		}

		_selectedPaymentMethod = _selectedLocation?.Id == 1
			? _paymentMethods.FirstOrDefault(pm => pm.Name == "Credit") ?? _paymentMethods.FirstOrDefault()
			: _paymentMethods.FirstOrDefault();

		await LoadItems();
		await SaveTransactionFile();
	}

	private async Task OnTransactionDateChanged(DateTime value)
	{
		_saleReturn.TransactionDateTime = value;
		await LoadItems();
	}

	private async Task OnPartyChanged(LedgerModel value)
	{
		if (value is null || value.Id == 0)
		{
			_selectedParty = null;
			_saleReturn.PartyId = null;
			await SaveTransactionFile();
			return;
		}

		_selectedParty = value;
		_saleReturn.PartyId = value.Id;

		var location = _locations.FirstOrDefault(s => s.LedgerId == _selectedParty.Id);
		_saleReturn.DiscountPercent = location is not null ? location.Discount : 0;

		await LoadItems();
		await SaveTransactionFile();
	}

	private async Task OnCustomerNumberChanged(string value)
	{
		value ??= string.Empty;
		if (value.Any(c => !char.IsDigit(c)))
			value = new string([.. value.Where(char.IsDigit)]);

		if (string.IsNullOrWhiteSpace(value))
		{
			_selectedCustomer = new();
			_saleReturn.CustomerId = null;
			await SaveTransactionFile();
			return;
		}

		value = value.Trim();
		_selectedCustomer = await CustomerData.LoadCustomerByNumber(value);
		_selectedCustomer ??= new()
		{
			Id = 0,
			Name = "",
			Number = value
		};

		_saleReturn.CustomerId = _selectedCustomer.Id > 0 ? _selectedCustomer.Id : null;
		await SaveTransactionFile();
	}

	private async Task OnOtherChargesPercentChanged(decimal value)
	{
		_saleReturn.OtherChargesPercent = value;
		await SaveTransactionFile();
	}

	private async Task OnOtherChargesAmountChanged(decimal value)
	{
		_saleReturn.OtherChargesPercent = _saleReturn.TotalAfterTax > 0 ? value / _saleReturn.TotalAfterTax * 100 : 0;
		await SaveTransactionFile();
	}

	private async Task OnDiscountPercentChanged(decimal value)
	{
		_saleReturn.DiscountPercent = value;
		await SaveTransactionFile();
	}

	private async Task OnDiscountAmountChanged(decimal value)
	{
		var totalAfterOtherCharges = _saleReturn.TotalAfterTax + _saleReturn.OtherChargesAmount;
		_saleReturn.DiscountPercent = totalAfterOtherCharges > 0 ? value / totalAfterOtherCharges * 100 : 0;
		await SaveTransactionFile();
	}

	private async Task OnRoundOffAmountChanged(decimal value)
	{
		_saleReturn.RoundOffAmount = value;
		await SaveTransactionFile(false, true);
	}
	#endregion

	#region Cart
	private void OnItemChanged(ProductLocationOverviewModel value)
	{
		if (value is null || value.ProductId <= 0)
			return;

		_selectedProduct = value;

		var isSameState = _selectedParty is null || _selectedParty.StateUTId == _selectedCompany.StateUTId;
		var tax = _taxes.FirstOrDefault(s => s.Id == _selectedProduct.TaxId);

		_selectedCart.ItemId = _selectedProduct.ProductId;
		_selectedCart.ItemName = _selectedProduct.Name;
		_selectedCart.Quantity = 0;
		_selectedCart.Rate = _selectedProduct.Rate;
		_selectedCart.DiscountPercent = 0;
		_selectedCart.CGSTPercent = tax.CGST;
		_selectedCart.SGSTPercent = isSameState ? tax.SGST : 0;
		_selectedCart.IGSTPercent = isSameState ? 0 : tax.IGST;
		_selectedCart.InclusiveTax = tax.Inclusive;

		UpdateSelectedItemFinancialDetails();
	}

	private void OnItemQuantityChanged(decimal value)
	{
		_selectedCart.Quantity = value;
		UpdateSelectedItemFinancialDetails();
	}

	private void OnItemRateChanged(decimal value)
	{
		_selectedCart.Rate = value;
		UpdateSelectedItemFinancialDetails();
	}

	private void OnItemBaseTotalChanged(decimal value)
	{
		if (_selectedCart.Quantity > 0)
			_selectedCart.Rate = value / _selectedCart.Quantity;
		UpdateSelectedItemFinancialDetails();
	}

	private void OnItemDiscountPercentChanged(decimal value)
	{
		_selectedCart.DiscountPercent = value;
		UpdateSelectedItemFinancialDetails();
	}

	private void OnItemDiscountAmountChanged(decimal value)
	{
		_selectedCart.DiscountPercent = _selectedCart.BaseTotal > 0
			? value / _selectedCart.BaseTotal * 100 : 0;
		UpdateSelectedItemFinancialDetails();
	}

	private void OnItemCGSTPercentChanged(decimal value)
	{
		_selectedCart.CGSTPercent = value;
		UpdateSelectedItemFinancialDetails();
	}

	private void OnItemCGSTAmountChanged(decimal value)
	{
		_selectedCart.CGSTPercent = _selectedCart.InclusiveTax ?
			(_selectedCart.AfterDiscount - value) > 0 ? 100 * value / (_selectedCart.AfterDiscount - value) : 0 :
			_selectedCart.AfterDiscount > 0 ? value / _selectedCart.AfterDiscount * 100 : 0;
		UpdateSelectedItemFinancialDetails();
	}

	private void OnItemSGSTPercentChanged(decimal value)
	{
		_selectedCart.SGSTPercent = value;
		UpdateSelectedItemFinancialDetails();
	}

	private void OnItemSGSTAmountChanged(decimal value)
	{
		_selectedCart.SGSTPercent = _selectedCart.InclusiveTax ?
			(_selectedCart.AfterDiscount - value) > 0 ? 100 * value / (_selectedCart.AfterDiscount - value) : 0 :
			_selectedCart.AfterDiscount > 0 ? value / _selectedCart.AfterDiscount * 100 : 0;
		UpdateSelectedItemFinancialDetails();
	}

	private void OnItemIGSTPercentChanged(decimal value)
	{
		_selectedCart.IGSTPercent = value;
		UpdateSelectedItemFinancialDetails();
	}

	private void OnItemIGSTAmountChanged(decimal value)
	{
		_selectedCart.IGSTPercent = _selectedCart.InclusiveTax ?
			(_selectedCart.AfterDiscount - value) > 0 ? 100 * value / (_selectedCart.AfterDiscount - value) : 0 :
			_selectedCart.AfterDiscount > 0 ? value / _selectedCart.AfterDiscount * 100 : 0;
		UpdateSelectedItemFinancialDetails();
	}

	private void OnItemInclusiveTaxChanged(bool value)
	{
		_selectedCart.InclusiveTax = value;
		UpdateSelectedItemFinancialDetails();
	}

	private void UpdateSelectedItemFinancialDetails()
	{
		if (_selectedProduct is null)
			return;

		if (_selectedCart.Quantity < 0)
			_selectedCart.Quantity = 1;

		_selectedCart.ItemId = _selectedProduct.ProductId;
		_selectedCart.ItemName = _selectedProduct.Name;
		_selectedCart.BaseTotal = _selectedCart.Rate * _selectedCart.Quantity;
		_selectedCart.DiscountAmount = _selectedCart.BaseTotal * (_selectedCart.DiscountPercent / 100);
		_selectedCart.AfterDiscount = _selectedCart.BaseTotal - _selectedCart.DiscountAmount;

		if (_selectedCart.InclusiveTax)
		{
			_selectedCart.CGSTAmount = _selectedCart.AfterDiscount * (_selectedCart.CGSTPercent / (100 + _selectedCart.CGSTPercent));
			_selectedCart.SGSTAmount = _selectedCart.AfterDiscount * (_selectedCart.SGSTPercent / (100 + _selectedCart.SGSTPercent));
			_selectedCart.IGSTAmount = _selectedCart.AfterDiscount * (_selectedCart.IGSTPercent / (100 + _selectedCart.IGSTPercent));
			_selectedCart.TotalTaxAmount = _selectedCart.CGSTAmount + _selectedCart.SGSTAmount + _selectedCart.IGSTAmount;
			_selectedCart.Total = _selectedCart.AfterDiscount;
		}
		else
		{
			_selectedCart.CGSTAmount = _selectedCart.AfterDiscount * (_selectedCart.CGSTPercent / 100);
			_selectedCart.SGSTAmount = _selectedCart.AfterDiscount * (_selectedCart.SGSTPercent / 100);
			_selectedCart.IGSTAmount = _selectedCart.AfterDiscount * (_selectedCart.IGSTPercent / 100);
			_selectedCart.TotalTaxAmount = _selectedCart.CGSTAmount + _selectedCart.SGSTAmount + _selectedCart.IGSTAmount;
			_selectedCart.Total = _selectedCart.AfterDiscount + _selectedCart.TotalTaxAmount;
		}

		StateHasChanged();
	}

	private async Task AddItemToCart()
	{
		if (_selectedProduct is null || _selectedProduct.ProductId <= 0 || _selectedCart.Quantity <= 0 || _selectedCart.Rate < 0 || _selectedCart.DiscountPercent < 0 || _selectedCart.CGSTPercent < 0 || _selectedCart.SGSTPercent < 0 || _selectedCart.IGSTPercent < 0 || _selectedCart.Total < 0)
		{
			await _toastNotification.ShowAsync("Invalid Item Details", "Please ensure all item details are correctly filled before adding to the cart.", ToastType.Error);
			return;
		}

		int taxCount = 0;
		if (_selectedCart.CGSTPercent > 0) taxCount++;
		if (_selectedCart.SGSTPercent > 0) taxCount++;
		if (_selectedCart.IGSTPercent > 0) taxCount++;

		if (taxCount == 3)
		{
			await _toastNotification.ShowAsync("Invalid Tax Configuration", "All three taxes (CGST, SGST, IGST) cannot be applied together. Use either CGST+SGST or IGST only.", ToastType.Error);
			return;
		}

		UpdateSelectedItemFinancialDetails();

		var existingItem = _cart.FirstOrDefault(s => s.ItemId == _selectedCart.ItemId);
		if (existingItem is not null)
		{
			existingItem.Quantity += _selectedCart.Quantity;
			existingItem.Rate = _selectedCart.Rate;
			existingItem.DiscountPercent = _selectedCart.DiscountPercent;
			existingItem.CGSTPercent = _selectedCart.CGSTPercent;
			existingItem.SGSTPercent = _selectedCart.SGSTPercent;
			existingItem.IGSTPercent = _selectedCart.IGSTPercent;
			existingItem.InclusiveTax = _selectedCart.InclusiveTax;
		}
		else
			_cart.Add(new()
			{
				ItemId = _selectedCart.ItemId,
				ItemName = _selectedCart.ItemName,
				Quantity = _selectedCart.Quantity,
				Rate = _selectedCart.Rate,
				DiscountPercent = _selectedCart.DiscountPercent,
				CGSTPercent = _selectedCart.CGSTPercent,
				SGSTPercent = _selectedCart.SGSTPercent,
				IGSTPercent = _selectedCart.IGSTPercent,
				InclusiveTax = _selectedCart.InclusiveTax,
				Remarks = _selectedCart.Remarks
			});

		_selectedProduct = null;
		_selectedCart = new();

		await _itemAutoComplete.FocusAsync();
		await SaveTransactionFile();
	}

	private async Task EditSelectedCartItem()
	{
		if (_sfCartGrid is null || _sfCartGrid.SelectedRecords is null || _sfCartGrid.SelectedRecords.Count == 0)
			return;

		var selectedCartItem = _sfCartGrid.SelectedRecords.First();
		await EditCartItem(selectedCartItem);
	}

	private async Task EditCartItem(SaleReturnItemCartModel cartItem)
	{
		_selectedProduct = _products.FirstOrDefault(s => s.ProductId == cartItem.ItemId);

		if (_selectedProduct is null)
			return;

		_selectedCart = new()
		{
			ItemId = cartItem.ItemId,
			ItemName = cartItem.ItemName,
			Quantity = cartItem.Quantity,
			Rate = cartItem.Rate,
			DiscountPercent = cartItem.DiscountPercent,
			CGSTPercent = cartItem.CGSTPercent,
			SGSTPercent = cartItem.SGSTPercent,
			IGSTPercent = cartItem.IGSTPercent,
			InclusiveTax = cartItem.InclusiveTax,
			Remarks = cartItem.Remarks
		};

		await _itemAutoComplete.FocusAsync();
		UpdateSelectedItemFinancialDetails();
		await RemoveItemFromCart(cartItem);
	}

	private async Task RemoveSelectedCartItem()
	{
		if (_sfCartGrid is null || _sfCartGrid.SelectedRecords is null || _sfCartGrid.SelectedRecords.Count == 0)
			return;

		var selectedCartItem = _sfCartGrid.SelectedRecords.First();
		await RemoveItemFromCart(selectedCartItem);
	}

	private async Task RemoveItemFromCart(SaleReturnItemCartModel cartItem)
	{
		_cart.Remove(cartItem);
		await SaveTransactionFile();
	}
	#endregion

	#region Saving
	private void UpdateFinancialDetails(bool customRoundOff = false)
	{
		if (!_user.ChangeProductFinancial)
		{
			_saleReturn.DiscountPercent = 0;
			_saleReturn.OtherChargesPercent = 0;
			customRoundOff = false;
		}

		if (_user.LocationId != 1)
			_selectedLocation = _locations.FirstOrDefault(s => s.Id == _user.LocationId);

		if (_saleReturn.LocationId != 1)
			_selectedParty = null;

		foreach (var item in _cart.ToList())
		{
			if (item.Quantity <= 0)
			{
				_cart.Remove(item);
				continue;
			}

			if (_saleReturn.LocationId != 1)
				item.IGSTPercent = 0;

			if (!_user.ChangeProductFinancial)
			{
				var isSameState = _selectedParty is null || _selectedParty.StateUTId == _selectedCompany.StateUTId;
				var product = _products.FirstOrDefault(p => p.ProductId == item.ItemId);
				var tax = _taxes.FirstOrDefault(t => t.Id == product?.TaxId);

				item.Rate = product?.Rate ?? item.Rate;
				item.DiscountPercent = 0;
				item.CGSTPercent = tax?.CGST ?? 0;
				item.SGSTPercent = isSameState ? (tax?.SGST ?? 0) : 0;
				item.IGSTPercent = isSameState ? 0 : (tax?.IGST ?? 0);
				item.InclusiveTax = tax?.Inclusive ?? false;
			}

			item.BaseTotal = item.Rate * item.Quantity;
			item.DiscountAmount = item.BaseTotal * (item.DiscountPercent / 100);
			item.AfterDiscount = item.BaseTotal - item.DiscountAmount;

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

			var perUnitCost = item.Total / item.Quantity;
			var withOtherCharges = perUnitCost * (1 + _saleReturn.OtherChargesPercent / 100);
			item.NetRate = withOtherCharges * (1 - _saleReturn.DiscountPercent / 100);

			item.Remarks = string.IsNullOrWhiteSpace(item.Remarks) ? null : item.Remarks.Trim();
		}

		_cart = [.. _cart.OrderBy(s => s.ItemName)];

		_saleReturn.TotalItems = _cart.Count;
		_saleReturn.TotalQuantity = _cart.Sum(x => x.Quantity);
		_saleReturn.BaseTotal = _cart.Sum(x => x.BaseTotal);
		_saleReturn.ItemDiscountAmount = _cart.Sum(x => x.DiscountAmount);
		_saleReturn.TotalAfterItemDiscount = _cart.Sum(x => x.AfterDiscount);
		_saleReturn.TotalInclusiveTaxAmount = _cart.Where(x => x.InclusiveTax).Sum(x => x.TotalTaxAmount);
		_saleReturn.TotalExtraTaxAmount = _cart.Where(x => !x.InclusiveTax).Sum(x => x.TotalTaxAmount);
		_saleReturn.TotalAfterTax = _cart.Sum(x => x.Total);

		_saleReturn.OtherChargesAmount = _saleReturn.TotalAfterTax * _saleReturn.OtherChargesPercent / 100;
		var totalAfterOtherCharges = _saleReturn.TotalAfterTax + _saleReturn.OtherChargesAmount;

		_saleReturn.DiscountAmount = totalAfterOtherCharges * _saleReturn.DiscountPercent / 100;
		var totalAfterDiscount = totalAfterOtherCharges - _saleReturn.DiscountAmount;

		if (!customRoundOff)
			_saleReturn.RoundOffAmount = Math.Round(totalAfterDiscount) - totalAfterDiscount;

		_saleReturn.TotalAmount = totalAfterDiscount + _saleReturn.RoundOffAmount;

		_saleReturn.CompanyId = _selectedCompany.Id;
		_saleReturn.LocationId = _selectedLocation.Id;
		_saleReturn.PartyId = _selectedParty?.Id;
		_saleReturn.CustomerId = _selectedCustomer is { Id: > 0 } ? _selectedCustomer.Id : null;
		_saleReturn.CreatedBy = _user.Id;

		SyncPaymentsFromSaleReturn();
		ApplyPaymentsToSaleReturn();
	}

	private async Task PrepareSave()
	{
		if (_user.LocationId != 1)
		{
			var mainCompanyId = await SettingsData.LoadSettingsByKey(SettingsKeys.PrimaryCompanyLinkingId);
			_selectedCompany = _companies.FirstOrDefault(s => s.Id.ToString() == mainCompanyId.Value);
			_saleReturn.CompanyId = _selectedCompany.Id;
			_saleReturn.TransactionDateTime = await CommonData.LoadCurrentDateTime();
		}

		_selectedFinancialYear = await FinancialYearData.LoadFinancialYearByDateTime(_saleReturn.TransactionDateTime);
		if (_selectedFinancialYear is not null && !_selectedFinancialYear.Locked)
			_saleReturn.FinancialYearId = _selectedFinancialYear.Id;
		else
			await _toastNotification.ShowAsync("Invalid Transaction Date", "The selected transaction date does not fall within an active financial year.", ToastType.Error);

		if (Id is null)
			_saleReturn.TransactionNo = await GenerateCodes.GenerateSaleReturnTransactionNo(_saleReturn);

		var currentDateTime = await CommonData.LoadCurrentDateTime();
		_saleReturn.Status = true;
		_saleReturn.TransactionDateTime = DateOnly.FromDateTime(_saleReturn.TransactionDateTime).ToDateTime(new TimeOnly(currentDateTime.Hour, currentDateTime.Minute, currentDateTime.Second));
		_saleReturn.LastModifiedAt = currentDateTime;
		_saleReturn.CreatedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform();
		_saleReturn.LastModifiedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform();
		_saleReturn.CreatedBy = _user.Id;
		_saleReturn.LastModifiedBy = _user.Id;
	}

	private async Task SaveTransactionFile(bool prepareSave = false, bool customRoundOff = false)
	{
		if (_isProcessing || _isLoading)
			return;

		try
		{
			_isProcessing = true;

			UpdateFinancialDetails(customRoundOff);
			if (prepareSave) await PrepareSave();

			if (_cart.Count == 0 || _saleReturn.Id > 0)
			{
				await DeleteLocalFiles();
				return;
			}

			await DataStorageService.LocalSaveAsync(StorageFileNames.SaleReturnDataFileName, JsonSerializer.Serialize(_saleReturn));
			await DataStorageService.LocalSaveAsync(StorageFileNames.SaleReturnCartDataFileName, JsonSerializer.Serialize(_cart));
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("An Error Occurred While Saving Transaction Data", ex.Message, ToastType.Error);
		}
		finally
		{
			if (_sfCartGrid is not null) await _sfCartGrid.Refresh();
			if (_sfPaymentsCartGrid is not null) await _sfPaymentsCartGrid.Refresh();

			_isProcessing = false;
			StateHasChanged();
		}
	}

	private async Task SaveTransaction(bool savePDF = false, bool saveExcel = false)
	{
		if (_isProcessing || _isLoading)
			return;

		try
		{
			await SaveTransactionFile(true, true);
			_isProcessing = true;
			StateHasChanged();

			await _toastNotification.ShowAsync("Processing Transaction", "Please wait while the transaction is being saved...", ToastType.Info);

			var saleReturnDetails = SaleReturnData.ConvertCartToDetails(_cart);
			_saleReturn.Id = await SaleReturnData.SaveTransaction(_saleReturn, saleReturnDetails, _selectedCustomer);
			_saleReturn = await CommonData.LoadTableDataById<SaleReturnModel>(StoreNames.SaleReturn, _saleReturn.Id);

			if (savePDF) await ExportSelectedTransaction(false, true);
			if (saveExcel) await ExportSelectedTransaction(true, true);

			await _toastNotification.ShowAsync("Save Transaction", "Transaction saved successfully.", ToastType.Success);

			if (Id.HasValue && Id.Value > 0)
				await AuthenticationService.CloseWindowOrTab(FormFactor, JSRuntime);
			await ResetPage();
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error While Saving Transaction", ex.Message, ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
		}
	}
	#endregion

	#region Exporting
	private async Task ExportSelectedTransaction(bool isExcel = false, bool force = false)
	{
		if (_saleReturn.Id <= 0 || (_isProcessing && !force))
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();
			await _toastNotification.ShowAsync("Processing", "Generating the Export...", ToastType.Info);

			var decodeTransactionNo = await DecodeCode.DecodeTransactionNo(_saleReturn.TransactionNo, !isExcel, isExcel, CodeType.SaleReturn);
			await SaveAndViewService.SaveAndView(isExcel ? decodeTransactionNo.ExcelStream.fileName : decodeTransactionNo.PDFStream.fileName,
				isExcel ? decodeTransactionNo.ExcelStream.stream : decodeTransactionNo.PDFStream.stream);

			await _toastNotification.ShowAsync("Exported", "The export has been downloaded successfully.", ToastType.Success);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error While Exporting", ex.Message, ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
			StateHasChanged();
		}
	}
	#endregion

	#region Utilities
	private async Task OnCartGridContextMenuItemClicked(ContextMenuClickEventArgs<SaleReturnItemCartModel> args)
	{
		switch (args.Item.Id)
		{
			case "EditCart": await EditSelectedCartItem(); break;
			case "DeleteCart": await RemoveSelectedCartItem(); break;
		}
	}

	private async Task OnPaymentsCartGridContextMenuItemClicked(ContextMenuClickEventArgs<PaymentItem> args)
	{
		switch (args.Item.Id)
		{
			case "EditCart": await EditSelectedPaymentCartItem(); break;
			case "DeleteCart": await RemoveSelectedPaymentCartItem(); break;
		}
	}

	private async Task DeleteLocalFiles()
	{
		await DataStorageService.LocalRemove(StorageFileNames.SaleReturnDataFileName);
		await DataStorageService.LocalRemove(StorageFileNames.SaleReturnCartDataFileName);
	}

	private async Task ResetPage()
	{
		await DeleteLocalFiles();
		PageRefresh.Request();
	}
	#endregion
}
