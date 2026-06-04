using PrimeBakes.Shared.Components.Dialog;
using PrimeBakes.Shared.Components.Input;
using PrimeBakesLibrary.Data.Inventory;
using PrimeBakesLibrary.Data.Inventory.Purchase;
using PrimeBakesLibrary.Data.Store.Product;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Exporting.Inventory.Recipe;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Inventory;
using PrimeBakesLibrary.Models.Operations;
using PrimeBakesLibrary.Models.Store.Product;

using Syncfusion.Blazor.DropDowns;
using Syncfusion.Blazor.Grids;

namespace PrimeBakes.Shared.Pages.Inventory.Recipe;

public partial class RecipePage
{
	private bool _isLoading = true;
	private bool _isProcessing = false;

	private ProductLocationOverviewModel _selectedProduct;
	private RawMaterialModel _selectedRawMaterial;
	private RecipeItemCartModel _selectedCart = new();
	private RecipeModel _recipe = new();
	private DateTime _recipeDateTime = DateTime.Now;

	private List<ProductLocationOverviewModel> _products = [];
	private List<RawMaterialModel> _rawMaterials = [];
	private readonly List<RecipeItemCartModel> _recipeItems = [];
	private readonly List<ContextMenuItemModel> _recipeGridContextMenuItems =
	[
		new() { Text = "Edit Item (Insert)", Id = "EditRecipeItem", IconCss = "e-icons e-edit", Target = ".e-content" },
		new() { Text = "Delete Item (Del)", Id = "DeleteRecipeItem", IconCss = "e-icons e-trash", Target = ".e-content" }
	];

	private AutoCompleteWithAdd<RawMaterialModel, RawMaterialModel> _sfItemAutoComplete;
	private SfGrid<RecipeItemCartModel> _sfCartGrid;

	private ToastNotification _toastNotification;

	#region Load Data
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, [UserRoles.Inventory], true);
		await LoadData();
	}

	private async Task LoadData()
	{
		try
		{
			_recipeDateTime = await CommonData.LoadCurrentDateTime();
			_products = await ProductLocationData.LoadProductLocationOverviewByProductLocation(LocationId: 1);
			_rawMaterials = await PurchaseData.LoadRawMaterialByPartyPurchaseDateTime(0, _recipeDateTime);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("An Error Occurred While Loading Data", ex.Message, ToastType.Error);
		}

		_isLoading = false;
		StateHasChanged();
	}

	private async Task OnProductChanged(ChangeEventArgs<ProductLocationOverviewModel, ProductLocationOverviewModel> args)
	{
		try
		{
			if (args is null)
				_selectedProduct = _products.FirstOrDefault();
			else
				_selectedProduct = args.Value;

			await LoadRecipe();
			StateHasChanged();
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error Changing Product", ex.Message, ToastType.Error);
		}
	}

	private async Task LoadRecipe()
	{
		if (_isProcessing || _selectedProduct is null || _selectedProduct.ProductId == 0 || _isLoading)
			return;

		try
		{
			_isProcessing = true;

			_recipeItems.Clear();
			if (_sfCartGrid is not null)
				await _sfCartGrid?.Refresh();

			_recipe = await RecipeData.LoadRecipeByProduct(_selectedProduct.ProductId);
			_recipe ??= new() { ProductId = _selectedProduct.ProductId, Deduct = true };

			var recipeDetails = await CommonData.LoadTableDataByMasterId<RecipeDetailModel>(TableNames.RecipeDetail, _recipe.Id);
			if (recipeDetails is null || recipeDetails.Count == 0)
				return;

			_recipeItems.Clear();

			foreach (var detail in recipeDetails)
			{
				var rawMaterial = _rawMaterials.FirstOrDefault(r => r.Id == detail.RawMaterialId);

				var amount = detail.Quantity * rawMaterial.Rate;
				_recipeItems.Add(new()
				{
					ItemId = detail.RawMaterialId,
					ItemName = rawMaterial.Name,
					Quantity = detail.Quantity,
					Rate = rawMaterial.Rate,
					Amount = amount,
					PerUnit = _recipe.Quantity > 0 ? amount / _recipe.Quantity : 0m
				});
			}

			await _toastNotification.ShowAsync("Recipe Loaded", $"Recipe loaded for {_selectedProduct.Name} with {_recipeItems.Count} items", ToastType.Success);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error Loading Recipe", ex.Message, ToastType.Error);
		}
		finally
		{
			if (_sfCartGrid is not null)
				await _sfCartGrid?.Refresh();

			_isProcessing = false;
			StateHasChanged();
		}
	}
	#endregion

	#region Cart
	private async Task OnRecipeDateChanged(Syncfusion.Blazor.Calendars.ChangedEventArgs<DateTime> args)
	{
		_rawMaterials = await PurchaseData.LoadRawMaterialByPartyPurchaseDateTime(0, _recipeDateTime);

		foreach (var rawMaterial in _recipeItems)
		{
			rawMaterial.Rate = _rawMaterials.FirstOrDefault(r => r.Id == rawMaterial.ItemId)?.Rate ?? rawMaterial.Rate;
			rawMaterial.Amount = rawMaterial.Quantity * rawMaterial.Rate;
			rawMaterial.PerUnit = _recipe.Quantity > 0 ? rawMaterial.Amount / _recipe.Quantity : 0m;
		}

		if (_sfCartGrid is not null)
			await _sfCartGrid.Refresh();
	}

	private async Task AddItemToCart()
	{
		if (_selectedRawMaterial is null || _selectedRawMaterial.Id == 0 || _selectedCart.Quantity <= 0)
			return;

		var existingRecipe = _recipeItems.FirstOrDefault(r => r.ItemId == _selectedRawMaterial.Id);
		if (existingRecipe is not null)
		{
			existingRecipe.Quantity += _selectedCart.Quantity;
			existingRecipe.Amount = existingRecipe.Quantity * existingRecipe.Rate;
			existingRecipe.PerUnit = _recipe.Quantity > 0 ? existingRecipe.Amount / _recipe.Quantity : 0m;
		}
		else
		{
			var amount = _selectedCart.Quantity * _selectedRawMaterial.Rate;
			_recipeItems.Add(new()
			{
				ItemId = _selectedRawMaterial.Id,
				ItemName = _selectedRawMaterial.Name,
				Quantity = _selectedCart.Quantity,
				Rate = _selectedRawMaterial.Rate,
				Amount = amount,
				PerUnit = _recipe.Quantity > 0 ? amount / _recipe.Quantity : 0m
			});
		}

		_selectedRawMaterial = null;
		_selectedCart = new();

		_sfItemAutoComplete?.FocusAsync();

		if (_sfCartGrid is not null)
			await _sfCartGrid?.Refresh();

		StateHasChanged();
	}

	private async Task EditSelectedCartItem()
	{
		if (_sfCartGrid is null || _sfCartGrid.SelectedRecords is null || _sfCartGrid.SelectedRecords.Count == 0)
			return;

		var selectedCartItem = _sfCartGrid.SelectedRecords.First();
		await EditCartItem(selectedCartItem);
	}

	private async Task EditCartItem(RecipeItemCartModel cartItem)
	{
		_selectedRawMaterial = _rawMaterials.FirstOrDefault(r => r.Id == cartItem.ItemId);
		_selectedCart = new()
		{
			ItemId = cartItem.ItemId,
			ItemName = cartItem.ItemName,
			Quantity = cartItem.Quantity,
			Rate = cartItem.Rate,
			Amount = cartItem.Amount
		};
		_recipeItems.Remove(cartItem);

		_sfItemAutoComplete?.FocusAsync();

		if (_sfCartGrid is not null)
			await _sfCartGrid?.Refresh();

		StateHasChanged();
	}

	private async Task RemoveSelectedCartItem()
	{
		if (_sfCartGrid is null || _sfCartGrid.SelectedRecords is null || _sfCartGrid.SelectedRecords.Count == 0)
			return;

		var selectedCartItem = _sfCartGrid.SelectedRecords.First();
		await RemoveItemFromCart(selectedCartItem);
	}

	private async Task RemoveItemFromCart(RecipeItemCartModel cartItem)
	{
		_recipeItems.Remove(cartItem);

		if (_sfCartGrid is not null)
			await _sfCartGrid?.Refresh();

		StateHasChanged();
	}
	#endregion

	#region Saving
	private async Task DeleteRecipe()
	{
		if (_isProcessing || _recipe is null || _recipe.Id == 0 || _isLoading)
			return;

		try
		{
			await _toastNotification.ShowAsync("Processing Transaction", "Please wait while the transaction is being deleted...", ToastType.Info);

			await RecipeData.DeleteRecipe(_recipe);
			ResetPage();
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error Deleting Recipe", ex.Message, ToastType.Error);
		}
		finally
		{
			if (_sfCartGrid is not null)
				await _sfCartGrid?.Refresh();

			_isProcessing = false;
			StateHasChanged();
		}
	}

	private async Task OnSaveButtonClick()
	{
		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();

			if (_selectedProduct is null || _selectedProduct.Id == 0)
				throw new Exception("No product selected. Please select a product before saving the recipe.");

			if (_recipeItems.Count == 0)
				throw new Exception("Please add at least one raw material to the recipe before saving");

			if (_recipe.Quantity <= 0)
				throw new Exception("Quantity must be greater than zero");

			await _toastNotification.ShowAsync("Processing Transaction", "Please wait while the transaction is being saved...", ToastType.Info);

			await RecipeData.SaveRecipe(new()
			{
				Id = _recipe?.Id ?? 0,
				ProductId = _selectedProduct.ProductId,
				Quantity = _recipe?.Quantity ?? 0,
				Deduct = _recipe?.Deduct ?? true,
				Status = true,
			}, _recipeItems);

			await _toastNotification.ShowAsync("Recipe Saved", $"Recipe saved successfully for {_selectedProduct.Name} with {_recipeItems.Count} items!", ToastType.Success);

			ResetPage();
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error Saving Recipe", ex.Message, ToastType.Error);
		}
		finally
		{
			if (_sfCartGrid is not null)
				await _sfCartGrid?.Refresh();

			_isProcessing = false;
			StateHasChanged();
		}
	}
	#endregion

	#region Export
	private async Task DownloadPdfInvoice()
	{
		if (_recipe is null || _recipe.Id == 0)
		{
			await _toastNotification.ShowAsync("No Transaction Selected", "Please save the transaction first before downloading the invoice.", ToastType.Error);
			return;
		}

		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();
			await _toastNotification.ShowAsync("Processing", "Generating PDF invoice...", ToastType.Info);

			var (pdfStream, fileName) = await RecipeInvoiceExport.ExportInvoice(_recipe.Id, InvoiceExportType.PDF, _recipeDateTime);
			await SaveAndViewService.SaveAndView(fileName, pdfStream);

			await _toastNotification.ShowAsync("Invoice Downloaded", "The PDF invoice has been downloaded successfully.", ToastType.Success);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("An Error Occurred While Downloading Invoice", ex.Message, ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
		}
	}

	private async Task DownloadExcelInvoice()
	{
		if (_recipe is null || _recipe.Id == 0)
		{
			await _toastNotification.ShowAsync("No Transaction Selected", "Please save the transaction first before downloading the invoice.", ToastType.Error);
			return;
		}

		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();
			await _toastNotification.ShowAsync("Processing", "Generating Excel invoice...", ToastType.Info);

			var (excelStream, fileName) = await RecipeInvoiceExport.ExportInvoice(_recipe.Id, InvoiceExportType.Excel, _recipeDateTime);
			await SaveAndViewService.SaveAndView(fileName, excelStream);

			await _toastNotification.ShowAsync("Invoice Downloaded", "The Excel invoice has been downloaded successfully.", ToastType.Success);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("An Error Occurred While Downloading Invoice", ex.Message, ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
		}
	}
	#endregion

	#region Utilities
	private async Task OnMenuSelected(Syncfusion.Blazor.Navigations.MenuEventArgs<Syncfusion.Blazor.Navigations.MenuItem> args)
	{
		switch (args.Item.Id)
		{
			case "NewRecipe":
				ResetPage();
				break;
			case "SaveRecipe":
				await OnSaveButtonClick();
				break;
			case "ExportPdfInvoice":
				await DownloadPdfInvoice();
				break;
			case "ExportExcelInvoice":
				await DownloadExcelInvoice();
				break;
			case "DeleteRecipe":
				await DeleteRecipe();
				break;
			case "EditSelectedRecipeItem":
				await EditSelectedCartItem();
				break;
			case "DeleteSelectedRecipeItem":
				await RemoveSelectedCartItem();
				break;
		}
	}

	private async Task OnRecipeGridContextMenuItemClicked(ContextMenuClickEventArgs<RecipeItemCartModel> args)
	{
		switch (args.Item.Id)
		{
			case "EditRecipeItem":
				await EditSelectedCartItem();
				break;
			case "DeleteRecipeItem":
				await RemoveSelectedCartItem();
				break;
		}
	}

	private void ResetPage() =>
		NavigationManager.NavigateTo(PageRouteNames.Recipe, true);

	private void NavigateBack() =>
		NavigationManager.NavigateTo(PageRouteNames.InventoryDashboard);
	#endregion
}
