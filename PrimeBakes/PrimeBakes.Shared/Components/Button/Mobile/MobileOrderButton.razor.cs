using Microsoft.AspNetCore.Components;

namespace PrimeBakes.Shared.Components.Button.Mobile;

public partial class MobileOrderButton
{
	[Parameter]
	public string Id { get; set; } = "orderAnimatedBtn";

	[Parameter]
	public bool IsProcessing { get; set; }

	[Parameter]
	public bool Disabled { get; set; }

	[Parameter]
	public string DefaultText { get; set; } = "Place Order";

	[Parameter]
	public string SuccessText { get; set; } = "Order Placed";

	[Parameter]
	public EventCallback OnClick { get; set; }
}