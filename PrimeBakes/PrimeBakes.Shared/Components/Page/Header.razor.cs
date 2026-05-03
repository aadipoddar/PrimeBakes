using Microsoft.AspNetCore.Components;

using PrimeBakes.Shared.Components.Dialog;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Models.Operations;

using Syncfusion.Blazor.DropDowns;

using System.Reflection;

namespace PrimeBakes.Shared.Components.Page;

public partial class Header
{
    #region Blueetooth
    private BluetoothReconnectDialog _btDialog;
    private BluetoothDeviceInfo? _btSaved;
    private bool _isBtReconnecting;

    private bool BtIsConnected => BluetoothPrinterService.IsConnected;
    private bool ShowBtButton => _btSaved is not null;

    private async Task LoadBluetoothStatusAsync()
    {
        var json = await DataStorageService.LocalGetAsync(StorageFileNames.BluetoothPrinterDataFileName);
        _btSaved = string.IsNullOrEmpty(json)
            ? null
            : System.Text.Json.JsonSerializer.Deserialize<BluetoothDeviceInfo>(json);
        StateHasChanged();
    }

    private async Task HandleBtClick() =>
        await _btDialog.ShowAsync();

    private async Task HandleBtReconnect()
    {
        try
        {
            _isBtReconnecting = true;
            StateHasChanged();

            var connected = await BluetoothPrinterService.ConnectAsync(_btSaved?.Address ?? string.Empty);
            var info = new BluetoothDeviceInfo
            {
                Name = connected ? BluetoothPrinterService.ConnectedPrinterName : (_btSaved?.Name ?? string.Empty),
                Address = _btSaved?.Address ?? string.Empty,
                IsPaired = true,
                IsConnected = connected
            };
            await DataStorageService.LocalSaveAsync(StorageFileNames.BluetoothPrinterDataFileName, System.Text.Json.JsonSerializer.Serialize(info));
            _btSaved = info;
            await _btDialog.HideAsync();
        }
        catch
        {
            await _btDialog.HideAsync();
        }
        finally
        {
            _isBtReconnecting = false;
            StateHasChanged();
        }
    }

    private async Task HandleBtDismiss() =>
        await _btDialog.HideAsync();

    private async Task HandleBtDisconnect()
    {
        try
        {
            await BluetoothPrinterService.DisconnectAsync();
            await DataStorageService.LocalRemove(StorageFileNames.BluetoothPrinterDataFileName);
            _btSaved = null;
        }
        catch { }
        finally
        {
            StateHasChanged();
        }
    }
    #endregion

    #region Search
    private string _searchText = string.Empty;
    private List<GlobalSearchItem> _searchItems = [];
    private SfAutoComplete<string, GlobalSearchItem> _sfGlobalSearch;

    private async Task FocusSearchBox()
    {
        if (_sfGlobalSearch is null)
            return;

        await _sfGlobalSearch.FocusAsync();
    }

    private void LoadRoutes()
    {
        _searchItems = [.. typeof(PageRouteNames)
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
    }

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
            var decodedTransaction = await DecodeSearchTransactionAsync(searchText);
            if (!string.IsNullOrWhiteSpace(decodedTransaction.PageRouteName))
            {
                NavigationManager.NavigateTo(decodedTransaction.PageRouteName, true);
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
            var decodedTransaction = await DecodeSearchTransactionAsync(searchText);
            if (decodedTransaction.PDFStream.stream is not null && !string.IsNullOrWhiteSpace(decodedTransaction.PDFStream.fileName))
                await SaveAndViewService.SaveAndView(decodedTransaction.PDFStream.fileName, decodedTransaction.PDFStream.stream);
        }
        catch
        {
            // Ignore decode/download failures from header action.
        }
    }

    private static async Task<DecodeTransactionNoModel> DecodeSearchTransactionAsync(string searchText)
    {
        var decodedTransaction = await DecodeCode.DecodeTransactionNo(searchText);
        if (!string.IsNullOrWhiteSpace(decodedTransaction.PageRouteName) || decodedTransaction.PDFStream.stream is not null)
            return decodedTransaction;

        var upperSearchText = searchText.ToUpperInvariant();
        if (!searchText.Equals(upperSearchText, StringComparison.Ordinal))
        {
            var upperDecodedTransaction = await DecodeCode.DecodeTransactionNo(upperSearchText);
            if (!string.IsNullOrWhiteSpace(upperDecodedTransaction.PageRouteName) || upperDecodedTransaction.PDFStream.stream is not null)
                return upperDecodedTransaction;
        }

        return decodedTransaction;
    }

    private void OnRouteSelected(Syncfusion.Blazor.DropDowns.SelectEventArgs<GlobalSearchItem> args)
    {
        if (args.ItemData is not null)
            NavigationManager.NavigateTo(args.ItemData.Route);
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

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;
        await LoadBluetoothStatusAsync();
    }

    protected override async Task OnInitializedAsync()
    {
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
        NavigationManager.NavigateTo(PageRouteNames.Dashboard);
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