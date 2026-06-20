using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

using PrimeBakesLibrary.Operations.Location;
using PrimeBakesLibrary.Store.Product.Data;
using PrimeBakesLibrary.Store.Product.Models;

using System.Globalization;

namespace PrimeBakes.Shared.Pages.Restaurant.Menu;

public partial class GuestMenuPage : IAsyncDisposable
{
	[Parameter] public int LocationId { get; set; }

	private bool _isLoading = true;

	private LocationModel _location;
	private List<ProductCategoryModel> _categories = [];
	private List<ProductLocationOverviewModel> _items = [];

	private string _query = string.Empty;
	private readonly HashSet<string> _diets = [];

	private IJSObjectReference _module;

	private bool IsFiltering => !string.IsNullOrWhiteSpace(_query) || _diets.Count > 0;

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender)
		{
			await LoadData();
			_isLoading = false;
			StateHasChanged();
		}
		else if (_module is null && _categories.Count > 0)
		{
			_module = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/PrimeBakes.Shared/js/guestMenu.js");
			await _module.InvokeVoidAsync("init");
		}
	}

	private async Task LoadData()
	{
		try
		{
			_location = await CommonData.LoadTableDataById<LocationModel>(OperationNames.Location, LocationId);

			var categories = await CommonData.LoadTableDataByStatus<ProductCategoryModel>(StoreNames.ProductCategory);
			_items = [.. (await ProductLocationData.LoadProductLocationOverviewByProductLocation(LocationId: LocationId)).Where(p => p.ShowInMenu).OrderBy(p => p.Name)];

			// Keep only categories that have items at this outlet, in name order.
			var categoryIds = _items.Select(p => p.ProductCategoryId).ToHashSet();
			_categories = [.. categories.Where(c => categoryIds.Contains(c.Id)).OrderBy(c => c.Name)];
		}
		catch
		{
			_location = null;
			_categories = [];
			_items = [];
		}
	}

	// Items for a category that match the current search + diet filters.
	private List<ProductLocationOverviewModel> ItemsFor(ProductCategoryModel category)
	{
		var q = _query.Trim();
		return [.. _items.Where(p => p.ProductCategoryId == category.Id
			&& (q.Length == 0 || (p.Name?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false))
			&& (_diets.Count == 0 || _diets.Contains(DietCode(p.FoodType))))];
	}

	private void OnQueryChange(ChangeEventArgs e) => _query = e.Value?.ToString() ?? string.Empty;

	private void ClearQuery() => _query = string.Empty;

	private void ClearDiets() => _diets.Clear();

	private void ToggleDiet(string code)
	{
		if (!_diets.Remove(code))
			_diets.Add(code);
	}

	private async Task JumpToCategory(int categoryId)
	{
		if (_module is not null)
			await _module.InvokeVoidAsync("jumpTo", $"pb-cat-{categoryId}");
	}

	private async Task ScrollToTop()
	{
		if (_module is not null)
			await _module.InvokeVoidAsync("scrollToTop");
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

	public async ValueTask DisposeAsync()
	{
		if (_module is not null)
			try { await _module.DisposeAsync(); } catch { }

		GC.SuppressFinalize(this);
	}
}
