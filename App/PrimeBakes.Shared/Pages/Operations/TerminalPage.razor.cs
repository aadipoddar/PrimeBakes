using PrimeBakes.Exports.Operations.Terminal;
using PrimeBakes.Models.Operations.Terminal;
using PrimeBakes.Models.Operations.User;
using PrimeBakes.Shared.Components.Dialog;

using Syncfusion.Blazor.Grids;

namespace PrimeBakes.Shared.Pages.Operations;

public partial class TerminalPage
{
	private UserModel _user;

	private bool _isLoading = true;
	private bool _isProcessing = false;

	private List<TerminalOverviewModel> _terminals = [];

	private SfGrid<TerminalOverviewModel> _sfGrid;
	private ToastNotification _toastNotification;

	#region Load Data
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		try
		{
			_user = await AuthService.ValidateUser([UserRoles.Admin], true);
			await InitializePage();
		}
		catch { NavigationManager.NavigateTo(OperationRouteNames.Dashboard); }
	}

	private async Task InitializePage()
	{
		await LoadTerminals();

		_isLoading = false;
		StateHasChanged();
	}

	private async Task LoadTerminals()
	{
		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();
			await _toastNotification.ShowAsync("Loading", "Loading the Terminals...", ToastType.Info);

			_terminals = [.. (await CommonData.LoadTableData<TerminalOverviewModel>(OperationNames.TerminalOverview))
				.OrderBy(terminal => terminal.TerminalNo)];

			if (_sfGrid is not null)
				await _sfGrid.Refresh();

			StateHasChanged();
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", ex.Message, ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
			StateHasChanged();
		}
	}
	#endregion

	#region Exporting
	private async Task ExportReport(bool isExcel = false)
	{
		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();
			await _toastNotification.ShowAsync("Processing", "Generating the Export...", ToastType.Info);

			var (stream, fileName) = TerminalExport.ExportReport(
				_terminals,
				await CommonData.LoadCurrentDateTime(),
				isExcel ? ReportExportType.Excel : ReportExportType.PDF
			);
			await SaveAndViewService.SaveAndView(fileName, stream);

			await _toastNotification.ShowAsync("Exported", "The export has been downloaded successfully.", ToastType.Success);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error While Exporting", ex.Message, ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
			StateHasChanged();
		}
	}
	#endregion
}
