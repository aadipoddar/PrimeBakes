using Microsoft.AspNetCore.Components;

using MudBlazor;

namespace PrimeBakes.Shared.Components.Input;

public partial class CustomCheckBox
{
	private bool _isFocused;

	private void OnFocusIn() => _isFocused = true;
	private void OnFocusOut() => _isFocused = false;

	[Parameter] public bool Value { get; set; }
	[Parameter] public EventCallback<bool> ValueChanged { get; set; }

	[Parameter] public string Label { get; set; }

	// Accessible name for the checkbox input. Falls back to Label when not set
	// so the control is never unlabelled for assistive technology.
	[Parameter] public string AriaLabel { get; set; }

	[Parameter] public Color Color { get; set; } = Color.Primary;
	[Parameter] public Size Size { get; set; } = Size.Medium;
	[Parameter] public bool Dense { get; set; } = true;
	[Parameter] public bool Disabled { get; set; } = false;
	[Parameter] public bool ReadOnly { get; set; } = false;
	[Parameter] public bool Required { get; set; } = false;

	private async Task OnValueChangedInternal(bool value)
	{
		Value = value;
		await ValueChanged.InvokeAsync(value);
	}
}
