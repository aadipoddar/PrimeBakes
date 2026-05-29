using Microsoft.AspNetCore.Components;

using MudBlazor;

using PrimeBakesLibrary.Accounts.Masters.Models;

namespace PrimeBakes.Shared.Components.Input;

public partial class CustomDatePicker
{
	private MudDatePicker _datePicker;
	private bool _isFocused;

	private void OnFocusIn() => _isFocused = true;
	private void OnFocusOut() => _isFocused = false;

	// Required=true  → bind to these (DateTime)
	[Parameter] public DateTime Value { get; set; }
	[Parameter] public EventCallback<DateTime> ValueChanged { get; set; }

	// Required=false → bind to these (DateTime?)
	[Parameter] public DateTime? NullableValue { get; set; }
	[Parameter] public EventCallback<DateTime?> NullableValueChanged { get; set; }

	[Parameter] public FinancialYearModel FinancialYear { get; set; }
	[Parameter] public string Label { get; set; } = "Transaction Date";
	[Parameter] public bool Required { get; set; } = true;
	[Parameter] public bool ShowFinancialYear { get; set; } = true;
	[Parameter] public string AddNewRoute { get; set; } = PageRouteNames.FinancialYearMaster;

	// MudDatePicker always needs DateTime? internally
	private DateTime? DateValue => Required
		? (Value == default ? null : Value)
		: NullableValue;

	public ValueTask FocusAsync() =>
		_datePicker is null ? ValueTask.CompletedTask : _datePicker.FocusAsync();

	private async Task OnDateChangedInternal(DateTime? date)
	{
		if (Required)
		{
			if (!date.HasValue) return;
			Value = date.Value;
			await ValueChanged.InvokeAsync(Value);
		}
		else
		{
			NullableValue = date;
			await NullableValueChanged.InvokeAsync(date);
		}
	}

	private async Task NavigateToAddNew()
	{
		if (AddNewRoute is not null)
			await AuthenticationService.NavigateToRoute(AddNewRoute, FormFactor, JSRuntime, NavigationManager);
	}
}