using PrimeBakesLibrary.Restaurant.Bill.Exports;
using PrimeBakesLibrary.Restaurant.Bill.Models;

using System.Text.Json;

namespace PrimeBakes.Shared.Pages.Restaurant.Bill.Mobile;

public partial class BillMobileConfirmationPage
{
	private BillOverviewModel _bill = null;
	private List<BillItemOverviewModel> _cart = [];
	private bool _isPrinting;

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		if (await DataStorageService.LocalExists(StorageFileNames.BillMobileDataFileName))
		{
			_bill = JsonSerializer.Deserialize<BillOverviewModel>(await DataStorageService.LocalGetAsync(StorageFileNames.BillMobileDataFileName));
			_cart = await CommonData.LoadTableDataByMasterId<BillItemOverviewModel>(RestaurantNames.BillItemOverview, _bill.Id);
			_cart = [.. _cart.OrderBy(i => i.ItemName)];
		}

		await SoundService.PlaySound("checkout.mp3");
		VibrationService.VibrateWithTime(500);

		await DataStorageService.LocalRemove(StorageFileNames.BillMobileDataFileName);
		await DataStorageService.LocalRemove(StorageFileNames.BillMobileCartDataFileName);

		StateHasChanged();
	}

	private string PaymentSummary()
	{
		if (_bill is null)
			return string.Empty;

		List<string> parts = [];
		if (_bill.Cash > 0) parts.Add($"Cash ₹{_bill.Cash:N0}");
		if (_bill.UPI > 0) parts.Add($"UPI ₹{_bill.UPI:N0}");
		if (_bill.Card > 0) parts.Add($"Card ₹{_bill.Card:N0}");
		if (_bill.Credit > 0) parts.Add($"Credit ₹{_bill.Credit:N0}");

		var table = string.IsNullOrWhiteSpace(_bill.DiningTableName) ? "Table" : _bill.DiningTableName;

		return parts.Count > 0 ? $"{table} closed · {string.Join(" · ", parts)}" : $"{table} closed.";
	}

	private async Task PrintThermal()
	{
		if (_bill is null || _isPrinting)
			return;

		try
		{
			_isPrinting = true;
			StateHasChanged();

			await ThermalPrintDispatcher.PrintAsync(
				() => BillThermalPrint.GenerateThermalBill(_bill.Id),
				() => BillThermalPrint.GenerateThermalBillPng(_bill.Id));
		}
		finally
		{
			_isPrinting = false;
			StateHasChanged();
		}
	}
}
