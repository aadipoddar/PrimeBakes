namespace PrimeBakes.Shared.Pages.Restaurant.Bill.Mobile;

public partial class BillMobileConfirmationPage
{
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		await SoundService.PlaySound("checkout.mp3");
		VibrationService.VibrateWithTime(500);

		// Wait for 2 seconds then navigate to home
		await Task.Delay(2000);
		NavigationManager.NavigateTo(PageRouteNames.DiningMobileDashboard, forceLoad: true);
	}
}