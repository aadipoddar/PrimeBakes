using PrimeBakesLibrary.Data.Restaurant.Bill;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Models.Operations;
using PrimeBakesLibrary.Models.Restuarant.Bill;
using PrimeBakesLibrary.Models.Restuarant.Dining;

namespace PrimeBakes.Shared.Pages.Restaurant.Bill.Mobile;

public partial class DiningMobileDashbaord : IAsyncDisposable
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
			.Add(ModCode.Ctrl, Code.B, NavigateBack, "Back", Exclude.None);

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

	private void NavigateBack() =>
		NavigationManager.NavigateTo(PageRouteNames.RestaurantDashboard);

	private void OpenBillPage(int diningTableId) =>
		NavigationManager.NavigateTo($"{PageRouteNames.BillMobile}/table/{diningTableId}");

	public ValueTask DisposeAsync()
	{
		GC.SuppressFinalize(this);
		return ((IAsyncDisposable)HotKeys).DisposeAsync();
	}
}
