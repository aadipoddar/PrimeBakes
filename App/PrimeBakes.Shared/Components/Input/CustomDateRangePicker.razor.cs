using Microsoft.AspNetCore.Components;

using MudBlazor;

namespace PrimeBakes.Shared.Components.Input;

public partial class CustomDateRangePicker
{
	private MudDateRangePicker _dateRangePicker;
	private bool _isFocused;

	private void OnFocusIn() => _isFocused = true;
	private void OnFocusOut() => _isFocused = false;

	[Parameter] public DateRange Value { get; set; }
	[Parameter] public EventCallback<DateRange> ValueChanged { get; set; }

	[Parameter] public string Label { get; set; } = "Date Range";
	[Parameter] public bool Required { get; set; } = true;
	[Parameter] public bool ReadOnly { get; set; } = false;

	// MudDateRangePicker needs a non-null DateRange to bind against; an unset
	// Value renders an empty (placeholder) range.
	private DateRange DateRangeValue => Value ?? new DateRange();

	public ValueTask FocusAsync() =>
		_dateRangePicker is null ? ValueTask.CompletedTask : _dateRangePicker.FocusStartAsync();

	private async Task OnDateRangeChangedInternal(DateRange range)
	{
		Value = range;
		await ValueChanged.InvokeAsync(range);
	}
}
