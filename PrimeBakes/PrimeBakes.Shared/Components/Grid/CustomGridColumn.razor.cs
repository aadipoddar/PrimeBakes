using Microsoft.AspNetCore.Components;

using Syncfusion.Blazor.Grids;

namespace PrimeBakes.Shared.Components.Grid;

public partial class CustomGridColumn
{
	[Parameter] public string Field { get; set; }
	[Parameter] public string HeaderText { get; set; }
	[Parameter] public string Width { get; set; }
	[Parameter] public string? Format { get; set; }
	[Parameter] public TextAlign TextAlign { get; set; } = TextAlign.Left;
	[Parameter] public ColumnType Type { get; set; }
	[Parameter] public bool Visible { get; set; } = true;
	[Parameter] public bool Color { get; set; } = false;
	[Parameter] public bool DisplayAsCheckBox { get; set; } = false;
	[Parameter] public RenderFragment? CustomTemplate { get; set; }
}