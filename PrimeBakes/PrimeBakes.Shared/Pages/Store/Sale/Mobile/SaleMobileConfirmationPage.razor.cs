using PrimeBakesLibrary.Store.Sale.Exports;
using PrimeBakesLibrary.Store.Sale.Models;

using System.Text.Json;

namespace PrimeBakes.Shared.Pages.Store.Sale.Mobile;

public partial class SaleMobileConfirmationPage
{
	private SaleOverviewModel _sale = null;
	private List<SaleItemCartModel> _cart = [];
	private bool _isPrinting;

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		if (await DataStorageService.LocalExists(StorageFileNames.SaleMobileDataFileName))
		{
			_sale = JsonSerializer.Deserialize<SaleOverviewModel>(await DataStorageService.LocalGetAsync(StorageFileNames.SaleMobileDataFileName));
			_cart = JsonSerializer.Deserialize<List<SaleItemCartModel>>(await DataStorageService.LocalGetAsync(StorageFileNames.SaleMobileCartDataFileName)) ?? [];
		}

		await SoundService.PlaySound("checkout.mp3");
		VibrationService.VibrateWithTime(500);

		StateHasChanged();
	}

	private string PaymentSummary()
	{
		if (_sale is null)
			return string.Empty;

		List<string> parts = [];
		if (_sale.Cash > 0) parts.Add($"Cash ₹{_sale.Cash:N0}");
		if (_sale.UPI > 0) parts.Add($"UPI ₹{_sale.UPI:N0}");
		if (_sale.Card > 0) parts.Add($"Card ₹{_sale.Card:N0}");

		return parts.Count > 0 ? $"Paid in full · {string.Join(" · ", parts)}" : "Sale recorded.";
	}

	private async Task PrintThermal()
	{
		if (_sale is null || _isPrinting)
			return;

		try
		{
			_isPrinting = true;
			StateHasChanged();

			await ThermalPrintDispatcher.PrintAsync(
				() => SaleThermalPrint.GenerateThermalBill(_sale.Id),
				() => SaleThermalPrint.GenerateThermalBillPng(_sale.Id));
		}
		finally
		{
			_isPrinting = false;
			StateHasChanged();
		}
	}

	private async Task StartNewSale()
	{
		await DataStorageService.LocalRemove(StorageFileNames.SaleMobileDataFileName);
		await DataStorageService.LocalRemove(StorageFileNames.SaleMobileCartDataFileName);

		NavigationManager.NavigateTo(StoreRouteNames.SaleMobile);
	}

	private async Task GoHome()
	{
		await DataStorageService.LocalRemove(StorageFileNames.SaleMobileDataFileName);
		await DataStorageService.LocalRemove(StorageFileNames.SaleMobileCartDataFileName);

		NavigationManager.NavigateTo(OperationRouteNames.Dashboard);
	}
}
