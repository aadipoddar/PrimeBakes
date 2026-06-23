using Microsoft.AspNetCore.Components;

using PrimeBakes.Library.Accounts.Masters.Data;
using PrimeBakes.Library.Accounts.Masters.Models;
using PrimeBakes.Library.Operations.Location;
using PrimeBakes.Library.Operations.Settings;
using PrimeBakes.Library.Operations.User;
using PrimeBakes.Library.Restaurant.Bill.Data;
using PrimeBakes.Library.Restaurant.Bill.Exports;
using PrimeBakes.Library.Restaurant.Bill.Models;
using PrimeBakes.Library.Restaurant.Dining.Models;
using PrimeBakes.Library.Store.Customer.Data;
using PrimeBakes.Library.Store.Customer.Models;
using PrimeBakes.Library.Store.PaymentMode;
using PrimeBakes.Library.Store.Product.Data;
using PrimeBakes.Library.Store.Product.Models;
using PrimeBakes.Shared.Components.Dialog;
using PrimeBakes.Shared.Components.Input;

using Syncfusion.Blazor.Grids;

using System.Text.Json;

namespace PrimeBakes.Shared.Pages.Restaurant.Bill;

public partial class BillPage
{
	[Parameter] public int? Id { get; set; }
	[Parameter] public int? DiningTableId { get; set; }

	private UserModel _user;

	private bool _isLoading = true;
	private bool _isProcessing = false;

	private CompanyModel _selectedCompany = new();
	private LocationModel _selectedLocation = new();
	private DiningTableModel _selectedDiningTable = new();
	private CustomerModel _selectedCustomer = new();
	private FinancialYearModel _selectedFinancialYear = new();
	private ProductLocationOverviewModel _selectedProduct;
	private BillItemCartModel _selectedCart = new();
	private BillModel _bill = new();

	private List<CompanyModel> _companies = [];
	private List<LocationModel> _locations = [];
	private List<DiningTableModel> _diningTables = [];
	private List<ProductLocationOverviewModel> _products = [];
	private List<TaxModel> _taxes = [];
	private List<BillItemCartModel> _cart = [];
	private readonly List<ContextMenuItemModel> _cartGridContextMenuItems =
	[
		new() { Text = "Edit (Insert)", Id = "EditCart", IconCss = "e-icons e-edit", Target = ".e-content" },
		new() { Text = "Delete (Del)", Id = "DeleteCart", IconCss = "e-icons e-trash", Target = ".e-content" }
	];

	private readonly List<PaymentItem> _paymentsCart = [];
	private readonly List<PaymentModeModel> _paymentMethods = PaymentModeData.GetPaymentModes();
	private PaymentModeModel _selectedPaymentMethod = new();
	private PaymentItem _selectedPaymentCart = new();
	private decimal _remainingAmount => _bill.TotalAmount - _paymentsCart.Sum(p => p.Amount);

	private readonly List<ContextMenuItemModel> _paymentsCartGridContextMenuItems =
	[
		new() { Text = "Edit (Insert)", Id = "EditCart", IconCss = "e-icons e-edit", Target = ".e-content" },
		new() { Text = "Delete (Del)", Id = "DeleteCart", IconCss = "e-icons e-trash", Target = ".e-content" }
	];

	private CustomAutoComplete<CompanyModel> _firstFocus;
	private CustomAutoComplete<ProductLocationOverviewModel> _itemAutoComplete;
	private CustomNumericField<decimal> _discountPercentField;
	private CustomAutoComplete<PaymentModeModel> _paymentModeAutoComplete;
	private SfGrid<BillItemCartModel> _sfCartGrid;
	private SfGrid<PaymentItem> _sfPaymentsCartGrid;

	private ToastNotification _toastNotification;

	private bool NeedsSave => _bill.Id == 0 || _bill.Running;

	#region Load Data
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		try
		{
			_user = await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, [UserRoles.Restaurant]);
			await InitializePage();
		}
		catch { await ResetPage(); }
	}

	private async Task InitializePage()
	{
		await LoadData();
		await ResolveTransaction();
		await LoadSelections();
		await LoadDiningTables();
		await LoadItems();
		await ResolveCart();

		_isLoading = false;
		StateHasChanged();

		await SaveTransactionFile(true);

		if (_firstFocus is not null) await _firstFocus.FocusAsync();
	}

	private async Task LoadData()
	{
		_companies = await CommonData.LoadTableDataByStatus<CompanyModel>(AccountNames.Company);
		_locations = await CommonData.LoadTableDataByStatus<LocationModel>(OperationNames.Location);

		_companies = [.. _companies.OrderBy(s => s.Name)];
		_locations = [.. _locations.OrderBy(s => s.Name)];

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

			if (await ResolveTableBill())
				return;

			if (await TryRestoreFromLocalStorage())
				return;

			await CreateNewTransaction(null);
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

		_bill = await CommonData.LoadTableDataById<BillModel>(RestaurantNames.Bill, Id.Value);
		if (_bill is null || _bill.Id == 0)
		{
			await _toastNotification.ShowAsync("Transaction Not Found", "The requested transaction could not be found.", ToastType.Error);
			await ResetPage();
		}

		if (_user.LocationId != 1 && _bill.LocationId != _user.LocationId)
		{
			await _toastNotification.ShowAsync("Access Denied", "You do not have permission to view this bill.", ToastType.Error);
			await ResetPage();
		}

		return true;
	}

	private async Task<bool> ResolveTableBill()
	{
		if (!DiningTableId.HasValue)
			return false;

		var diningTable = await CommonData.LoadTableDataById<DiningTableModel>(RestaurantNames.DiningTable, DiningTableId.Value);
		if (diningTable is null)
		{
			await _toastNotification.ShowAsync("Dining Table Not Found", "The selected dining table could not be found.", ToastType.Error);
			await ResetPage();
		}

		var diningArea = await CommonData.LoadTableDataById<DiningAreaModel>(RestaurantNames.DiningArea, diningTable.DiningAreaId);
		if (_user.LocationId != 1 && diningArea.LocationId != _user.LocationId)
		{
			await _toastNotification.ShowAsync("Access Denied", "This dining table does not belong to your location.", ToastType.Error);
			await ResetPage();
		}

		// If this table already has a running bill, redirect to it.
		var runningBills = await BillData.LoadRunningBillByLocationId(diningArea.LocationId);
		var existingBill = runningBills.FirstOrDefault(b => b.DiningTableId == DiningTableId.Value && b.Running);
		if (existingBill is not null)
		{
			NavigationManager.NavigateTo($"{RestaurantRouteNames.Bill}/{existingBill.Id}");
			PageRefresh.Request();
			return false;
		}

		await CreateNewTransaction(DiningTableId.Value);
		_bill.LocationId = diningArea.LocationId;
		return true;
	}

	private async Task<bool> TryRestoreFromLocalStorage()
	{
		if (!await DataStorageService.LocalExists(StorageFileNames.BillDataFileName))
			return false;

		try
		{
			_bill = JsonSerializer.Deserialize<BillModel>(await DataStorageService.LocalGetAsync(StorageFileNames.BillDataFileName));
			return _bill is not null && _bill.Id == 0; // Only restore unsaved new bills.
		}
		catch
		{
			await DeleteLocalFiles();
			return false;
		}
	}

	private async Task CreateNewTransaction(int? diningTableId)
	{
		var currentDateTime = await CommonData.LoadCurrentDateTime();
		var financialYear = await FinancialYearData.LoadFinancialYearByDateTime(currentDateTime);

		_bill = new()
		{
			Id = 0,
			TransactionNo = string.Empty,
			CompanyId = _selectedCompany.Id,
			LocationId = _user.LocationId,
			DiningTableId = diningTableId ?? 0,
			CustomerId = null,
			TransactionDateTime = currentDateTime,
			FinancialYearId = financialYear?.Id ?? 0,
			TotalPeople = 1,
			CreatedBy = _user.Id,
			TotalQuantity = 0,
			TotalItems = 0,
			BaseTotal = 0,
			ItemDiscountAmount = 0,
			TotalAfterItemDiscount = 0,
			TotalInclusiveTaxAmount = 0,
			TotalExtraTaxAmount = 0,
			TotalAfterTax = 0,
			DiscountPercent = 0,
			DiscountAmount = 0,
			ServiceChargePercent = 0,
			ServiceChargeAmount = 0,
			RoundOffAmount = 0,
			TotalAmount = 0,
			Card = 0,
			Cash = 0,
			Credit = 0,
			UPI = 0,
			Remarks = null,
			CreatedAt = DateTime.Now,
			CreatedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform(),
			Running = true,
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
			? _locations.FirstOrDefault(s => s.Id == _bill.LocationId) ?? _locations.FirstOrDefault()
			: _locations.FirstOrDefault(s => s.Id == _user.LocationId);
		_bill.LocationId = _selectedLocation?.Id ?? 0;

		var mainCompanyId = await SettingsData.LoadSettingsByKey(SettingsKeys.PrimaryCompanyLinkingId);
		_selectedCompany = _user.LocationId == 1 && _bill.CompanyId > 0
			? _companies.FirstOrDefault(s => s.Id == _bill.CompanyId) ?? _companies.FirstOrDefault(s => s.Id.ToString() == mainCompanyId.Value)
			: _companies.FirstOrDefault(s => s.Id.ToString() == mainCompanyId.Value);
		_bill.CompanyId = _selectedCompany?.Id ?? 0;

		if (_bill.CustomerId is not null && _bill.CustomerId > 0)
			_selectedCustomer = await CommonData.LoadTableDataById<CustomerModel>(StoreNames.Customer, _bill.CustomerId.Value);
		else
		{
			_selectedCustomer = new();
			_bill.CustomerId = null;
		}

		_selectedFinancialYear = await CommonData.LoadTableDataById<FinancialYearModel>(AccountNames.FinancialYear, _bill.FinancialYearId);
		SyncPaymentsFromBill();
	}

	private async Task LoadDiningTables()
	{
		var diningAreas = await CommonData.LoadTableDataByStatus<DiningAreaModel>(RestaurantNames.DiningArea);
		diningAreas = [.. diningAreas.Where(d => d.LocationId == _bill.LocationId)];

		_diningTables = await CommonData.LoadTableDataByStatus<DiningTableModel>(RestaurantNames.DiningTable);
		_diningTables = [.. _diningTables.Where(dt => diningAreas.Any(da => da.Id == dt.DiningAreaId)).OrderBy(s => s.Name)];

		if (_diningTables.Count == 0)
			await _toastNotification.ShowAsync("No Dining Tables Found", "No dining tables were found for the selected location. Please create dining tables to proceed.", ToastType.Warning);

		_selectedDiningTable = _bill.DiningTableId > 0
			? _diningTables.FirstOrDefault(s => s.Id == _bill.DiningTableId) ?? new()
			: new();

		_bill.DiningTableId = _selectedDiningTable.Id;
	}

	private async Task LoadItems()
	{
		_products = await ProductLocationData.LoadProductLocationOverviewByProductLocationDate(null, _bill.LocationId, DateOnly.FromDateTime(_bill.TransactionDateTime));
		_taxes = await CommonData.LoadTableDataByStatus<TaxModel>(StoreNames.Tax);

		_products = [.. _products.OrderBy(s => s.Name)];
	}

	private async Task ResolveCart()
	{
		try
		{
			_cart.Clear();

			if (await LoadExistingCart())
				return;

			if (await DataStorageService.LocalExists(StorageFileNames.BillCartDataFileName))
				_cart = JsonSerializer.Deserialize<List<BillItemCartModel>>(await DataStorageService.LocalGetAsync(StorageFileNames.BillCartDataFileName)) ?? [];
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("An Error Occurred While Loading Existing Cart", ex.Message, ToastType.Error);
			await ResetPage();
		}
	}

	private async Task<bool> LoadExistingCart()
	{
		if (_bill.Id <= 0)
			return false;

		var existingCart = await CommonData.LoadTableDataByMasterId<BillDetailModel>(RestaurantNames.BillDetail, _bill.Id);

		foreach (var item in existingCart)
		{
			var product = _products.FirstOrDefault(s => s.ProductId == item.ProductId);
			if (product is null)
			{
				var missing = await CommonData.LoadTableDataById<ProductModel>(StoreNames.Product, item.ProductId);
				await _toastNotification.ShowAsync("Item Not Found", $"The item {missing?.Name} (ID: {item.ProductId}) was not found in available items. It may have been deleted.", ToastType.Error);
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
				KOTPrint = item.KOTPrint,
				Remarks = item.Remarks
			});
		}

		return true;
	}
	#endregion

	#region Payment
	private void SyncPaymentsFromBill()
	{
		_paymentsCart.Clear();

		AddPaymentFromBill("Cash", _bill.Cash);
		AddPaymentFromBill("Card", _bill.Card);
		AddPaymentFromBill("UPI", _bill.UPI);
		AddPaymentFromBill("Credit", _bill.Credit);

		_selectedPaymentMethod = _paymentMethods.FirstOrDefault();
		_selectedPaymentCart = new() { Amount = Math.Max(0, _remainingAmount) };
	}

	private void AddPaymentFromBill(string modeName, decimal amount)
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

	private void ApplyPaymentsToBill()
	{
		_bill.Cash = _paymentsCart.FirstOrDefault(p => p.Method == "Cash")?.Amount ?? 0;
		_bill.Card = _paymentsCart.FirstOrDefault(p => p.Method == "Card")?.Amount ?? 0;
		_bill.UPI = _paymentsCart.FirstOrDefault(p => p.Method == "UPI")?.Amount ?? 0;
		_bill.Credit = _paymentsCart.FirstOrDefault(p => p.Method == "Credit")?.Amount ?? 0;
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
		_selectedPaymentCart.Amount = Math.Max(0, _bill.TotalAmount - _paymentsCart.Where(p => p.Id != value.Id).Sum(p => p.Amount));
	}

	private async Task AddPaymentToCart()
	{
		if (_selectedPaymentMethod is null || _selectedPaymentMethod.Id <= 0 || _selectedPaymentCart.Amount <= 0)
		{
			await _toastNotification.ShowAsync("Invalid Payment Details", "Please ensure all payment details are correctly filled before adding to the cart.", ToastType.Error);
			return;
		}

		if (_paymentsCart.Where(p => p.Id != _selectedPaymentMethod.Id).Sum(p => p.Amount) + _selectedPaymentCart.Amount > _bill.TotalAmount)
		{
			await _toastNotification.ShowAsync("Payment Amount Exceeds Total", $"The total payment amount cannot exceed the total amount of ₹{_bill.TotalAmount:N2}.", ToastType.Error);
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

		ApplyPaymentsToBill();

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
		ApplyPaymentsToBill();
		await SaveTransactionFile(false, true);
	}
	#endregion

	#region Changed Events
	private async Task OnCompanyChanged(CompanyModel value)
	{
		if (value is null || value.Id == 0)
			return;

		_selectedCompany = value;
		_bill.CompanyId = value.Id;
		await SaveTransactionFile();
	}

	private async Task OnLocationChanged(LocationModel value)
	{
		if (value is null || value.Id == 0)
			return;

		_selectedLocation = value;
		_bill.LocationId = value.Id;

		await LoadDiningTables();
		await LoadItems();
		await SaveTransactionFile();
	}

	private async Task OnTransactionDateChanged(DateTime value)
	{
		_bill.TransactionDateTime = value;
		await LoadItems();
	}

	private async Task OnDiningTableChanged(DiningTableModel value)
	{
		if (value is null || value.Id == 0)
			return;

		var diningArea = await CommonData.LoadTableDataById<DiningAreaModel>(RestaurantNames.DiningArea, value.DiningAreaId);
		if (diningArea.LocationId != _bill.LocationId)
		{
			await _toastNotification.ShowAsync("Invalid Dining Table", "The selected dining table does not belong to the selected location.", ToastType.Error);
			return;
		}

		var runningBills = await BillData.LoadRunningBillByLocationId(diningArea.LocationId);
		var existingRunningBill = runningBills.FirstOrDefault(b => b.DiningTableId == value.Id);
		if (existingRunningBill is not null)
		{
			NavigationManager.NavigateTo($"{RestaurantRouteNames.Bill}/{existingRunningBill.Id}");
			PageRefresh.Request();
			return;
		}

		_selectedDiningTable = value;
		_bill.DiningTableId = value.Id;

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
			_bill.CustomerId = null;
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

		_bill.CustomerId = _selectedCustomer.Id > 0 ? _selectedCustomer.Id : null;
		await SaveTransactionFile();
	}

	private async Task OnTotalPeopleChanged(int value)
	{
		_bill.TotalPeople = value < 1 ? 1 : value;
		await SaveTransactionFile();
	}

	private async Task OnDiscountPercentChanged(decimal value)
	{
		_bill.DiscountPercent = value;
		await SaveTransactionFile();
	}

	private async Task OnServiceChargePercentChanged(decimal value)
	{
		_bill.ServiceChargePercent = value;
		await SaveTransactionFile();
	}

	private async Task OnRoundOffAmountChanged(decimal value)
	{
		_bill.RoundOffAmount = value;
		await SaveTransactionFile(true);
	}
	#endregion

	#region Cart
	private void OnItemChanged(ProductLocationOverviewModel value)
	{
		if (value is null || value.ProductId <= 0)
			return;

		_selectedProduct = value;

		var tax = _taxes.FirstOrDefault(s => s.Id == _selectedProduct.TaxId);

		_selectedCart.ItemId = _selectedProduct.ProductId;
		_selectedCart.ItemName = _selectedProduct.Name;
		_selectedCart.Quantity = 0;
		_selectedCart.Rate = _selectedProduct.Rate;
		_selectedCart.DiscountPercent = 0;
		_selectedCart.CGSTPercent = tax?.CGST ?? 0;
		_selectedCart.SGSTPercent = tax?.SGST ?? 0;
		_selectedCart.IGSTPercent = 0;
		_selectedCart.InclusiveTax = tax?.Inclusive ?? false;
		_selectedCart.KOTPrint = true;

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

	private void OnItemDiscountPercentChanged(decimal value)
	{
		_selectedCart.DiscountPercent = value;
		UpdateSelectedItemFinancialDetails();
	}

	private void OnItemCGSTPercentChanged(decimal value)
	{
		_selectedCart.CGSTPercent = value;
		UpdateSelectedItemFinancialDetails();
	}

	private void OnItemSGSTPercentChanged(decimal value)
	{
		_selectedCart.SGSTPercent = value;
		UpdateSelectedItemFinancialDetails();
	}

	private void OnItemIGSTPercentChanged(decimal value)
	{
		_selectedCart.IGSTPercent = value;
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
		if (_selectedProduct is null || _selectedProduct.ProductId <= 0
			|| _selectedCart.Quantity <= 0 || _selectedCart.Rate < 0
			|| _selectedCart.DiscountPercent < 0 || _selectedCart.Total < 0)
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

		var existingItem = _cart.FirstOrDefault(s => s.ItemId == _selectedCart.ItemId && s.KOTPrint == _selectedCart.KOTPrint);
		if (existingItem is not null)
		{
			existingItem.Quantity += _selectedCart.Quantity;
			existingItem.Rate = _selectedCart.Rate;
			existingItem.DiscountPercent = _selectedCart.DiscountPercent;
			existingItem.CGSTPercent = _selectedCart.CGSTPercent;
			existingItem.SGSTPercent = _selectedCart.SGSTPercent;
			existingItem.IGSTPercent = _selectedCart.IGSTPercent;
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
				KOTPrint = _selectedCart.KOTPrint,
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

		await EditCartItem(_sfCartGrid.SelectedRecords.First());
	}

	private async Task EditCartItem(BillItemCartModel cartItem)
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
			KOTPrint = cartItem.KOTPrint,
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

		await RemoveItemFromCart(_sfCartGrid.SelectedRecords.First());
	}

	private async Task RemoveItemFromCart(BillItemCartModel cartItem)
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
			_bill.DiscountPercent = 0;
			_bill.ServiceChargePercent = 0;
			customRoundOff = false;
		}

		if (_user.LocationId != 1)
			_selectedLocation = _locations.FirstOrDefault(s => s.Id == _user.LocationId);

		foreach (var item in _cart.ToList())
		{
			if (item.Quantity <= 0)
			{
				_cart.Remove(item);
				continue;
			}

			if (_bill.LocationId != 1)
				item.IGSTPercent = 0;

			if (!_user.ChangeProductFinancial)
			{
				item.Rate = _products.FirstOrDefault(p => p.ProductId == item.ItemId)?.Rate ?? item.Rate;
				item.DiscountPercent = 0;
				item.CGSTPercent = _taxes.FirstOrDefault(t => t.Id == _products.FirstOrDefault(p => p.ProductId == item.ItemId)?.TaxId)?.CGST ?? 0;
				item.SGSTPercent = _taxes.FirstOrDefault(t => t.Id == _products.FirstOrDefault(p => p.ProductId == item.ItemId)?.TaxId)?.SGST ?? 0;
				item.IGSTPercent = 0;
				item.InclusiveTax = _taxes.FirstOrDefault(t => t.Id == _products.FirstOrDefault(p => p.ProductId == item.ItemId)?.TaxId)?.Inclusive ?? false;
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
			item.NetRate = perUnitCost * (1 - _bill.DiscountPercent / 100);

			item.Remarks = string.IsNullOrWhiteSpace(item.Remarks) ? null : item.Remarks.Trim();
		}

		_bill.TotalItems = _cart.Count;
		_bill.TotalQuantity = _cart.Sum(x => x.Quantity);
		_bill.BaseTotal = _cart.Sum(x => x.BaseTotal);
		_bill.ItemDiscountAmount = _cart.Sum(x => x.DiscountAmount);
		_bill.TotalAfterItemDiscount = _cart.Sum(x => x.AfterDiscount);
		_bill.TotalInclusiveTaxAmount = _cart.Where(x => x.InclusiveTax).Sum(x => x.TotalTaxAmount);
		_bill.TotalExtraTaxAmount = _cart.Where(x => !x.InclusiveTax).Sum(x => x.TotalTaxAmount);
		_bill.TotalAfterTax = _cart.Sum(x => x.Total);

		_bill.DiscountAmount = _bill.TotalAfterTax * _bill.DiscountPercent / 100;
		var totalAfterDiscount = _bill.TotalAfterTax - _bill.DiscountAmount;

		_bill.ServiceChargeAmount = totalAfterDiscount * _bill.ServiceChargePercent / 100;
		var totalAfterServiceCharge = totalAfterDiscount + _bill.ServiceChargeAmount;

		if (!customRoundOff)
			_bill.RoundOffAmount = Math.Round(totalAfterServiceCharge) - totalAfterServiceCharge;

		_bill.TotalAmount = totalAfterServiceCharge + _bill.RoundOffAmount;

		_bill.CompanyId = _selectedCompany.Id;
		_bill.LocationId = _selectedLocation.Id;
		_bill.DiningTableId = _selectedDiningTable?.Id ?? 0;
		_bill.CustomerId = _selectedCustomer is { Id: > 0 } ? _selectedCustomer.Id : null;

		SyncPaymentsFromBill();
		ApplyPaymentsToBill();

		// A bill is "running" until it is fully paid.
		_bill.Running = _bill.Cash + _bill.Card + _bill.UPI + _bill.Credit != _bill.TotalAmount;
	}

	private async Task PrepareSave()
	{
		if (_user.LocationId != 1)
		{
			var mainCompanyId = await SettingsData.LoadSettingsByKey(SettingsKeys.PrimaryCompanyLinkingId);
			_selectedCompany = _companies.FirstOrDefault(s => s.Id.ToString() == mainCompanyId.Value);
			_bill.CompanyId = _selectedCompany.Id;
			_bill.TransactionDateTime = await CommonData.LoadCurrentDateTime();
		}

		_selectedFinancialYear = await FinancialYearData.LoadFinancialYearByDateTime(_bill.TransactionDateTime);
		if (_selectedFinancialYear is not null && !_selectedFinancialYear.Locked)
			_bill.FinancialYearId = _selectedFinancialYear.Id;
		else
			await _toastNotification.ShowAsync("Invalid Transaction Date", "The selected transaction date does not fall within an active financial year.", ToastType.Error);

		if (Id is null)
			_bill.TransactionNo = await GenerateCodes.GenerateBillTransactionNo(_bill);

		var currentDateTime = await CommonData.LoadCurrentDateTime();
		_bill.Status = true;
		_bill.TransactionDateTime = DateOnly.FromDateTime(_bill.TransactionDateTime).ToDateTime(new TimeOnly(currentDateTime.Hour, currentDateTime.Minute, currentDateTime.Second));
		_bill.LastModifiedAt = currentDateTime;
		_bill.CreatedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform();
		_bill.LastModifiedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform();
		_bill.CreatedBy = _user.Id;
		_bill.LastModifiedBy = _user.Id;
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

			if (_cart.Count == 0 || _bill.Id > 0)
			{
				await DeleteLocalFiles();
				return;
			}

			await DataStorageService.LocalSaveAsync(StorageFileNames.BillDataFileName, JsonSerializer.Serialize(_bill));
			await DataStorageService.LocalSaveAsync(StorageFileNames.BillCartDataFileName, JsonSerializer.Serialize(_cart));
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

	private async Task SaveTransaction(bool saveThermal = false, bool savePDF = false, bool saveExcel = false)
	{
		if (_isProcessing || _isLoading)
			return;

		try
		{
			await SaveTransactionFile(true, true);
			_isProcessing = true;
			StateHasChanged();

			await _toastNotification.ShowAsync("Processing Transaction", "Please wait while the transaction is being saved...", ToastType.Info);

			_bill.Id = await BillData.SaveTransaction(_bill, BillData.ConvertCartToDetails(_cart), _selectedCustomer);
			_bill = await CommonData.LoadTableDataById<BillModel>(RestaurantNames.Bill, _bill.Id);

			if (saveThermal) await PrintThermalInvoice(true);
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

	#region KOT
	private async Task PrintKOT()
	{
		if (!NeedsSave)
		{
			await _toastNotification.ShowAsync("Transaction Already Saved", "The transaction has already been saved. KOT has been printed if there were items marked for KOT print.", ToastType.Info);
			return;
		}

		if (_isProcessing || _isLoading)
			return;

		try
		{
			await SaveTransactionFile(true, true);
			_isProcessing = true;
			StateHasChanged();

			await _toastNotification.ShowAsync("Processing Transaction", "Please wait while the transaction is being saved...", ToastType.Info);

			if (!_bill.Running)
				foreach (var item in _cart)
					item.KOTPrint = false;

			_bill.Id = await BillData.SaveTransaction(_bill, BillData.ConvertCartToDetails(_cart), _selectedCustomer);
			_bill = await CommonData.LoadTableDataById<BillModel>(RestaurantNames.Bill, _bill.Id);

			await HandleKOTPrint();

			await _toastNotification.ShowAsync("KOT Printed", "Kitchen order ticket sent to printer.", ToastType.Success);

			await DeleteLocalFiles();
			NavigationManager.NavigateTo($"{RestaurantRouteNames.Bill}/{_bill.Id}", true);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error Printing KOT", ex.Message, ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
		}
	}

	private async Task HandleKOTPrint()
	{
		var kotCategoryItems = await BillData.KOTCategoryItemsFromBill(_bill.Id);
		if (kotCategoryItems.Count == 0)
		{
			await _toastNotification.ShowAsync("No KOT Items", "There are no items marked for KOT printing.", ToastType.Info);
			return;
		}

		foreach (var kotCategoryId in kotCategoryItems.Keys)
			await ThermalPrintDispatcher.PrintAsync(
				async () => await KOTThermalPrint.GenerateThermalBill(_bill.Id, kotCategoryId, kotCategoryItems[kotCategoryId]),
				async () => await KOTThermalPrint.GenerateThermalBillPng(_bill.Id, kotCategoryId, kotCategoryItems[kotCategoryId]));

		await BillData.MarkKOTAsPrinted(_bill.Id);
	}
	#endregion

	#region Exporting
	private async Task ExportSelectedTransaction(bool isExcel = false, bool force = false)
	{
		if (_bill.Id <= 0 || _bill.Running || (_isProcessing && !force))
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();
			await _toastNotification.ShowAsync("Processing", "Generating the Export...", ToastType.Info);

			var decodeTransactionNo = await DecodeCode.DecodeTransactionNo(_bill.TransactionNo, !isExcel, isExcel, CodeType.Bill);
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

	private async Task PrintThermalInvoice(bool force = false)
	{
		if (_bill.Id <= 0 || (_isProcessing && !force))
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();
			await _toastNotification.ShowAsync("Processing", "Generating Thermal invoice...", ToastType.Info);

			await ThermalPrintDispatcher.PrintAsync(
				() => BillThermalPrint.GenerateThermalBill(_bill.Id),
				() => BillThermalPrint.GenerateThermalBillPng(_bill.Id));

			await _toastNotification.ShowAsync("Print Sent", "Thermal invoice sent to printer.", ToastType.Success);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error While Printing", ex.Message, ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
			StateHasChanged();
		}
	}
	#endregion

	#region Utilities
	private async Task OnCartGridContextMenuItemClicked(ContextMenuClickEventArgs<BillItemCartModel> args)
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
		await DataStorageService.LocalRemove(StorageFileNames.BillDataFileName);
		await DataStorageService.LocalRemove(StorageFileNames.BillCartDataFileName);
	}

	private async Task ResetPage()
	{
		await DeleteLocalFiles();
		NavigationManager.NavigateTo(RestaurantRouteNames.DiningDashboard);
	}
	#endregion
}
