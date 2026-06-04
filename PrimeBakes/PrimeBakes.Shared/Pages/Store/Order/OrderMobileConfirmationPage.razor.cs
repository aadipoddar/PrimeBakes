using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Exporting.Store.Order;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Store.Order;

namespace PrimeBakes.Shared.Pages.Store.Order;

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
		}

		await SoundService.PlaySound("checkout.mp3");
		VibrationService.VibrateWithTime(500);

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

	private async Task StartNewOrder()
	{
		await DataStorageService.LocalRemove(StorageFileNames.OrderMobileDataFileName);
		await DataStorageService.LocalRemove(StorageFileNames.OrderMobileCartDataFileName);

		NavigationManager.NavigateTo(PageRouteNames.OrderMobile, true);
	}

	private async Task GoHome()
	{
		await DataStorageService.LocalRemove(StorageFileNames.OrderMobileDataFileName);
		await DataStorageService.LocalRemove(StorageFileNames.OrderMobileCartDataFileName);

		NavigationManager.NavigateTo(PageRouteNames.Dashboard, true);
	}
}
