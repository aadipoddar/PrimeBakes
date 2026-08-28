using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

using PrimeBakes.Models.Common;
using PrimeBakes.Models.DataAccess;
using PrimeBakes.Models.Operations.User;

using PrimeBakes.Shared.Services;

using System.Text.Json;

namespace PrimeBakes.Shared.Pages.Operations;

public partial class MapPage
{
	private UserModel _user;
	private bool _isLoading = true;
	private bool _mapShown = false;

	private string _title = "Map";
	private List<MapPointModel> _points = [];

	public static async Task Open(string title, List<MapPointModel> points, IDataStorageService dataStorageService,
		IFormFactor formFactor, IJSRuntime jsRuntime, NavigationManager navigationManager)
	{
		await dataStorageService.LocalSaveAsync(StorageFileNames.MapPointsFileName,
			JsonSerializer.Serialize(new MapRequest(title, points)));

		await AuthenticationService.NavigateToRoute(OperationRouteNames.Map, formFactor, jsRuntime, navigationManager);
	}

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender)
		{
			try
			{
				_user = await AuthService.ValidateUser();
				await LoadData();
			}
			catch { NavigationManager.NavigateTo(OperationRouteNames.Dashboard); }

			return;
		}

		if (_isLoading || _mapShown)
			return;

		_mapShown = true;
		await JSRuntime.InvokeVoidAsync("locationMap.show", CommonSecrets.GoogleMapsBrowserKey,
			_points.Select(p => new { name = p.Name, lat = (double)p.Latitude, lng = (double)p.Longitude }));
	}

	private async Task LoadData()
	{
		var stored = await DataStorageService.LocalGetAsync(StorageFileNames.MapPointsFileName);
		var request = string.IsNullOrWhiteSpace(stored) ? null : JsonSerializer.Deserialize<MapRequest>(stored);
		await DataStorageService.LocalRemove(StorageFileNames.MapPointsFileName);

		if (request is not null)
		{
			_title = request.Title;
			_points = request.Points;
		}

		_isLoading = false;
		StateHasChanged();
	}

	private sealed record MapRequest(string Title, List<MapPointModel> Points);
}
