using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace PrimeBakes.Shared.Components.Button;

public partial class FioriTile
{
	[Parameter] public string Title { get; set; } = string.Empty;
	[Parameter] public string Subtitle { get; set; } = string.Empty;
	[Parameter] public string? CustomColor { get; set; }
	[Parameter] public RenderFragment? IconContent { get; set; }
	[Parameter] public string KPI { get; set; } = string.Empty;
	[Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

	// Shrink the KPI font as the number gets longer so lakh/crore values
	// (e.g. "₹1,23,45,678.00") still fit inside the tile instead of overflowing.
	private string ValueSizeClass => (KPI?.Length ?? 0) switch
	{
		> 15 => "ft-value--xs",
		> 12 => "ft-value--sm",
		> 9 => "ft-value--md",
		_ => string.Empty
	};
}
