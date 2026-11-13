using Microsoft.AspNetCore.Components;

namespace PrimeBakes.Shared.Components.Dialog;

public partial class PageRecommendationBanner
{
	private List<string> _recommendations = [];
	private bool _dismissed;

	public async Task ShowAsync(List<string> recommendations)
	{
		_recommendations =
			[.. (recommendations ?? [])
		.Where(route => !string.IsNullOrWhiteSpace(route))
		.Take(2)];

		_dismissed = false;
		await InvokeAsync(StateHasChanged);
	}

	private static string GetPageLabel(string route)
	{
		var label = route.Trim('/').Split('/').LastOrDefault()?.Replace('-', ' ');
		return string.IsNullOrWhiteSpace(label)
			? "Open page"
			: System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(label);
	}

	private void Dismiss() => _dismissed = true;

	private void OpenPage(string route)
	{
		if (string.IsNullOrWhiteSpace(route)) return;
		_dismissed = true; // hide the banner after navigating
		NavigationManager.NavigateTo(route);
	}
}
