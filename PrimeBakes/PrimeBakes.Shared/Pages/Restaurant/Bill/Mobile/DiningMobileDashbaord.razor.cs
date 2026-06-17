using PrimeBakesLibrary.Operations.User;
using PrimeBakesLibrary.Restaurant.Bill.Data;
using PrimeBakesLibrary.Restaurant.Bill.Models;
using PrimeBakesLibrary.Restaurant.Dining.Models;

namespace PrimeBakes.Shared.Pages.Restaurant.Bill.Mobile;

public partial class DiningMobileDashbaord
{
	private UserModel _user;
	private bool _isLoading = true;
	private DateTime _now = DateTime.Now;

	private List<DiningAreaModel> _diningAreas = [];
	private List<DiningTableModel> _diningTables = [];
	private List<BillModel> _runningBills = [];

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		_user = await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, [UserRoles.Restaurant]);

		await LoadData();

		_isLoading = false;
		StateHasChanged();
	}

	private async Task LoadData()
	{
		_now = await CommonData.LoadCurrentDateTime();

		_diningAreas = await CommonData.LoadTableDataByStatus<DiningAreaModel>(RestaurantNames.DiningArea);
		_diningAreas = [.. _diningAreas.Where(area => area.LocationId == _user.LocationId).OrderBy(area => area.Name)];

		_diningTables = await CommonData.LoadTableDataByStatus<DiningTableModel>(RestaurantNames.DiningTable);
		_diningTables = [.. _diningTables.Where(dt => _diningAreas.Any(area => area.Id == dt.DiningAreaId)).OrderBy(dt => dt.Name)];

		_runningBills = await BillData.LoadRunningBillByLocationId(_user.LocationId);
	}

	private string Elapsed(DateTime since)
	{
		var span = _now - since;
		if (span < TimeSpan.Zero)
			span = TimeSpan.Zero;

		if (span.TotalHours >= 1)
			return $"{(int)span.TotalHours}h {span.Minutes}m";

		return $"{(int)span.TotalMinutes}m";
	}

	private void NavigateBack() =>
		NavigationManager.NavigateTo(RestaurantRouteNames.RestaurantDashboard);

	private void OpenBillPage(int diningTableId) =>
		NavigationManager.NavigateTo($"{RestaurantRouteNames.BillMobile}/table/{diningTableId}");
}
