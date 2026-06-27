using Microsoft.AspNetCore.Components;

using Syncfusion.Blazor.Grids;

namespace PrimeBakes.Shared.Components.Grid;

public partial class GridColumnAggregate
{
	[Parameter] public string Field { get; set; }
	[Parameter] public AggregateType Type { get; set; } = AggregateType.Sum;
	[Parameter] public string? Format { get; set; } = "N2";
}