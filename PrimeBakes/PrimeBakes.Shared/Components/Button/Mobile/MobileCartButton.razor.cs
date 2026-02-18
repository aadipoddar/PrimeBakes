using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace PrimeBakes.Shared.Components.Button.Mobile;

/// <summary>
/// Animated cart button with GSAP-powered add-to-cart animation.
/// Displays a button that plays a cart animation on click before invoking the callback.
/// </summary>
public partial class MobileCartButton
{
	/// <summary>
	/// The text displayed inside the button (e.g., "Go to Cart").
	/// </summary>
	[Parameter]
	public string Text { get; set; } = "Go to Cart";

	/// <summary>
	/// Optional badge count displayed as a circle to the right of the text.
	/// </summary>
	[Parameter]
	public int? BadgeCount { get; set; }

	/// <summary>
	/// Whether the button is disabled.
	/// </summary>
	[Parameter]
	public bool Disabled { get; set; }

	/// <summary>
	/// Callback invoked after the animation completes (or immediately if animation fails).
	/// </summary>
	[Parameter]
	public EventCallback OnClick { get; set; }

	private readonly string _instanceId = Guid.NewGuid().ToString("N")[..8];

	/// <summary>
	/// Handles the button click: plays the GSAP animation, then invokes the <see cref="OnClick"/> callback.
	/// </summary>
	private async Task OnClickHandler()
	{
		if (Disabled)
			return;

		var buttonId = $"animated-cart-btn-{_instanceId}";

		try
		{
			await JSRuntime.InvokeVoidAsync("CartButtonAnimation.play", buttonId);
		}
		catch (Exception)
		{
			// If animation fails (e.g., GSAP not loaded), proceed directly
		}

		await OnClick.InvokeAsync();
	}
}
