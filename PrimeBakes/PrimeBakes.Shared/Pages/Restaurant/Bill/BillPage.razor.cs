using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

using PrimeBakes.Shared.Components.Dialog;

using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Operations;
using PrimeBakesLibrary.Data.Restaurant.Bill;
using PrimeBakesLibrary.Data.Store.Masters;
using PrimeBakesLibrary.Data.Store.Product;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Exporting.Restaurant.Bill;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Operations;
using PrimeBakesLibrary.Models.Restuarant.Bill;
using PrimeBakesLibrary.Models.Restuarant.Dining;
using PrimeBakesLibrary.Models.Store.Masters;
using PrimeBakesLibrary.Models.Store.Product;

using Syncfusion.Blazor.DropDowns;
using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Inputs;

namespace PrimeBakes.Shared.Pages.Restaurant.Bill;

public partial class BillPage : IAsyncDisposable
{
	private HotKeysContext _hotKeysContext;

	[Parameter] public int? Id { get; set; }
	[Parameter] public int? DiningTableId { get; set; }

	private UserModel _user;

	private bool _isLoading = true;
	private bool _isProcessing = false;

	private int _primaryCompanyId;

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
	private readonly List<PaymentItem> _payments = [];
	private readonly List<PaymentModeModel> _paymentMethods = PaymentModeData.GetPaymentModes();

	private PaymentModeModel _selectedPaymentMethod = new();
	private decimal _paymentAmount = 0;
	private decimal _remainingAmount => _bill.TotalAmount - _payments.Sum(p => p.Amount);

	private SfAutoComplete<ProductLocationOverviewModel, ProductLocationOverviewModel> _sfItemAutoComplete;
	private SfGrid<BillItemCartModel> _sfCartGrid;

	ToastNotification _toastNotification;

	#region Initialization
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		_user = await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, [UserRoles.Restaurant]);
		await InitializePage();
		_isLoading = false;
		StateHasChanged();
	}

	private async Task InitializePage()
	{
		_hotKeysContext = HotKeys.CreateContext()
			.Add(ModCode.Ctrl, Code.Enter, AddItemToCart, "Add item to cart", Exclude.None)
			.Add(ModCode.Ctrl, Code.E, () => _sfItemAutoComplete.FocusAsync(), "Focus on item input", Exclude.None)
			.Add(ModCode.Ctrl, Code.S, SaveTransaction, "Save the transaction", Exclude.None)
			.Add(ModCode.Alt, Code.T, DownloadThermalInvoice, "Download Thermal invoice", Exclude.None)
			.Add(ModCode.Alt, Code.P, DownloadPdfInvoice, "Download PDF invoice", Exclude.None)
			.Add(ModCode.Alt, Code.E, DownloadExcelInvoice, "Download Excel invoice", Exclude.None)
			.Add(ModCode.Ctrl, Code.H, NavigateToTransactionHistoryPage, "Open transaction history", Exclude.None)
			.Add(ModCode.Ctrl, Code.I, NavigateToItemReport, "Open item report", Exclude.None)
			.Add(ModCode.Ctrl, Code.N, ResetPage, "Reset the page", Exclude.None)
			.Add(ModCode.Ctrl, Code.D, NavigateToDashboard, "Go to dashboard", Exclude.None)
			.Add(ModCode.Ctrl, Code.B, NavigateBack, "Back", Exclude.None)
			.Add(ModCode.Ctrl, Code.L, Logout, "Logout", Exclude.None)
			.Add(Code.Delete, RemoveSelectedCartItem, "Delete selected cart item", Exclude.None)
			.Add(Code.Insert, EditSelectedCartItem, "Edit selected cart item", Exclude.None);

		await LoadCompanies();
		await LoadLocations();

		if (!await ResolveBill())
			return;

		await LoadSelections();
		await LoadDiningTables();
		await LoadItems();
		await LoadCart();
		SyncPaymentsFromBill();
		await RecalculateAndSave();
	}

	private async Task LoadCompanies()
	{
		try
		{
			_companies = await CommonData.LoadTableDataByStatus<CompanyModel>(TableNames.Company);
			_companies = [.. _companies.OrderBy(s => s.Name)];

			if (_user.LocationId == 1)
				_companies.Add(new() { Id = 0, Name = "Create New Company ..." });

			var mainCompanyId = await SettingsData.LoadSettingsByKey(SettingsKeys.PrimaryCompanyLinkingId);
			_primaryCompanyId = int.TryParse(mainCompanyId?.Value, out var id) ? id : 0;
			_selectedCompany = _companies.FirstOrDefault(s => s.Id == _primaryCompanyId)
				?? throw new Exception("Main Company Not Found");
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error Loading Companies", ex.Message, ToastType.Error);
		}
	}

	private async Task LoadLocations()
	{
		try
		{
			_locations = await CommonData.LoadTableDataByStatus<LocationModel>(TableNames.Location);
			_locations = [.. _locations.OrderBy(s => s.Name)];

			if (_user.LocationId == 1)
				_locations.Add(new() { Id = 0, Name = "Create New Location ..." });

			_selectedLocation = _locations.FirstOrDefault(s => s.Id == _user.LocationId);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error Loading Locations", ex.Message, ToastType.Error);
		}
	}

	private async Task LoadSelections()
	{
		if (_user.LocationId == 1)
		{
			_selectedCompany = _companies.FirstOrDefault(s => s.Id == _bill.CompanyId)
				?? _companies.FirstOrDefault(s => s.Id == _primaryCompanyId)
				?? _selectedCompany;
			_selectedLocation = _locations.FirstOrDefault(s => s.Id == _bill.LocationId)
				?? _locations.FirstOrDefault(s => s.Id == _user.LocationId)
				?? _selectedLocation;
		}
		else
		{
			_bill.CompanyId = _primaryCompanyId;
			_bill.LocationId = _user.LocationId;
			_selectedCompany = _companies.FirstOrDefault(s => s.Id == _primaryCompanyId) ?? _selectedCompany;
			_selectedLocation = _locations.FirstOrDefault(s => s.Id == _user.LocationId) ?? _selectedLocation;
		}

		if (_bill.CustomerId is not null && _bill.CustomerId > 0)
			_selectedCustomer = await CommonData.LoadTableDataById<CustomerModel>(TableNames.Customer, _bill.CustomerId.Value);
		else
		{
			_selectedCustomer = new();
			_bill.CustomerId = null;
		}

		if (_bill.FinancialYearId > 0)
			_selectedFinancialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, _bill.FinancialYearId);
	}

	private async Task LoadDiningTables()
	{
		try
		{
			var diningAreas = await CommonData.LoadTableDataByStatus<DiningAreaModel>(TableNames.DiningArea);
			diningAreas = [.. diningAreas.Where(d => d.LocationId == _bill.LocationId)];

			_diningTables = await CommonData.LoadTableDataByStatus<DiningTableModel>(TableNames.DiningTable);
			_diningTables = [.. _diningTables.Where(dt => diningAreas.Any(da => da.Id == dt.DiningAreaId)).OrderBy(s => s.Name)];

			if (_diningTables.Count == 0)
				await _toastNotification.ShowAsync("No Dining Tables Found", "No dining tables were found for the selected location. Please create dining tables to proceed.", ToastType.Warning);

			if (_user.LocationId == 1)
				_diningTables.Add(new() { Id = 0, Name = "Create New Dining Table ..." });

			_selectedDiningTable = _bill.DiningTableId > 0 ?
				_diningTables.FirstOrDefault(s => s.Id == _bill.DiningTableId) ?? new() :
				new();

			_bill.DiningTableId = _selectedDiningTable.Id;
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error Loading Dining Tables", ex.Message, ToastType.Error);
		}
	}

	private async Task LoadItems()
	{
		try
		{
			_taxes = await CommonData.LoadTableDataByStatus<TaxModel>(TableNames.Tax);
			_products = await ProductLocationData.LoadProductLocationOverviewByProductLocation(LocationId: _bill.LocationId);
			_products = [.. _products.OrderBy(s => s.Name)];

			if (_user.LocationId == 1)
				_products.Add(new() { Id = 0, Name = "Create New Item ..." });
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error Loading Items", ex.Message, ToastType.Error);
		}
	}

	private async Task LoadCart()
	{
		try
		{
			_cart.Clear();

			if (_bill.Id > 0)
			{
				var existingDetails = await CommonData.LoadTableDataByMasterId<BillDetailModel>(TableNames.BillDetail, _bill.Id);
				foreach (var item in existingDetails)
				{
					var product = _products.FirstOrDefault(s => s.ProductId == item.ProductId);
					if (product is null)
					{
						var productInfo = await CommonData.LoadTableDataById<ProductModel>(TableNames.Product, item.ProductId);
						await _toastNotification.ShowAsync("Item Not Found", $"The item {productInfo?.Name} (ID: {item.ProductId}) was not found in available items. It may have been deleted.", ToastType.Error);
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
			}
			else if (await DataStorageService.LocalExists(StorageFileNames.BillCartDataFileName))
			{
				_cart = System.Text.Json.JsonSerializer.Deserialize<List<BillItemCartModel>>(
					await DataStorageService.LocalGetAsync(StorageFileNames.BillCartDataFileName)) ?? [];
			}
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error Loading Cart", ex.Message, ToastType.Error);
			await DeleteLocalFiles();
		}
	}
	#endregion

	#region Bill Resolution (Load / Create / Redirect)
	/// <summary>
	/// Determines which bill to work with based on route parameters and local storage.
	/// Returns false if a navigation redirect occurred (caller should stop initialization).
	///
	/// Priority order:
	///   1. Id parameter → load existing bill by ID
	///   2. DiningTableId parameter → check for running bill on that table, redirect or create new
	///   3. No parameters → try restore from local storage, otherwise create blank bill
	/// </summary>
	private async Task<bool> ResolveBill()
	{
		try
		{
			// Mode 1: Editing an existing bill by ID
			if (Id.HasValue)
				return await LoadExistingBill(Id.Value);

			// Mode 2: Opening from dining dashboard with a specific table
			if (DiningTableId.HasValue)
				return await ResolveTableBill(DiningTableId.Value);

			// Mode 3: No route params — try local storage, then create new
			if (await TryRestoreFromLocalStorage())
				return true;

			await CreateNewBill(null);
			return true;
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error Loading Bill", ex.Message, ToastType.Error);
			await DeleteLocalFiles();
			await CreateNewBill(null);
			return true;
		}
	}

	/// <summary>
	/// Loads an existing bill by ID and validates the user has access to it.
	/// Rules:
	///   - Bill must exist
	///   - Non-head-office users cannot view bills from other locations
	///   - Settled (non-running) bills can only be edited by admins at head office
	/// </summary>
	private async Task<bool> LoadExistingBill(int billId)
	{
		_bill = await CommonData.LoadTableDataById<BillModel>(TableNames.Bill, billId);

		if (_bill is null || _bill.Id == 0)
		{
			await _toastNotification.ShowAsync("Transaction Not Found", "The requested transaction could not be found.", ToastType.Error);
			NavigationManager.NavigateTo(PageRouteNames.Bill, true);
			return false;
		}

		// Non-head-office users cannot see other location's bills
		if (_user.LocationId != 1 && _bill.LocationId != _user.LocationId)
		{
			await _toastNotification.ShowAsync("Access Denied", "You do not have permission to view this bill.", ToastType.Error);
			NavigationManager.NavigateTo(PageRouteNames.Bill, true);
			return false;
		}

		// Settled bills can only be edited by admins at head office
		if (!_bill.Running && !(_user.Admin && _user.LocationId == 1))
		{
			await _toastNotification.ShowAsync("Access Denied", "Only admin users can edit settled bills.", ToastType.Error);
			NavigationManager.NavigateTo(PageRouteNames.Bill, true);
			return false;
		}

		return true;
	}

	/// <summary>
	/// Resolves a bill for a specific dining table.
	/// If the table already has a running bill → redirects to it.
	/// Otherwise → validates the table belongs to the user's location and creates a new bill.
	/// </summary>
	private async Task<bool> ResolveTableBill(int diningTableId)
	{
		var diningTable = await CommonData.LoadTableDataById<DiningTableModel>(TableNames.DiningTable, diningTableId);
		if (diningTable is null)
		{
			await _toastNotification.ShowAsync("Dining Table Not Found", "The selected dining table could not be found.", ToastType.Error);
			NavigationManager.NavigateTo(PageRouteNames.Bill, true);
			return false;
		}

		var diningArea = await CommonData.LoadTableDataById<DiningAreaModel>(TableNames.DiningArea, diningTable.DiningAreaId);

		// Non-head-office users cannot create bills on tables from other locations
		if (_user.LocationId != 1 && diningArea.LocationId != _user.LocationId)
		{
			await _toastNotification.ShowAsync("Access Denied", "This dining table does not belong to your location.", ToastType.Error);
			NavigationManager.NavigateTo(PageRouteNames.Bill, true);
			return false;
		}

		// Check if this table already has a running bill → redirect to it
		var runningBills = await BillData.LoadRunningBillByLocationId(diningArea.LocationId);
		var existingBill = runningBills.FirstOrDefault(b => b.DiningTableId == diningTableId && b.Running);

		if (existingBill is not null)
		{
			// If we're already on the correct bill, just load it directly
			if (Id.HasValue && Id.Value == existingBill.Id)
				return await LoadExistingBill(existingBill.Id);

			NavigationManager.NavigateTo($"{PageRouteNames.Bill}/transaction/{existingBill.Id}", true);
			return false;
		}

		// No running bill on this table — create a new one
		await CreateNewBill(diningTableId);
		_bill.LocationId = diningArea.LocationId;
		return true;
	}

	private async Task<bool> TryRestoreFromLocalStorage()
	{
		if (!await DataStorageService.LocalExists(StorageFileNames.BillDataFileName))
			return false;

		try
		{
			_bill = System.Text.Json.JsonSerializer.Deserialize<BillModel>(
				await DataStorageService.LocalGetAsync(StorageFileNames.BillDataFileName));
			return _bill is not null && _bill.Id == 0; // Only restore unsaved new bills
		}
		catch
		{
			await DeleteLocalFiles();
			return false;
		}
	}

	private async Task CreateNewBill(int? diningTableId)
	{
		var currentDateTime = await CommonData.LoadCurrentDateTime();
		var financialYear = await FinancialYearData.LoadFinancialYearByDateTime(currentDateTime);

		_bill = new()
		{
			Id = 0,
			TransactionNo = string.Empty,
			CompanyId = _primaryCompanyId,
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
	#endregion

	#region Payments
	private void SyncPaymentsFromBill()
	{
		_payments.Clear();

		AddPaymentFromBill("Cash", _bill.Cash);
		AddPaymentFromBill("Card", _bill.Card);
		AddPaymentFromBill("UPI", _bill.UPI);
		AddPaymentFromBill("Credit", _bill.Credit);

		_selectedPaymentMethod = _paymentMethods.FirstOrDefault();
		_paymentAmount = Math.Max(0, _remainingAmount);
	}

	private void AddPaymentFromBill(string modeName, decimal amount)
	{
		if (amount <= 0)
			return;

		var mode = _paymentMethods.FirstOrDefault(pm => pm.Name == modeName);
		if (mode is null)
			return;

		_payments.Add(new() { Id = mode.Id, Method = mode.Name, Amount = amount });
	}

	private void ApplyPaymentsToBill()
	{
		_bill.Cash = _payments.FirstOrDefault(p => p.Method == "Cash")?.Amount ?? 0;
		_bill.Card = _payments.FirstOrDefault(p => p.Method == "Card")?.Amount ?? 0;
		_bill.UPI = _payments.FirstOrDefault(p => p.Method == "UPI")?.Amount ?? 0;
		_bill.Credit = _payments.FirstOrDefault(p => p.Method == "Credit")?.Amount ?? 0;
	}

	private async Task AddPayment()
	{
		if (_isProcessing || _paymentAmount <= 0 || _selectedPaymentMethod is null || _selectedPaymentMethod.Id <= 0)
			return;

		if (_paymentAmount > _remainingAmount)
		{
			await _toastNotification.ShowAsync("Invalid Payment Amount", $"Payment amount cannot exceed remaining amount of ₹{_remainingAmount:N2}", ToastType.Error);
			return;
		}

		var existingPayment = _payments.FirstOrDefault(p => p.Id == _selectedPaymentMethod.Id);
		if (existingPayment is not null)
			existingPayment.Amount += _paymentAmount;
		else
			_payments.Add(new()
			{
				Id = _selectedPaymentMethod.Id,
				Method = _selectedPaymentMethod.Name,
				Amount = _paymentAmount
			});

		ApplyPaymentsToBill();
		_paymentAmount = Math.Max(0, _remainingAmount);
		_selectedPaymentMethod = _paymentMethods.FirstOrDefault(pm => pm.Id != _selectedPaymentMethod.Id)
								 ?? _paymentMethods.FirstOrDefault()
								 ?? new();

		await RecalculateAndSave(true);
	}

	private async Task RemovePayment(PaymentItem payment)
	{
		if (_isProcessing || payment is null)
			return;

		_payments.Remove(payment);
		ApplyPaymentsToBill();

		_selectedPaymentMethod = _paymentMethods.FirstOrDefault();
		_paymentAmount = Math.Max(0, _remainingAmount);
		await RecalculateAndSave(true);
	}
	#endregion

	#region Change Events
	private async Task OnCompanyChanged(ChangeEventArgs<CompanyModel, CompanyModel> args)
	{
		if (_user.LocationId != 1)
		{
			_selectedCompany = _companies.FirstOrDefault(s => s.Id == _primaryCompanyId);
			_bill.CompanyId = _primaryCompanyId;
			await _toastNotification.ShowAsync("Company Change Not Allowed", "You are not allowed to change the company.", ToastType.Error);
			return;
		}

		if (args.Value is null)
			return;

		if (args.Value.Id == 0)
		{
			if (FormFactor.GetFormFactor() == "Web")
				await JSRuntime.InvokeVoidAsync("open", PageRouteNames.CompanyMaster, "_blank");
			else
				NavigationManager.NavigateTo(PageRouteNames.CompanyMaster);

			return;
		}

		_selectedCompany = args.Value;
		_bill.CompanyId = _selectedCompany.Id;
		await RecalculateAndSave();
	}

	private async Task OnLocationChanged(ChangeEventArgs<LocationModel, LocationModel> args)
	{
		if (_user.LocationId != 1)
		{
			_selectedLocation = _locations.FirstOrDefault(s => s.Id == _user.LocationId);
			_bill.LocationId = _user.LocationId;
			await _toastNotification.ShowAsync("Location Change Not Allowed", "You are not allowed to change the location.", ToastType.Error);
			return;
		}

		if (args.Value is null)
			return;

		if (args.Value.Id == 0)
		{
			if (FormFactor.GetFormFactor() == "Web")
				await JSRuntime.InvokeVoidAsync("open", PageRouteNames.Location, "_blank");
			else
				NavigationManager.NavigateTo(PageRouteNames.Location);

			return;
		}

		_selectedLocation = args.Value;
		_bill.LocationId = _selectedLocation.Id;

		await LoadDiningTables();
		await LoadItems();
		await RecalculateAndSave();
	}

	private async Task OnDiningTableChanged(ChangeEventArgs<DiningTableModel, DiningTableModel> args)
	{
		if (args.Value is null)
			return;

		if (args.Value.Id == 0)
		{
			if (FormFactor.GetFormFactor() == "Web")
				await JSRuntime.InvokeVoidAsync("open", PageRouteNames.DiningTable, "_blank");
			else
				NavigationManager.NavigateTo(PageRouteNames.DiningTable);

			return;
		}

		var diningArea = await CommonData.LoadTableDataById<DiningAreaModel>(TableNames.DiningArea, args.Value.DiningAreaId);
		if (diningArea.LocationId != _bill.LocationId)
		{
			await _toastNotification.ShowAsync("Invalid Dining Table", "The selected dining table does not belong to the selected location.", ToastType.Error);
			return;
		}

		var runningBills = await BillData.LoadRunningBillByLocationId(diningArea.LocationId);
		var existingRunningBill = runningBills.FirstOrDefault(b => b.DiningTableId == args.Value.Id);
		if (existingRunningBill is not null)
		{
			NavigationManager.NavigateTo($"{PageRouteNames.Bill}/transaction/{existingRunningBill.Id}", true);
			return;
		}

		_selectedDiningTable = args.Value;
		_bill.DiningTableId = _selectedDiningTable.Id;

		await LoadItems();
		await RecalculateAndSave();
	}

	private async Task OnTransactionDateChanged(Syncfusion.Blazor.Calendars.ChangedEventArgs<DateTime> args)
	{
		if (_user.LocationId != 1)
		{
			_bill.TransactionDateTime = await CommonData.LoadCurrentDateTime();
			await _toastNotification.ShowAsync("Date Change Not Allowed", "You are not allowed to change the transaction date.", ToastType.Error);
			return;
		}

		_bill.TransactionDateTime = args.Value;
		await LoadItems();
		await RecalculateAndSave();
	}

	private async Task OnCustomerNumberChanged(string args)
	{
		if (args.Any(c => !char.IsDigit(c)))
			args = new string([.. args.Where(char.IsDigit)]);

		if (string.IsNullOrWhiteSpace(args))
		{
			_selectedCustomer = new();
			_bill.CustomerId = null;
			await RecalculateAndSave();
			return;
		}

		args = args.Trim();
		_selectedCustomer = await CustomerData.LoadCustomerByNumber(args);
		_selectedCustomer ??= new() { Id = 0, Name = "", Number = args };

		_bill.CustomerId = _selectedCustomer.Id;
		await RecalculateAndSave();
	}

	private async Task OnDiscountPercentChanged(ChangeEventArgs<decimal> args)
	{
		_bill.DiscountPercent = args.Value;
		await RecalculateAndSave();
	}

	private async Task OnServiceChargePercentChanged(ChangeEventArgs<decimal> args)
	{
		_bill.ServiceChargePercent = args.Value;
		await RecalculateAndSave();
	}

	private async Task OnRoundOffAmountChanged(ChangeEventArgs<decimal> args)
	{
		_bill.RoundOffAmount = args.Value;
		await RecalculateAndSave(true);
	}
	#endregion

	#region Cart
	private async Task OnItemChanged(ChangeEventArgs<ProductLocationOverviewModel, ProductLocationOverviewModel> args)
	{
		if (args.Value is null)
			return;

		if (args.Value.Id == 0)
		{
			if (FormFactor.GetFormFactor() == "Web")
				await JSRuntime.InvokeVoidAsync("open", PageRouteNames.Product, "_blank");
			else
				NavigationManager.NavigateTo(PageRouteNames.Product);

			return;
		}

		_selectedProduct = args.Value;

		if (_selectedProduct is null)
		{
			_selectedCart = new();
			return;
		}

		var tax = _taxes.FirstOrDefault(s => s.Id == _selectedProduct.TaxId);

		_selectedCart.ItemId = _selectedProduct.ProductId;
		_selectedCart.ItemName = _selectedProduct.Name;
		_selectedCart.Quantity = 0;
		_selectedCart.Rate = _selectedProduct.Rate;
		_selectedCart.DiscountPercent = 0;
		_selectedCart.CGSTPercent = _taxes.FirstOrDefault(s => s.Id == _selectedProduct.TaxId).CGST;
		_selectedCart.SGSTPercent = _taxes.FirstOrDefault(s => s.Id == _selectedProduct.TaxId).SGST;
		_selectedCart.IGSTPercent = 0;
		_selectedCart.InclusiveTax = _taxes.FirstOrDefault(s => s.Id == _selectedProduct.TaxId).Inclusive;
		_selectedCart.KOTPrint = true;

		RecalculateSelectedItem();
	}

	private void OnItemQuantityChanged(ChangeEventArgs<decimal> args)
	{
		_selectedCart.Quantity = args.Value;
		RecalculateSelectedItem();
	}

	private void OnItemRateChanged(ChangeEventArgs<decimal> args)
	{
		_selectedCart.Rate = args.Value;
		RecalculateSelectedItem();
	}

	private void OnItemDiscountPercentChanged(ChangeEventArgs<decimal> args)
	{
		_selectedCart.DiscountPercent = args.Value;
		RecalculateSelectedItem();
	}

	private void OnItemCGSTPercentChanged(ChangeEventArgs<decimal> args)
	{
		_selectedCart.CGSTPercent = args.Value;
		RecalculateSelectedItem();
	}

	private void OnItemSGSTPercentChanged(ChangeEventArgs<decimal> args)
	{
		_selectedCart.SGSTPercent = args.Value;
		RecalculateSelectedItem();
	}

	private void OnItemIGSTPercentChanged(ChangeEventArgs<decimal> args)
	{
		_selectedCart.IGSTPercent = args.Value;
		RecalculateSelectedItem();
	}

	private void OnItemInclusiveTaxChanged(Syncfusion.Blazor.Buttons.ChangeEventArgs<bool> args)
	{
		_selectedCart.InclusiveTax = args.Checked;
		RecalculateSelectedItem();
	}

	private void RecalculateSelectedItem()
	{
		if (_selectedProduct is null)
			return;

		if (_selectedCart.Quantity < 0)
			_selectedCart.Quantity = 1;

		_selectedCart.ItemId = _selectedProduct.ProductId;
		_selectedCart.ItemName = _selectedProduct.Name;

		CalculateItemFinancials(_selectedCart);
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

		// Validate tax configuration: CGST+SGST or IGST, not all three
		int taxCount = 0;
		if (_selectedCart.CGSTPercent > 0) taxCount++;
		if (_selectedCart.SGSTPercent > 0) taxCount++;
		if (_selectedCart.IGSTPercent > 0) taxCount++;

		if (taxCount == 3)
		{
			await _toastNotification.ShowAsync("Invalid Tax Configuration", "All three taxes (CGST, SGST, IGST) cannot be applied together. Use either CGST+SGST or IGST only.", ToastType.Error);
			return;
		}

		RecalculateSelectedItem();

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
		{
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
		}

		_selectedProduct = null;
		_selectedCart = new();

		await _sfItemAutoComplete.FocusAsync();
		await RecalculateAndSave();
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

		await _sfItemAutoComplete.FocusAsync();
		RecalculateSelectedItem();
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
		await RecalculateAndSave();
	}
	#endregion

	#region Financial Calculations
	private static void CalculateItemFinancials(BillItemCartModel item)
	{
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

		item.Remarks = string.IsNullOrWhiteSpace(item.Remarks) ? null : item.Remarks.Trim();

		if (item.Quantity > 0)
			item.NetRate = item.Total / item.Quantity * (1 - item.DiscountPercent / 100);
	}

	private async Task RecalculateAndSave(bool customRoundOff = false)
	{
		if (_isProcessing || _isLoading)
			return;

		try
		{
			_isProcessing = true;

			_cart.RemoveAll(item => item.Quantity == 0);

			foreach (var item in _cart)
				CalculateItemFinancials(item);

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

			_bill.CompanyId = _user.LocationId == 1 ? _selectedCompany.Id : _primaryCompanyId;
			_bill.LocationId = _user.LocationId == 1 ? _selectedLocation.Id : _user.LocationId;
			_bill.DiningTableId = _selectedDiningTable.Id;
			_bill.CustomerId = _selectedCustomer?.Id > 0 ? _selectedCustomer.Id : null;
			_bill.CreatedBy = _user.Id;

			if (_user.LocationId != 1)
				_bill.TransactionDateTime = await CommonData.LoadCurrentDateTime();

			_selectedFinancialYear = await FinancialYearData.LoadFinancialYearByDateTime(_bill.TransactionDateTime);
			if (_selectedFinancialYear is not null && !_selectedFinancialYear.Locked)
				_bill.FinancialYearId = _selectedFinancialYear.Id;
			else
			{
				await _toastNotification.ShowAsync("Invalid Transaction Date", "The selected transaction date does not fall within an active financial year.", ToastType.Error);
				_bill.TransactionDateTime = await CommonData.LoadCurrentDateTime();
				_selectedFinancialYear = await FinancialYearData.LoadFinancialYearByDateTime(_bill.TransactionDateTime);
				_bill.FinancialYearId = _selectedFinancialYear?.Id ?? 0;
			}

			if (_bill.Id == 0)
				_bill.TransactionNo = await GenerateCodes.GenerateBillTransactionNo(_bill);

			ApplyPaymentsToBill();

			await DataStorageService.LocalSaveAsync(StorageFileNames.BillDataFileName, System.Text.Json.JsonSerializer.Serialize(_bill));
			await DataStorageService.LocalSaveAsync(StorageFileNames.BillCartDataFileName, System.Text.Json.JsonSerializer.Serialize(_cart));
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error Saving Data", ex.Message, ToastType.Error);
		}
		finally
		{
			if (_sfCartGrid is not null)
				await _sfCartGrid.Refresh();

			_paymentAmount = Math.Max(0, _remainingAmount);
			_isProcessing = false;
			StateHasChanged();
		}
	}
	#endregion

	#region Save Transaction
	private async Task<bool> ValidateForm()
	{
		if (_selectedCompany is null || _bill.CompanyId <= 0)
		{
			await _toastNotification.ShowAsync("Company Not Selected", "Please select a company for the transaction.", ToastType.Error);
			return false;
		}

		if (string.IsNullOrWhiteSpace(_bill.TransactionNo))
		{
			await _toastNotification.ShowAsync("Transaction Number Missing", "Transaction number could not be generated. Please try again.", ToastType.Error);
			return false;
		}

		if (_bill.TransactionDateTime == default)
		{
			await _toastNotification.ShowAsync("Transaction Date Missing", "Please select a valid transaction date.", ToastType.Error);
			return false;
		}

		if (_selectedFinancialYear is null || _bill.FinancialYearId <= 0)
		{
			await _toastNotification.ShowAsync("Financial Year Not Found", "The transaction date does not fall within any financial year.", ToastType.Error);
			return false;
		}

		if (_selectedFinancialYear.Locked)
		{
			await _toastNotification.ShowAsync("Financial Year Locked", "The financial year for the selected date is locked.", ToastType.Error);
			return false;
		}

		if (!_selectedFinancialYear.Status)
		{
			await _toastNotification.ShowAsync("Financial Year Inactive", "The financial year for the selected date is inactive.", ToastType.Error);
			return false;
		}

		if (_bill.TotalItems <= 0 || _cart.Count == 0)
		{
			await _toastNotification.ShowAsync("Cart is Empty", "Please add at least one item to the cart before saving.", ToastType.Error);
			return false;
		}

		if (_cart.Any(item => item.Quantity <= 0))
		{
			await _toastNotification.ShowAsync("Invalid Item Quantity", "One or more items in the cart have zero or negative quantity.", ToastType.Error);
			return false;
		}

		if (_bill.TotalAmount < 0)
		{
			await _toastNotification.ShowAsync("Invalid Total Amount", "The total amount cannot be negative.", ToastType.Error);
			return false;
		}

		if (_bill.Id > 0)
		{
			var existingBill = await CommonData.LoadTableDataById<BillModel>(TableNames.Bill, _bill.Id);
			await FinancialYearData.ValidateFinancialYear(existingBill.TransactionDateTime);

			if (!existingBill.Running && !(_user.Admin && _user.LocationId == 1))
			{
				await _toastNotification.ShowAsync("Insufficient Permissions", "Only admin users can modify settled bills.", ToastType.Error);
				return false;
			}
		}

		if (string.IsNullOrWhiteSpace(_selectedCustomer.Name) && !string.IsNullOrWhiteSpace(_selectedCustomer.Number))
		{
			await _toastNotification.ShowAsync("Customer Name Missing", "Please enter a name for the new customer or clear the customer field.", ToastType.Error);
			return false;
		}

		if (_bill.Cash < 0 || _bill.Card < 0 || _bill.Credit < 0 || _bill.UPI < 0)
		{
			await _toastNotification.ShowAsync("Invalid Payment Amounts", "Payment amounts cannot be negative.", ToastType.Error);
			return false;
		}

		if (_bill.DiningTableId > 0)
		{
			var diningTable = await CommonData.LoadTableDataById<DiningTableModel>(TableNames.DiningTable, _bill.DiningTableId);
			var diningArea = await CommonData.LoadTableDataById<DiningAreaModel>(TableNames.DiningArea, diningTable.DiningAreaId);
			if (diningArea.LocationId != _bill.LocationId)
			{
				await _toastNotification.ShowAsync("Invalid Dining Table", "The selected dining table does not belong to the selected location.", ToastType.Error);
				return false;
			}
		}

		_bill.Remarks = string.IsNullOrWhiteSpace(_bill.Remarks) ? null : _bill.Remarks.Trim();
		return true;
	}

	private async Task HandleCustomerCreation()
	{
		if (_selectedCustomer.Id > 0)
		{
			_selectedCustomer = await CommonData.LoadTableDataById<CustomerModel>(TableNames.Customer, _selectedCustomer.Id);
			_bill.CustomerId = _selectedCustomer.Id;
		}
		else if (!string.IsNullOrWhiteSpace(_selectedCustomer.Number))
		{
			_selectedCustomer.Id = await CustomerData.InsertCustomer(_selectedCustomer);
			_bill.CustomerId = _selectedCustomer.Id;
		}
		else
		{
			_selectedCustomer = new();
			_bill.CustomerId = null;
		}
	}

	private async Task HandleBillSettlement()
	{
		_bill.Status = true;
		var currentDateTime = await CommonData.LoadCurrentDateTime();
		_bill.TransactionDateTime = DateOnly.FromDateTime(_bill.TransactionDateTime).ToDateTime(new TimeOnly(currentDateTime.Hour, currentDateTime.Minute, currentDateTime.Second));
		_bill.LastModifiedAt = currentDateTime;
		_bill.CreatedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform();
		_bill.LastModifiedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform();
		_bill.CreatedBy = _user.Id;
		_bill.LastModifiedBy = _user.Id;

		_bill.Running = _bill.Cash + _bill.Card + _bill.UPI + _bill.Credit != _bill.TotalAmount;

		if (!_bill.Running)
			foreach (var item in _cart)
				item.KOTPrint = false;
	}

	private bool NeedsSave => _bill.Id == 0 || _bill.Running;

	private async Task<bool> SaveCore()
	{
		await RecalculateAndSave(true);

		if (!await ValidateForm())
			return false;

		await _toastNotification.ShowAsync("Processing Transaction", "Please wait while the transaction is being saved...", ToastType.Info);

		await HandleCustomerCreation();
		await HandleBillSettlement();

		_bill.Id = await BillData.SaveTransaction(_bill, _cart);
		return true;
	}

	private async Task SaveTransaction()
	{
		if (_isProcessing || _isLoading)
			return;

		try
		{
			_isProcessing = true;

			if (!await SaveCore())
			{
				_isProcessing = false;
				return;
			}

			if (!_bill.Running)
			{
				var (pdfStream, fileName) = await BillInvoiceExport.ExportInvoice(_bill.Id, InvoiceExportType.PDF);
				await SaveAndViewService.SaveAndView(fileName, pdfStream);
			}

			await ResetPage();
			await _toastNotification.ShowAsync("Save Transaction", "Transaction saved successfully!", ToastType.Success);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error Saving Transaction", ex.Message, ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
		}
	}
	#endregion

	#region Utilities

	private async Task DownloadThermalInvoice()
	{
		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();

			if (NeedsSave && !await SaveCore())
			{
				_isProcessing = false;
				return;
			}

			await _toastNotification.ShowAsync("Processing", "Generating Thermal invoice...", ToastType.Info);

			var printStream = await BillThermalPrint.GenerateThermalBill(_bill.Id);
			await JSRuntime.InvokeVoidAsync("printToPrinter", printStream.ToString());
			await Task.Delay(2000);

			if (!_bill.Running)
				await ResetPage();

			await _toastNotification.ShowAsync("Invoice Downloaded", "The thermal invoice has been generated successfully.", ToastType.Success);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error Downloading Invoice", ex.Message, ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
		}
	}

	private async Task DownloadPdfInvoice()
	{
		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();

			if (NeedsSave && !await SaveCore())
			{
				_isProcessing = false;
				return;
			}

			await _toastNotification.ShowAsync("Processing", "Generating PDF invoice...", ToastType.Info);

			var (pdfStream, fileName) = await BillInvoiceExport.ExportInvoice(_bill.Id, InvoiceExportType.PDF);
			await SaveAndViewService.SaveAndView(fileName, pdfStream);

			await _toastNotification.ShowAsync("Invoice Downloaded", "The PDF invoice has been downloaded successfully.", ToastType.Success);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error Downloading Invoice", ex.Message, ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
		}
	}

	private async Task DownloadExcelInvoice()
	{
		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();

			if (NeedsSave && !await SaveCore())
			{
				_isProcessing = false;
				return;
			}

			await _toastNotification.ShowAsync("Processing", "Generating Excel invoice...", ToastType.Info);

			var (excelStream, fileName) = await BillInvoiceExport.ExportInvoice(_bill.Id, InvoiceExportType.Excel);
			await SaveAndViewService.SaveAndView(fileName, excelStream);

			await _toastNotification.ShowAsync("Invoice Downloaded", "The Excel invoice has been downloaded successfully.", ToastType.Success);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error Downloading Invoice", ex.Message, ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
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
		NavigationManager.NavigateTo(PageRouteNames.Bill, true);
	}

	private async Task NavigateToTransactionHistoryPage()
	{
		if (FormFactor.GetFormFactor() == "Web")
			await JSRuntime.InvokeVoidAsync("open", PageRouteNames.BillReport, "_blank");
		else
			NavigationManager.NavigateTo(PageRouteNames.BillReport);
	}

	private async Task NavigateToItemReport()
	{
		if (FormFactor.GetFormFactor() == "Web")
			await JSRuntime.InvokeVoidAsync("open", PageRouteNames.BillItemReport, "_blank");
		else
			NavigationManager.NavigateTo(PageRouteNames.BillItemReport);
	}

	private void NavigateToDashboard() =>
		NavigationManager.NavigateTo(PageRouteNames.Dashboard);

	private void NavigateBack() =>
		NavigationManager.NavigateTo(PageRouteNames.RestaurantDashboard);

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