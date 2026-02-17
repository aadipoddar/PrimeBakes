using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Restaurant.Bill;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Models.Operations;
using PrimeBakesLibrary.Models.Restuarant.Bill;
using PrimeBakesLibrary.Models.Restuarant.Dining;

namespace PrimeBakes.Shared.Pages.Restaurant.Dining;

public partial class DiningDashbaord : IAsyncDisposable
{
	private HotKeysContext _hotKeysContext;
	private UserModel _user;
	private bool _isLoading = true;

	private List<DiningAreaModel> _diningAreas = [];
	private List<DiningTableModel> _diningTables = [];
	private List<BillModel> _runningBills = [];

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		_user = await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, [UserRoles.Restaurant]);

		_hotKeysContext = HotKeys.CreateContext()
			.Add(ModCode.Ctrl, Code.L, Logout, "Logout", Exclude.None)
			.Add(ModCode.Ctrl, Code.B, NavigateBack, "Back", Exclude.None)
			.Add(ModCode.Ctrl, Code.D, NavigateToDashboard, "Dashboard", Exclude.None);

		await LoadData();

		_isLoading = false;
		StateHasChanged();
	}

	private async Task LoadData()
	{
		_diningAreas = await CommonData.LoadTableDataByStatus<DiningAreaModel>(TableNames.DiningArea);
		_diningAreas = [.. _diningAreas.Where(area => area.LocationId == _user.LocationId).OrderBy(area => area.Name)];

		_diningTables = await CommonData.LoadTableDataByStatus<DiningTableModel>(TableNames.DiningTable);
		_diningTables = [.. _diningTables.Where(dt => _diningAreas.Any(area => area.Id == dt.DiningAreaId)).OrderBy(dt => dt.Name)];

		_runningBills = await BillData.LoadRunningBillByLocationId(_user.LocationId);
	}

	private void NavigateToDashboard() =>
		NavigationManager.NavigateTo(PageRouteNames.Dashboard);

	private void NavigateBack() =>
		NavigationManager.NavigateTo(PageRouteNames.RestaurantDashboard);

	private void OpenBillPage(int diningTableId)
	{
		if (_runningBills.Any(bill => bill.DiningTableId == diningTableId))
		{
			
		}

		NavigationManager.NavigateTo($"{PageRouteNames.Bill}/table/{diningTableId}");
	}

	private async Task Logout() =>
		await AuthenticationService.Logout(DataStorageService, NavigationManager, NotificationService, VibrationService);

	public async ValueTask DisposeAsync()
	{
		if (_hotKeysContext is not null)
			await _hotKeysContext.DisposeAsync();

		GC.SuppressFinalize(this);
	}
}