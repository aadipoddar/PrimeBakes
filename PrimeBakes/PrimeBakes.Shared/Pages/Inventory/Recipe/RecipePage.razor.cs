using PrimeBakes.Library.Inventory.Purchase.Data;
using PrimeBakes.Library.Inventory.RawMaterial.Models;
using PrimeBakes.Library.Inventory.Recipe.Data;
using PrimeBakes.Library.Inventory.Recipe.Exports;
using PrimeBakes.Library.Inventory.Recipe.Models;
using PrimeBakes.Library.Operations.User;
using PrimeBakes.Library.Store.Product.Data;
using PrimeBakes.Library.Store.Product.Models;
using PrimeBakes.Library.Utils.Exports;
using PrimeBakes.Shared.Components.Dialog;
using PrimeBakes.Shared.Components.Input;

using Syncfusion.Blazor.Grids;

namespace PrimeBakes.Shared.Pages.Inventory.Recipe;

public partial class RecipePage
{
	private UserModel _user;

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
		new() { Text = "Edit (Insert)", Id = "EditCart", IconCss = "e-icons e-edit", Target = ".e-content" },
		new() { Text = "Delete (Del)", Id = "DeleteCart", IconCss = "e-icons e-trash", Target = ".e-content" }
	];

	private CustomAutoComplete<ProductLocationOverviewModel> _firstFocus;
	private CustomAutoComplete<RawMaterialModel> _itemAutoComplete;
	private SfGrid<RecipeItemCartModel> _sfCartGrid;

	private ToastNotification _toastNotification;

	#region Load Data
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		try
		{
			_user = await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, [UserRoles.Inventory], true);
			await LoadData();
		}
		catch { NavigationManager.NavigateTo(OperationRouteNames.Dashboard); }
	}

	private async Task LoadData()
	{
		_recipeDateTime = await CommonData.LoadCurrentDateTime();
		_products = await ProductLocationData.LoadProductLocationOverviewByProductLocation(LocationId: 1);
		_rawMaterials = await PurchaseData.LoadRawMaterialByPartyPurchaseDateTime(0, _recipeDateTime);

		_products = [.. _products.OrderBy(s => s.Name)];
		_rawMaterials = [.. _rawMaterials.OrderBy(s => s.Name)];

		_isLoading = false;
		StateHasChanged();

		if (_firstFocus is not null)
			await _firstFocus.FocusAsync();
	}

	private async Task LoadRecipe()
	{
		if (_isProcessing || _isLoading || _selectedProduct is null || _selectedProduct.ProductId == 0)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();

			_recipeItems.Clear();

			_recipe = await RecipeData.LoadRecipeByProduct(_selectedProduct.ProductId)
				?? new() { ProductId = _selectedProduct.ProductId, Deduct = true, Quantity = 1, Status = true };

			var recipeDetails = await CommonData.LoadTableDataByMasterId<RecipeDetailModel>(InventoryNames.RecipeDetail, _recipe.Id);

			foreach (var detail in recipeDetails)
			{
				var rawMaterial = _rawMaterials.FirstOrDefault(r => r.Id == detail.RawMaterialId);
				var amount = detail.Quantity * (rawMaterial?.Rate ?? 0);

				_recipeItems.Add(new()
				{
					ItemId = detail.RawMaterialId,
					ItemName = rawMaterial?.Name ?? "Unknown",
					Quantity = detail.Quantity,
					Rate = rawMaterial?.Rate ?? 0,
					Amount = amount,
					PerUnit = _recipe.Quantity > 0 ? amount / _recipe.Quantity : 0m
				});
			}
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error While Loading Recipe", ex.Message, ToastType.Error);
		}
		finally
		{
			if (_sfCartGrid is not null) await _sfCartGrid.Refresh();

			_isProcessing = false;
			StateHasChanged();
		}
	}
	#endregion

	#region Changed Events
	private async Task OnProductChanged(ProductLocationOverviewModel value)
	{
		_selectedProduct = value ?? _products.FirstOrDefault();
		await LoadRecipe();
	}

	private async Task OnRecipeDateChanged(DateTime value)
	{
		_recipeDateTime = value;
		_rawMaterials = await PurchaseData.LoadRawMaterialByPartyPurchaseDateTime(0, _recipeDateTime);
		_rawMaterials = [.. _rawMaterials.OrderBy(s => s.Name)];

		foreach (var item in _recipeItems)
		{
			item.Rate = _rawMaterials.FirstOrDefault(r => r.Id == item.ItemId)?.Rate ?? item.Rate;
			item.Amount = item.Quantity * item.Rate;
			item.PerUnit = _recipe.Quantity > 0 ? item.Amount / _recipe.Quantity : 0m;
		}

		if (_sfCartGrid is not null)
			await _sfCartGrid.Refresh();
		StateHasChanged();
	}

	private void OnRecipeQuantityChanged(decimal value)
	{
		_recipe.Quantity = value;

		foreach (var item in _recipeItems)
			item.PerUnit = _recipe.Quantity > 0 ? item.Amount / _recipe.Quantity : 0m;

		StateHasChanged();
	}
	#endregion

	#region Cart
	private async Task AddItemToCart()
	{
		if (_selectedRawMaterial is null || _selectedRawMaterial.Id == 0 || _selectedCart.Quantity <= 0)
		{
			await _toastNotification.ShowAsync("Invalid Item Details", "Please select a raw material and enter a quantity greater than zero.", ToastType.Error);
			return;
		}

		var existingItem = _recipeItems.FirstOrDefault(r => r.ItemId == _selectedRawMaterial.Id);
		if (existingItem is not null)
		{
			existingItem.Quantity += _selectedCart.Quantity;
			existingItem.Rate = _selectedRawMaterial.Rate;
			existingItem.Amount = existingItem.Quantity * existingItem.Rate;
			existingItem.PerUnit = _recipe.Quantity > 0 ? existingItem.Amount / _recipe.Quantity : 0m;
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

		if (_itemAutoComplete is not null)
			await _itemAutoComplete.FocusAsync();

		if (_sfCartGrid is not null)
			await _sfCartGrid.Refresh();

		StateHasChanged();
	}

	private async Task EditSelectedCartItem()
	{
		if (_sfCartGrid is null || _sfCartGrid.SelectedRecords is null || _sfCartGrid.SelectedRecords.Count == 0)
			return;

		await EditCartItem(_sfCartGrid.SelectedRecords.First());
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

		await RemoveItemFromCart(cartItem);

		if (_itemAutoComplete is not null)
			await _itemAutoComplete.FocusAsync();
		StateHasChanged();
	}

	private async Task RemoveSelectedCartItem()
	{
		if (_sfCartGrid is null || _sfCartGrid.SelectedRecords is null || _sfCartGrid.SelectedRecords.Count == 0)
			return;

		await RemoveItemFromCart(_sfCartGrid.SelectedRecords.First());
	}

	private async Task RemoveItemFromCart(RecipeItemCartModel cartItem)
	{
		_recipeItems.Remove(cartItem);

		if (_sfCartGrid is not null)
			await _sfCartGrid.Refresh();
		StateHasChanged();
	}
	#endregion

	#region Saving
	private async Task SaveTransaction(bool savePDF = false, bool saveExcel = false)
	{
		if (_isProcessing || _isLoading)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();

			await _toastNotification.ShowAsync("Processing Transaction", "Please wait while the recipe is being saved...", ToastType.Info);

			_recipe.ProductId = _selectedProduct.ProductId;
			_recipe.Status = true;

			var platform = FormFactor.GetFormFactor() + FormFactor.GetPlatform();
			var items = RecipeData.ConvertCartToDetails(_recipeItems, _recipe.Id);
			_recipe.Id = await RecipeData.SaveTransaction(_recipe, items, _user.Id, platform);

			if (savePDF) await ExportSelectedTransaction(false, true);
			if (saveExcel) await ExportSelectedTransaction(true, true);

			await _toastNotification.ShowAsync("Recipe Saved", $"Recipe saved successfully for {_selectedProduct.Name} with {_recipeItems.Count} items.", ToastType.Success);

			ResetPage();
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error While Saving Recipe", ex.Message, ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
			StateHasChanged();
		}
	}
	#endregion

	#region Exporting
	private async Task ExportSelectedTransaction(bool isExcel = false, bool force = false)
	{
		if (_recipe.Id <= 0 || (_isProcessing && !force))
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();
			await _toastNotification.ShowAsync("Processing", "Generating the Export...", ToastType.Info);

			var (stream, fileName) = await RecipeInvoiceExport.ExportInvoice(_recipe.Id, isExcel ? InvoiceExportType.Excel : InvoiceExportType.PDF, _recipeDateTime);
			await SaveAndViewService.SaveAndView(fileName, stream);

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
	private async Task OnRecipeGridContextMenuItemClicked(ContextMenuClickEventArgs<RecipeItemCartModel> args)
	{
		switch (args.Item.Id)
		{
			case "EditCart": await EditSelectedCartItem(); break;
			case "DeleteCart": await RemoveSelectedCartItem(); break;
		}
	}

	private void ResetPage() => PageRefresh.Request();
	#endregion
}
