using Microsoft.AspNetCore.Components;

using PrimeBakes.Library.Operations.Location;
using PrimeBakes.Library.Store.Product.Data;
using PrimeBakes.Library.Store.Product.Models;

using System.Globalization;

namespace PrimeBakes.Shared.Pages.Restaurant.Menu;

public partial class GuestMenuPage
{
	[Parameter] public int LocationId { get; set; }

	private bool _isLoading = true;

	private LocationModel _location;
	private List<ProductCategoryModel> _categories = [];
	private List<ProductLocationOverviewModel> _items = [];

	private ProductCategoryModel _selectedCategory;
	private string _query = string.Empty;
	private readonly HashSet<string> _diets = [];

	private bool IsFiltering => !string.IsNullOrWhiteSpace(_query) || _diets.Count > 0;

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		await LoadData();
		_isLoading = false;
		StateHasChanged();
	}

	private async Task LoadData()
	{
		try
		{
			_location = await CommonData.LoadTableDataById<LocationModel>(OperationNames.Location, LocationId);

			var categories = await CommonData.LoadTableDataByStatus<ProductCategoryModel>(StoreNames.ProductCategory);
			_items = [.. (await ProductLocationData.LoadProductLocationOverviewByProductLocationDate(null, LocationId, DateOnly.FromDateTime(await CommonData.LoadCurrentDateTime())))
				.Where(p => p.ShowInMenu)
				.DistinctBy(p => p.ProductId)
				.OrderBy(p => p.Name)];

			// Keep only categories that have items at this outlet, in name order, with an "All" tab first.
			var categoryIds = _items.Select(p => p.ProductCategoryId).ToHashSet();
			_categories = [.. categories.Where(c => c.ShowInMenu && categoryIds.Contains(c.Id)).OrderBy(c => c.Name)];
			if (_categories.Count > 0)
				_categories.Insert(0, new() { Id = 0, Name = "All" });
			_selectedCategory = _categories.FirstOrDefault();
		}
		catch
		{
			_location = null;
			_categories = [];
			_items = [];
			_selectedCategory = null;
		}
	}

	// Items for the selected category that match the current search + diet filters.
	private List<ProductLocationOverviewModel> GetItems()
	{
		var q = _query.Trim();
		return [.. _items.Where(p => (_selectedCategory is null || _selectedCategory.Id == 0 || p.ProductCategoryId == _selectedCategory.Id)
			&& (q.Length == 0 || (p.Name?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false))
			&& (_diets.Count == 0 || _diets.Contains(DietCode(p.FoodType))))];
	}

	private void SelectCategory(ProductCategoryModel category) => _selectedCategory = category;

	private void OnQueryChange(ChangeEventArgs e) => _query = e.Value?.ToString() ?? string.Empty;

	private void ClearQuery() => _query = string.Empty;

	private void ClearDiets() => _diets.Clear();

	private void ToggleDiet(string code)
	{
		if (!_diets.Remove(code))
			_diets.Add(code);
	}

	// FoodType -> diet mark code (v/e/n, or x when not set).
	private static string DietCode(string foodType) => foodType switch
	{
		"Veg" => "v",
		"Egg" => "e",
		"Non-Veg" => "n",
		_ => "x",
	};

	private static string Money(decimal value) => "₹" + value.ToString("N0", CultureInfo.GetCultureInfo("en-IN"));
}
