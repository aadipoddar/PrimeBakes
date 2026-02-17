using Microsoft.AspNetCore.Components;

namespace PrimeBakes.Shared.Components.Button;

public partial class ToggleButton
{
    [Parameter]
    public bool Disabled { get; set; } = false;

    [Parameter]
    public bool IsActive { get; set; } = false;

    [Parameter]
    public ToggleVariant Variant { get; set; } = ToggleVariant.Details;

    [Parameter]
    public string TextWhenActive { get; set; }

    [Parameter]
    public string TextWhenInactive { get; set; }

    [Parameter]
    public EventCallback OnToggle { get; set; }

    private async Task HandleClick() => await OnToggle.InvokeAsync();

    private RenderFragment GetIcon() => Variant switch
    {
        ToggleVariant.Details => IsActive
            ? (builder =>
            {
                builder.OpenElement(0, "circle");
                builder.AddAttribute(1, "cx", "12");
                builder.AddAttribute(2, "cy", "12");
                builder.AddAttribute(3, "r", "1");
                builder.CloseElement();
                builder.OpenElement(4, "circle");
                builder.AddAttribute(5, "cx", "19");
                builder.AddAttribute(6, "cy", "12");
                builder.AddAttribute(7, "r", "1");
                builder.CloseElement();
                builder.OpenElement(8, "circle");
                builder.AddAttribute(9, "cx", "5");
                builder.AddAttribute(10, "cy", "12");
                builder.AddAttribute(11, "r", "1");
                builder.CloseElement();
            })
            : (builder =>
            {
                builder.OpenElement(0, "rect");
                builder.AddAttribute(1, "x", "3");
                builder.AddAttribute(2, "y", "3");
                builder.AddAttribute(3, "width", "18");
                builder.AddAttribute(4, "height", "18");
                builder.AddAttribute(5, "rx", "2");
                builder.AddAttribute(6, "ry", "2");
                builder.CloseElement();
                builder.OpenElement(7, "line");
                builder.AddAttribute(8, "x1", "9");
                builder.AddAttribute(9, "y1", "9");
                builder.AddAttribute(10, "x2", "15");
                builder.AddAttribute(11, "y2", "9");
                builder.CloseElement();
                builder.OpenElement(12, "line");
                builder.AddAttribute(13, "x1", "9");
                builder.AddAttribute(14, "y1", "15");
                builder.AddAttribute(15, "x2", "15");
                builder.AddAttribute(16, "y2", "15");
                builder.CloseElement();
            }),

        ToggleVariant.Deleted => builder =>
        {
            builder.OpenElement(0, "polyline");
            builder.AddAttribute(1, "points", "3 6 5 6 21 6");
            builder.CloseElement();
            builder.OpenElement(2, "path");
            builder.AddAttribute(3, "d", "M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2");
            builder.CloseElement();
            builder.OpenElement(4, "line");
            builder.AddAttribute(5, "x1", "10");
            builder.AddAttribute(6, "y1", "11");
            builder.AddAttribute(7, "x2", "10");
            builder.AddAttribute(8, "y2", "17");
            builder.CloseElement();
            builder.OpenElement(9, "line");
            builder.AddAttribute(10, "x1", "14");
            builder.AddAttribute(11, "y1", "11");
            builder.AddAttribute(12, "x2", "14");
            builder.AddAttribute(13, "y2", "17");
            builder.CloseElement();
        },

        ToggleVariant.Summary => IsActive
            ? (builder =>
            {
                builder.OpenElement(0, "line");
                builder.AddAttribute(1, "x1", "8");
                builder.AddAttribute(2, "y1", "6");
                builder.AddAttribute(3, "x2", "21");
                builder.AddAttribute(4, "y2", "6");
                builder.CloseElement();
                builder.OpenElement(5, "line");
                builder.AddAttribute(6, "x1", "8");
                builder.AddAttribute(7, "y1", "12");
                builder.AddAttribute(8, "x2", "21");
                builder.AddAttribute(9, "y2", "12");
                builder.CloseElement();
                builder.OpenElement(10, "line");
                builder.AddAttribute(11, "x1", "8");
                builder.AddAttribute(12, "y1", "18");
                builder.AddAttribute(13, "x2", "21");
                builder.AddAttribute(14, "y2", "18");
                builder.CloseElement();
                builder.OpenElement(15, "line");
                builder.AddAttribute(16, "x1", "3");
                builder.AddAttribute(17, "y1", "6");
                builder.AddAttribute(18, "x2", "3.01");
                builder.AddAttribute(19, "y2", "6");
                builder.CloseElement();
                builder.OpenElement(20, "line");
                builder.AddAttribute(21, "x1", "3");
                builder.AddAttribute(22, "y1", "12");
                builder.AddAttribute(23, "x2", "3.01");
                builder.AddAttribute(24, "y2", "12");
                builder.CloseElement();
                builder.OpenElement(25, "line");
                builder.AddAttribute(26, "x1", "3");
                builder.AddAttribute(27, "y1", "18");
                builder.AddAttribute(28, "x2", "3.01");
                builder.AddAttribute(29, "y2", "18");
                builder.CloseElement();
            })
            : (builder =>
            {
                builder.OpenElement(0, "rect");
                builder.AddAttribute(1, "x", "3");
                builder.AddAttribute(2, "y", "3");
                builder.AddAttribute(3, "width", "18");
                builder.AddAttribute(4, "height", "18");
                builder.AddAttribute(5, "rx", "2");
                builder.AddAttribute(6, "ry", "2");
                builder.CloseElement();
                builder.OpenElement(7, "line");
                builder.AddAttribute(8, "x1", "3");
                builder.AddAttribute(9, "y1", "9");
                builder.AddAttribute(10, "x2", "21");
                builder.AddAttribute(11, "y2", "9");
                builder.CloseElement();
                builder.OpenElement(12, "line");
                builder.AddAttribute(13, "x1", "3");
                builder.AddAttribute(14, "y1", "15");
                builder.AddAttribute(15, "x2", "21");
                builder.AddAttribute(16, "y2", "15");
                builder.CloseElement();
                builder.OpenElement(17, "line");
                builder.AddAttribute(18, "x1", "9");
                builder.AddAttribute(19, "y1", "3");
                builder.AddAttribute(20, "x2", "9");
                builder.AddAttribute(21, "y2", "21");
                builder.CloseElement();
            }),

        ToggleVariant.Returns => builder =>
        {
            builder.OpenElement(0, "polyline");
            builder.AddAttribute(1, "points", "9 14 4 9 9 4");
            builder.CloseElement();
            builder.OpenElement(2, "path");
            builder.AddAttribute(3, "d", "M20 20v-7a4 4 0 0 0-4-4H4");
            builder.CloseElement();
        },

        ToggleVariant.Transfers => builder =>
        {
            builder.OpenElement(0, "polyline");
            builder.AddAttribute(1, "points", "17 2 21 6 17 10");
            builder.CloseElement();
            builder.OpenElement(2, "path");
            builder.AddAttribute(3, "d", "M3 14v-2a4 4 0 0 1 4-4h14");
            builder.CloseElement();
            builder.OpenElement(4, "polyline");
            builder.AddAttribute(5, "points", "7 22 3 18 7 14");
            builder.CloseElement();
            builder.OpenElement(6, "path");
            builder.AddAttribute(7, "d", "M21 10v2a4 4 0 0 1-4 4H3");
            builder.CloseElement();
        },

        _ => builder => { }
    };

    private string GetButtonText() => Variant switch
    {
        ToggleVariant.Details => IsActive
            ? (TextWhenActive ?? "Show Less")
            : (TextWhenInactive ?? "Details"),

        ToggleVariant.Deleted => IsActive ? "Hide Deleted" : "Show Deleted",
        ToggleVariant.Summary => IsActive ? "Show Details" : "Show Summary",
        ToggleVariant.Returns => IsActive ? "Hide Returns" : "Show Returns",
        ToggleVariant.Transfers => IsActive ? "Hide Transfers" : "Show Transfers",
        _ => ""
    };

    private string GetTitle() => Variant switch
    {
        ToggleVariant.Details => IsActive ? "Show Less" : "Show All Details",
        ToggleVariant.Deleted => IsActive ? "Hide Deleted Items" : "Show Deleted Items",
        ToggleVariant.Summary => IsActive ? "Show Detailed Transactions" : "Show Consolidated Summary",
        ToggleVariant.Returns => IsActive ? "Hide Returns" : "Show Returns",
        ToggleVariant.Transfers => IsActive ? "Hide Transfers" : "Show Transfers",
        _ => ""
    };

    private string GetVariantClass() => Variant switch
    {
        ToggleVariant.Details => "btn-toggle-details",
        ToggleVariant.Deleted => "btn-toggle-deleted",
        ToggleVariant.Summary => "btn-toggle-summary",
        ToggleVariant.Returns => "btn-toggle-returns",
        ToggleVariant.Transfers => "btn-toggle-transfers",
        _ => ""
    };

    public enum ToggleVariant
    {
        Details,
        Deleted,
        Summary,
        Returns,
        Transfers
    }
}
