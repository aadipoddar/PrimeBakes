using Microsoft.AspNetCore.Components;

using Syncfusion.Blazor.Grids;

namespace PrimeBakes.Shared.Components.Grid;

public partial class CustomGridColumn
{
	[Parameter] public string Field { get; set; }
	[Parameter] public string? HeaderText { get; set; }
	[Parameter] public string Width { get; set; } = "200";
	[Parameter] public string? Format { get; set; }
	[Parameter] public TextAlign TextAlign { get; set; } = TextAlign.Left;
	[Parameter] public ColumnType Type { get; set; }
	[Parameter] public bool Visible { get; set; } = true;
	[Parameter] public bool DisplayAsCheckBox { get; set; } = false;

	[Parameter] public bool StatusColumn { get; set; } = false;
	[Parameter] public RenderFragment? CustomTemplate { get; set; }
	[Parameter] public string? CustomColor { get; set; }
	[Parameter] public bool NumericColumn { get; set; } = false;
	[Parameter] public bool NegativePositiveColumn { get; set; } = false;
	[Parameter] public bool TotalColumn { get; set; } = false;
}