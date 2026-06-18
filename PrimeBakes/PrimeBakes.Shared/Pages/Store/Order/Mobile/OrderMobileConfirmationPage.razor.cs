using PrimeBakesLibrary.Store.Order.Exports;
using PrimeBakesLibrary.Store.Order.Models;
using PrimeBakesLibrary.Utils.Exports;

namespace PrimeBakes.Shared.Pages.Store.Order.Mobile;

public partial class OrderMobileConfirmationPage
{
	private OrderOverviewModel _order = null;
	private List<OrderItemCartModel> _cart = [];
	private bool _isPrinting;

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		if (await DataStorageService.LocalExists(StorageFileNames.OrderMobileDataFileName))
		{
			_order = System.Text.Json.JsonSerializer.Deserialize<OrderOverviewModel>(await DataStorageService.LocalGetAsync(StorageFileNames.OrderMobileDataFileName));
			_cart = System.Text.Json.JsonSerializer.Deserialize<List<OrderItemCartModel>>(await DataStorageService.LocalGetAsync(StorageFileNames.OrderMobileCartDataFileName)) ?? [];
			_cart = [.. _cart.OrderBy(i => i.ItemName)];
		}

		await SoundService.PlaySound("checkout.mp3");
		VibrationService.VibrateWithTime(500);

		await DataStorageService.LocalRemove(StorageFileNames.OrderMobileDataFileName);
		await DataStorageService.LocalRemove(StorageFileNames.OrderMobileCartDataFileName);

		StateHasChanged();
	}

	private async Task PrintInvoice()
	{
		if (_order is null || _isPrinting)
			return;

		try
		{
			_isPrinting = true;
			StateHasChanged();

			var (pdfStream, fileName) = await OrderInvoiceExport.ExportInvoice(_order.Id, InvoiceExportType.PDF);
			await SaveAndViewService.SaveAndView(fileName, pdfStream);
		}
		finally
		{
			_isPrinting = false;
			StateHasChanged();
		}
	}
}
