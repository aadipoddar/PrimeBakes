using Microsoft.AspNetCore.Components;

using PrimeBakes.Shared.Components.Input;

using PrimeBakesLibrary.Operations.User;

using System.Reflection;

namespace PrimeBakes.Shared.Components.Page;

public partial class Header
{
	#region Search
	private string _searchText = string.Empty;
	private List<GlobalSearchItem> _searchItems = [];
	private GlobalSearchItem _selectedSearchItem;
	private CustomAutoComplete<GlobalSearchItem> _globalSearch;

	private async Task FocusSearchBox()
	{
		if (_globalSearch is null)
			return;

		await _globalSearch.FocusAsync();
	}

	// Captures the typed text (used by the View/PDF transaction actions) and filters the route list.
	private Task<IEnumerable<GlobalSearchItem>> HeaderSearch(string searchText)
	{
		_searchText = searchText ?? string.Empty;

		if (string.IsNullOrWhiteSpace(searchText))
			return Task.FromResult<IEnumerable<GlobalSearchItem>>(_searchItems);

		return Task.FromResult(_searchItems.Where(x =>
			x.DisplayText.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
			x.FriendlyName.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
			x.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase)));
	}


	// TODO - Make all classes
	private void LoadRoutes() => _searchItems = [.. typeof(AccountsRouteNames)
			.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
			.Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
			.Select(f => new GlobalSearchItem
			{
				Name = f.Name,
				FriendlyName = string.Join(" ", System.Text.RegularExpressions.Regex.Split(f.Name, @"(?<!^)(?=[A-Z])")),
				Route = f.GetRawConstantValue() as string ?? string.Empty
			})
			.Select(x =>
			{
				x.DisplayText = x.FriendlyName.Equals(x.Name, StringComparison.Ordinal)
					? $"{x.Name} ({x.Route})"
					: $"{x.FriendlyName} ({x.Name}) - {x.Route}";
				return x;
			})
			.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)];

	private async Task NavigateToSearch()
	{
		if (string.IsNullOrWhiteSpace(_searchText))
			return;

		var searchText = _searchText.Trim();

		var matchingRoute = _searchItems.FirstOrDefault(x =>
			x.Route.Equals(searchText, StringComparison.OrdinalIgnoreCase) ||
			x.Name.Equals(searchText, StringComparison.OrdinalIgnoreCase) ||
			x.FriendlyName.Equals(searchText, StringComparison.OrdinalIgnoreCase) ||
			x.DisplayText.Equals(searchText, StringComparison.OrdinalIgnoreCase))
			?? _searchItems.FirstOrDefault(x =>
				x.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
				x.FriendlyName.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
				x.DisplayText.Contains(searchText, StringComparison.OrdinalIgnoreCase));

		if (matchingRoute is not null)
		{
			NavigationManager.NavigateTo(matchingRoute.Route, true);
			return;
		}

		if (await TryNavigateToDecodedTransactionAsync(searchText))
			return;

		NavigationManager.NavigateTo(searchText, true);
	}

	private async Task<bool> TryNavigateToDecodedTransactionAsync(string searchText)
	{
		try
		{
			var decodedTransaction = await DecodeSearchTransactionAsync(searchText, false);
			if (!string.IsNullOrWhiteSpace(decodedTransaction.PageRouteName))
			{
				NavigationManager.NavigateTo(decodedTransaction.PageRouteName);
				return true;
			}
		}
		catch
		{
			// Ignore decode failures and use existing fallback navigation.
		}

		return false;
	}

	private async Task DownloadPdfFromSearch()
	{
		if (string.IsNullOrWhiteSpace(_searchText))
			return;

		var searchText = _searchText.Trim();

		try
		{
			var decodedTransaction = await DecodeSearchTransactionAsync(searchText, true);
			if (decodedTransaction.PDFStream.stream is not null && !string.IsNullOrWhiteSpace(decodedTransaction.PDFStream.fileName))
				await SaveAndViewService.SaveAndView(decodedTransaction.PDFStream.fileName, decodedTransaction.PDFStream.stream);
		}
		catch
		{
			// Ignore decode/download failures from header action.
		}
	}

	private static async Task<DecodeTransactionNoModel> DecodeSearchTransactionAsync(string searchText, bool pdf)
	{
		var decodedTransaction = await DecodeCode.DecodeTransactionNo(searchText);
		if (!string.IsNullOrWhiteSpace(decodedTransaction.PageRouteName) || (pdf && decodedTransaction.PDFStream.stream is not null))
			return decodedTransaction;

		var upperSearchText = searchText.ToUpperInvariant();
		if (!searchText.Equals(upperSearchText, StringComparison.Ordinal))
		{
			var upperDecodedTransaction = await DecodeCode.DecodeTransactionNo(upperSearchText);
			if (!string.IsNullOrWhiteSpace(upperDecodedTransaction.PageRouteName) || (pdf && upperDecodedTransaction.PDFStream.stream is not null))
				return upperDecodedTransaction;
		}

		return decodedTransaction;
	}

	private void OnRouteSelected(GlobalSearchItem item)
	{
		_selectedSearchItem = item;

		if (item is not null)
			NavigationManager.NavigateTo(item.Route);
	}
	#endregion

	#region Load Data
	[Parameter]
	public string Title { get; set; } = string.Empty;

	[Parameter]
	public RenderFragment? LeftContent { get; set; }

	[Parameter]
	public RenderFragment? RightContent { get; set; }

	private UserModel _user;

	protected override async Task OnInitializedAsync()
	{
		_user = new UserModel { Name = "aa", Id = 1 };
		_user = await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService);
		LoadRoutes();
	}

	private string GetMobileUserName()
	{
		if (_user is null || string.IsNullOrWhiteSpace(_user.Name))
			return string.Empty;

		var userName = _user.Name.Trim();
		return userName.Length > 5 ? $"{userName[..5]}..." : userName;
	}

	private void NavigateToHome() =>
		NavigationManager.NavigateTo(OperationNames.Dashboard);

	private async Task Logout() =>
		await AuthenticationService.Logout(DataStorageService, NavigationManager, NotificationService, VibrationService);
	#endregion
}

public class GlobalSearchItem
{
	public string Name { get; set; } = string.Empty;
	public string FriendlyName { get; set; } = string.Empty;
	public string DisplayText { get; set; } = string.Empty;
	public string Route { get; set; } = string.Empty;
}