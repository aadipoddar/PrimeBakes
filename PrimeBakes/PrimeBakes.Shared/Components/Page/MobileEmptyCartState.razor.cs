using Microsoft.AspNetCore.Components;

namespace PrimeBakes.Shared.Components.Page;

public partial class MobileEmptyCartState
{
	[Parameter]
	public string Title { get; set; } = "Your cart is empty";

	[Parameter]
	public string Message { get; set; } = "Add some items to your cart to continue";

	[Parameter]
	public string ButtonText { get; set; } = "Continue Shopping";

	[Parameter]
	public string NavigateTo { get; set; } = string.Empty;

	[Parameter]
	public bool Disabled { get; set; }

	private void Navigate()
	{
		if (!string.IsNullOrWhiteSpace(NavigateTo))
			NavigationManager.NavigateTo(NavigateTo);
	}
}