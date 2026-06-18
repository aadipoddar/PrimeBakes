using Microsoft.AspNetCore.Components;

using MudBlazor;

using System.Globalization;

namespace PrimeBakes.Shared.Components.Input;

public partial class CustomNumericField<T>
{
	private MudNumericField<T> _numericField;
	private bool _isFocused;

	private void OnFocusIn() => _isFocused = true;
	private void OnFocusOut() => _isFocused = false;

	[Parameter] public T Value { get; set; }
	[Parameter] public EventCallback<T> ValueChanged { get; set; }

	[Parameter] public string Label { get; set; }
	[Parameter] public string Placeholder { get; set; }
	[Parameter] public bool Required { get; set; } = true;
	[Parameter] public bool ReadOnly { get; set; } = false;
	[Parameter] public string AddNewRoute { get; set; }

	// Min/Max/Step are forwarded only when the caller actually set them (see
	// SetParametersAsync). This project has no nullable context and T has no
	// struct constraint, so a [Parameter] T Min would default to 0 for int;
	// always forwarding that makes MudNumericField clamp Max to 0 and snap
	// every entered value back to 0. Splatting only the supplied keys lets
	// MudNumericField apply its own defaults (type min/max, Step = 1).
	[Parameter] public T Min { get; set; }
	[Parameter] public T Max { get; set; }
	[Parameter] public T Step { get; set; }
	private readonly Dictionary<string, object> _rangeAttributes = [];
	[Parameter] public string Format { get; set; } = "N2";
	[Parameter] public CultureInfo Culture { get; set; } = CultureInfo.CurrentCulture;
	[Parameter] public bool HideSpinButtons { get; set; } = true;
	[Parameter] public int TabIndex { get; set; } = 0;

	// MudNumericField reformats its text on every keystroke when Immediate is
	// true, which resets the caret and prevents typing more than one digit.
	// Default to false so the value is committed on change/blur instead.
	[Parameter] public bool Immediate { get; set; } = false;

	[Parameter] public string AdornmentText { get; set; }
	[Parameter] public string AdornmentIcon { get; set; }
	[Parameter] public string AdornmentAriaLabel { get; set; }
	[Parameter] public EventCallback OnAdornmentClick { get; set; }

	private bool ShowAddNew => AddNewRoute is not null;

	public override Task SetParametersAsync(ParameterView parameters)
	{
		foreach (var parameter in parameters)
		{
			if (parameter.Name is nameof(Min) or nameof(Max) or nameof(Step))
				_rangeAttributes[parameter.Name] = parameter.Value;
		}

		return base.SetParametersAsync(parameters);
	}

	public ValueTask FocusAsync() =>
		_numericField is null ? ValueTask.CompletedTask : _numericField.FocusAsync();

	private async Task OnValueChangedInternal(T value)
	{
		Value = value;
		await ValueChanged.InvokeAsync(value);
	}

	private async Task NavigateToAddNew()
	{
		if (AddNewRoute is not null)
			await AuthenticationService.NavigateToRoute(AddNewRoute, FormFactor, JSRuntime, NavigationManager);
	}
}
