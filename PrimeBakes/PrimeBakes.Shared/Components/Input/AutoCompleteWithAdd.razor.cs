using Microsoft.AspNetCore.Components;
using Syncfusion.Blazor.DropDowns;

namespace PrimeBakes.Shared.Components.Input;

public partial class AutoCompleteWithAdd<TValue, TItem>
{
	private SfAutoComplete<TValue, TItem> _sfAutoComplete;
	private bool _isFocused;

	private void OnFocus(object args) => _isFocused = true;
	private void OnBlur(object args) => _isFocused = false;

	[Parameter] public TValue? Value { get; set; }
	[Parameter] public EventCallback<TValue?> ValueChanged { get; set; }
	[Parameter] public EventCallback<ChangeEventArgs<TValue, TItem>> ValueChange { get; set; }

	[Parameter] public IEnumerable<TItem>? DataSource { get; set; }
	[Parameter] public string Placeholder { get; set; } = "Select...";
	[Parameter] public string FieldValue { get; set; }
	[Parameter] public bool Disabled { get; set; } = false;

	[Parameter] public string? Label { get; set; }
	[Parameter] public bool Required { get; set; } = true;
	[Parameter] public string? AddNewRoute { get; set; }
	[Parameter] public string AddNewLabel { get; set; } = "New";

	public Task FocusAsync() => _sfAutoComplete.FocusAsync();

	private async Task OnValueChangeInternal(ChangeEventArgs<TValue, TItem> args)
	{
		Value = args.Value;
		await ValueChanged.InvokeAsync(args.Value);
		if (ValueChange.HasDelegate)
			await ValueChange.InvokeAsync(args);
	}

	private async Task NavigateToAddNew()
	{
		if (AddNewRoute is not null)
			await AuthenticationService.NavigateToRoute(AddNewRoute, FormFactor, JSRuntime, NavigationManager);
	}
}
