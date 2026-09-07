using PrimeBakes.Models.DataAccess;
using PrimeBakes.Models.Operations.OfflineQueue;
using PrimeBakes.Models.Operations.User;
using PrimeBakes.Shared.Components.Dialog;

using Syncfusion.Blazor.Grids;

namespace PrimeBakes.Shared.Pages.Operations;

public partial class OfflineQueuePage
{
	private UserModel _user;

	private bool _isLoading = true;
	private bool _isProcessing = false;

	private List<OfflineQueueModel> _offlineQueues = [];

	private SfGrid<OfflineQueueModel> _sfGrid;
	private ToastNotification _toastNotification;

	#region Load Data
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		try
		{
			if (!OfflineState.LocalDBAvailable)
				throw new InvalidOperationException("The offline queue is only available on a machine with a local database.");

			_user = await AuthService.ValidateUser();
			await InitializePage();
		}
		catch { NavigationManager.NavigateTo(OperationRouteNames.Dashboard); }
	}

	private async Task InitializePage()
	{
		await LoadOfflineQueues();

		_isLoading = false;
		StateHasChanged();
	}

	private async Task LoadOfflineQueues()
	{
		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();
			await _toastNotification.ShowAsync("Loading", "Loading the Offline Queue...", ToastType.Info);

			_offlineQueues = [.. (await CommonData.LoadTableData<OfflineQueueModel>(OperationNames.OfflineQueue, useLocalDB: true))
				.OrderBy(offlineQueue => offlineQueue.Id)];

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
			await _toastNotification.HideAllInfoAsync();
		}
	}
	#endregion
}
