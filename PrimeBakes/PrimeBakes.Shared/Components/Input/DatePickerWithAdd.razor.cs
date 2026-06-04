using Microsoft.AspNetCore.Components;
using PrimeBakesLibrary.Models.Accounts.Masters;
using Syncfusion.Blazor.Calendars;

namespace PrimeBakes.Shared.Components.Input;

public partial class DatePickerWithAdd
{
	private SfDatePicker<DateTime> _sfDatePicker;
	private bool _isFocused;

	private void OnFocus(object args) => _isFocused = true;
	private void OnBlur(object args) => _isFocused = false;

	[Parameter] public DateTime Value { get; set; }
	[Parameter] public EventCallback<DateTime> ValueChanged { get; set; }
	[Parameter] public EventCallback<ChangedEventArgs<DateTime>> ValueChange { get; set; }

	[Parameter] public FinancialYearModel? FinancialYear { get; set; }

	[Parameter] public string Placeholder { get; set; } = "Select Date";
	[Parameter] public bool Disabled { get; set; } = false;

	[Parameter] public string? Label { get; set; } = "Transaction Date";
	[Parameter] public bool Required { get; set; } = true;
	[Parameter] public string? AddNewRoute { get; set; } = PageRouteNames.FinancialYearMaster;
	[Parameter] public string AddNewLabel { get; set; } = "New";

	public Task FocusAsync() => _sfDatePicker.FocusAsync();

	private async Task OnValueChangeInternal(ChangedEventArgs<DateTime> args)
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
