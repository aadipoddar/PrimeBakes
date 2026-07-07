using PrimeBakes.Library.Operations.Location;
using PrimeBakes.Library.Operations.User;
using PrimeBakes.Library.Restaurant.Bill.Data;
using PrimeBakes.Library.Restaurant.Bill.Models;
using PrimeBakes.Library.Restaurant.Dining.Data;
using PrimeBakes.Library.Restaurant.Dining.Models;
using PrimeBakes.Shared.Components.Input;

using Syncfusion.Blazor.Diagram;

using System.Text.Json;

namespace PrimeBakes.Shared.Pages.Restaurant.Bill;

public partial class DiningDashbaord
{
	private UserModel _user;
	private bool _isLoading = true;
	private DateTime _now = DateTime.Now;

	private LocationModel _selectedLocation = new();
	private DiningAreaModel _selectedArea;

	private List<LocationModel> _locations = [];
	private List<DiningAreaModel> _diningAreas = [];
	private List<DiningTableModel> _diningTables = [];
	private List<BillModel> _runningBills = [];

	private readonly DiagramObjectCollection<Node> _nodes = [];

	private CustomAutoComplete<LocationModel> _firstFocus;
	private bool _designMode;

	private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

	private const NodeConstraints _nodeConstraints =
		NodeConstraints.Default & ~(NodeConstraints.Rotate | NodeConstraints.InConnect | NodeConstraints.OutConnect);

	// Fallback grid for tables that have no saved layout yet.
	private const int _gridColumns = 4;
	private const double _nodeWidth = 130;
	private const double _nodeHeight = 80;
	private const double _gridGapX = 40;
	private const double _gridGapY = 40;
	private const double _gridStartX = 95;
	private const double _gridStartY = 70;

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		_user = await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, [UserRoles.Restaurant]);
		await LoadData();
	}

	private async Task LoadData()
	{
		_now = await CommonData.LoadCurrentDateTime();

		_locations = [.. (await CommonData.LoadTableDataByStatus<LocationModel>(OperationNames.Location)).OrderBy(l => l.Name)];
		_selectedLocation = _locations.FirstOrDefault(l => l.Id == _user.LocationId);

		await LoadDining();

		_isLoading = false;
		StateHasChanged();

		if (_firstFocus is not null) await _firstFocus.FocusAsync();
	}

	private async Task LoadDining()
	{
		_diningAreas = [.. (await CommonData.LoadTableDataByStatus<DiningAreaModel>(RestaurantNames.DiningArea)).Where(a => a.LocationId == _selectedLocation.Id).OrderBy(a => a.Name)];
		_diningTables = [.. (await CommonData.LoadTableDataByStatus<DiningTableModel>(RestaurantNames.DiningTable)).Where(t => _diningAreas.Any(a => a.Id == t.DiningAreaId)).OrderBy(t => t.Name)];
		_runningBills = await BillData.LoadRunningBillByLocationId(_selectedLocation.Id);

		_selectedArea = _diningAreas.FirstOrDefault();
		BuildNodes();
	}

	private void BuildNodes()
	{
		_nodes.Clear();

		if (_selectedArea is null)
			return;

		var areaTables = _diningTables.Where(t => t.DiningAreaId == _selectedArea.Id).OrderBy(t => t.Name).ToList();

		for (var i = 0; i < areaTables.Count; i++)
		{
			var table = areaTables[i];
			var layout = ParseLayout(table.LayoutJson);
			var bill = _runningBills.FirstOrDefault(b => b.DiningTableId == table.Id && b.Running);
			var running = bill is not null;

			_nodes.Add(new Node
			{
				ID = table.Id.ToString(),
				OffsetX = layout?.X ?? _gridStartX + i % _gridColumns * (_nodeWidth + _gridGapX),
				OffsetY = layout?.Y ?? _gridStartY + i / _gridColumns * (_nodeHeight + _gridGapY),
				Width = layout?.W ?? _nodeWidth,
				Height = layout?.H ?? _nodeHeight,
				Shape = new BasicShape { Type = NodeShapes.Basic, Shape = NodeBasicShapes.Rectangle, CornerRadius = 10 },
				Style = new ShapeStyle
				{
					Fill = running ? "#ffe4e6" : "#dcfce7",
					StrokeColor = running ? "#fda4af" : "#86efac",
					StrokeWidth = 1.5
				},
				Annotations = BuildAnnotations(table.Name, bill, running, running ? Elapsed(bill.TransactionDateTime) : null),
				Constraints = _nodeConstraints
			});
		}
	}

	private static DiagramObjectCollection<ShapeAnnotation> BuildAnnotations(string name, BillModel bill, bool running, string elapsed)
	{
		var color = running ? "#9f1239" : "#166534";

		var detail = running
			? $"{bill.TotalAmount.FormatIndianCurrency()}\n{bill.TotalItems} items • {bill.TotalQuantity.FormatSmartDecimal()} qty\n {elapsed}"
			: "Available";

		return
		[
			new ShapeAnnotation
			{
				Content = name,
				Offset = new DiagramPoint { X = 0.5, Y = 0.28 },
				Style = new TextStyle { Color = color, Bold = true, FontSize = 15 }
			},
			new ShapeAnnotation
			{
				Content = detail,
				Offset = new DiagramPoint { X = 0.5, Y = 0.62 },
				Style = new TextStyle { Color = color, FontSize = 11 }
			}
		];
	}

	private static DiningTableLayout ParseLayout(string json) =>
		string.IsNullOrWhiteSpace(json) ? null : JsonSerializer.Deserialize<DiningTableLayout>(json, _jsonOptions);

	private string Elapsed(DateTime since)
	{
		var span = _now - since;
		if (span < TimeSpan.Zero)
			span = TimeSpan.Zero;

		if (span.TotalHours >= 1)
			return $"{(int)span.TotalHours}h {span.Minutes}m";

		return $"{(int)span.TotalMinutes}m";
	}

	private async Task OnLocationChanged(LocationModel location)
	{
		if (_user.LocationId > 1 || location is null)
			return;

		_selectedLocation = location;
		await LoadDining();
	}

	private void OnAreaChanged(DiningAreaModel area)
	{
		_selectedArea = area;
		BuildNodes();
	}

	private void OnPositionChanging(PositionChangingEventArgs args) => args.Cancel = !_designMode;
	private void OnSizeChanging(SizeChangingEventArgs args) => args.Cancel = !_designMode;

	private void EnterDesignMode() => _designMode = true;

	private void CancelDesign()
	{
		_designMode = false;
		BuildNodes();
	}

	private async Task SaveDesign()
	{
		var updated = new List<DiningTableModel>();

		foreach (var node in _nodes)
		{
			var table = _diningTables.FirstOrDefault(t => t.Id == int.Parse(node.ID));
			if (table is null)
				continue;

			table.LayoutJson = JsonSerializer.Serialize(new DiningTableLayout
			{
				X = node.OffsetX,
				Y = node.OffsetY,
				W = node.Width ?? _nodeWidth,
				H = node.Height ?? _nodeHeight,
				Shape = "Rectangle"
			});
			updated.Add(table);
		}

		foreach (var item in updated)
			await DiningTableData.InsertDiningTable(item);

		_designMode = false;
		BuildNodes();
	}

	private void OnDiagramClick(ClickEventArgs args)
	{
		if (_designMode)
			return;

		if (args.ActualObject is Node node && int.TryParse(node.ID, out var diningTableId))
			NavigationManager.NavigateTo($"{RestaurantRouteNames.Bill}/table/{diningTableId}");
	}
}
