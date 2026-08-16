using Microsoft.AspNetCore.Components;

using MudBlazor;

namespace PrimeBakes.Shared.Components.Input;

public partial class CustomTextField
{
	private MudTextField<string> _textField;
	private bool _isFocused;

	private void OnFocusIn() => _isFocused = true;
	private void OnFocusOut() => _isFocused = false;

	[Parameter] public string Value { get; set; }
	[Parameter] public EventCallback<string> ValueChanged { get; set; }

	[Parameter] public string Label { get; set; }
	[Parameter] public string Placeholder { get; set; }
	[Parameter] public bool Required { get; set; } = true;
	[Parameter] public bool ReadOnly { get; set; } = false;
	[Parameter] public string AddNewRoute { get; set; }

	[Parameter] public InputType InputType { get; set; } = InputType.Text;

	[Parameter] public int Lines { get; set; } = 1;
	[Parameter] public int MaxLines { get; set; } = 5;
	[Parameter] public int MaxLength { get; set; } = 524288;
	[Parameter] public int TabIndex { get; set; } = 0;

	private bool ShowAddNew => AddNewRoute is not null;

	public ValueTask FocusAsync() =>
		_textField is null ? ValueTask.CompletedTask : _textField.FocusAsync();

	private async Task OnValueChangedInternal(string value)
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
