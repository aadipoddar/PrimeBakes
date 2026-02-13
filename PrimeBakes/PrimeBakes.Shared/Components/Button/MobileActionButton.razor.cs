using Microsoft.AspNetCore.Components;

namespace PrimeBakes.Shared.Components.Button;

public partial class MobileActionButton
{
	[Parameter]
	public string CssClass { get; set; } = string.Empty;

	[Parameter]
	public string Text { get; set; } = string.Empty;

	[Parameter]
	public string IconCssClass { get; set; } = string.Empty;

	[Parameter]
	public string TextCssClass { get; set; } = string.Empty;

	[Parameter]
	public string BadgeCssClass { get; set; } = "cart-count";

	[Parameter]
	public int? BadgeCount { get; set; }

	[Parameter]
	public bool Disabled { get; set; }

	[Parameter]
	public string Title { get; set; } = string.Empty;

	[Parameter]
	public string Type { get; set; } = "button";

	[Parameter]
	public bool StopPropagation { get; set; }

	[Parameter]
	public EventCallback OnClick { get; set; }
}