using PrimeBakes.Shared.Components.Dialog;
using PrimeBakes.Shared.Components.Input;
using PrimeBakesLibrary.Data.Inventory;
using PrimeBakesLibrary.Data.Store.Product;
using PrimeBakesLibrary.DataAccess;
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

	private decimal _selectedRawMaterialQuantity;

	private ProductLocationOverviewModel _selectedProduct;
	private RawMaterialModel _selectedRawMaterial;
	private RecipeModel _recipe;

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
			_products = await ProductLocationData.LoadProductLocationOverviewByProductLocation(LocationId: 1);
			_rawMaterials = await CommonData.LoadTableData<RawMaterialModel>(TableNames.RawMaterial);
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
			if (_recipe is null)
				return;

			var recipeDetails = await CommonData.LoadTableDataByMasterId<RecipeDetailModel>(TableNames.RecipeDetail, _recipe.Id);
			if (recipeDetails is null || recipeDetails.Count == 0)
				return;

			_recipeItems.Clear();

			foreach (var detail in recipeDetails)
			{
				var rawMaterial = await CommonData.LoadTableDataById<RawMaterialModel>(TableNames.RawMaterial, detail.RawMaterialId);

				_recipeItems.Add(new()
				{
					ItemId = detail.RawMaterialId,
					ItemName = rawMaterial.Name,
					Quantity = detail.Quantity
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
	private async Task AddItemToCart()
	{
		if (_selectedRawMaterial is null || _selectedRawMaterial.Id == 0 || _selectedRawMaterialQuantity <= 0)
			return;

		var existingRecipe = _recipeItems.FirstOrDefault(r => r.ItemId == _selectedRawMaterial.Id);
		if (existingRecipe is not null)
			existingRecipe.Quantity += _selectedRawMaterialQuantity;
		else
			_recipeItems.Add(new()
			{
				ItemId = _selectedRawMaterial.Id,
				ItemName = _selectedRawMaterial.Name,
				Quantity = _selectedRawMaterialQuantity,
			});

		_selectedRawMaterial = null;
		_selectedRawMaterialQuantity = 0;

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
		_selectedRawMaterialQuantity = cartItem.Quantity;
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
			{
				await _toastNotification.ShowAsync("No Product Selected", "Please select a product to save the recipe for", ToastType.Warning);
				return;
			}

			if (_recipeItems.Count == 0)
			{
				await _toastNotification.ShowAsync("No Raw Materials Added", "Please add at least one raw material to the recipe", ToastType.Warning);
				return;
			}

			await _toastNotification.ShowAsync("Processing Transaction", "Please wait while the transaction is being saved...", ToastType.Info);

			await RecipeData.SaveRecipe(new()
			{
				Id = _recipe?.Id ?? 0,
				ProductId = _selectedProduct.ProductId,
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
