using Microsoft.AspNetCore.Components;

using PrimeBakesLibrary.Models.Store.Product;

namespace PrimeBakes.Shared.Components.Page;

public partial class MobileCategoryChipsFilter
{
    [Parameter]
    public IReadOnlyList<ProductCategoryModel> Categories { get; set; } = [];

    [Parameter]
    public ProductCategoryModel SelectedCategory { get; set; }

    [Parameter]
    public EventCallback<ProductCategoryModel> SelectedCategoryChanged { get; set; }

    private async Task SelectCategory(ProductCategoryModel category)
    {
        if (SelectedCategoryChanged.HasDelegate)
            await SelectedCategoryChanged.InvokeAsync(category);
    }
}