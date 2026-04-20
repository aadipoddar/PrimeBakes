using Microsoft.AspNetCore.Components;

namespace PrimeBakes.Shared.Components.Button;

public partial class IconButton
{
    [Parameter]
    public IconType Icon { get; set; }

    [Parameter]
    public string Title { get; set; } = string.Empty;

    [Parameter]
    public bool Disabled { get; set; } = false;

    [Parameter]
    public EventCallback OnClick { get; set; }

    [Parameter]
    public ButtonVariant Variant { get; set; } = ButtonVariant.Default;

    [Parameter]
    public ButtonSize Size { get; set; } = ButtonSize.Medium;

    [Parameter]
    public string CssClass { get; set; } = string.Empty;

    [Parameter]
    public string Text { get; set; } = string.Empty;

    private int IconSize => Size switch
    {
        ButtonSize.Small => 16,
        ButtonSize.Medium => 20,
        ButtonSize.Large => 24,
        _ => 20
    };

    private string GetCssClass()
    {
        var classes = new List<string>
        {
            "icon-btn",
            // Add variant class
            Variant switch
            {
                ButtonVariant.Excel => "icon-btn-excel",
                ButtonVariant.Pdf => "icon-btn-pdf",
                ButtonVariant.View => "icon-btn-view",
                ButtonVariant.Delete => "icon-btn-delete",
                ButtonVariant.Add => "icon-btn-add",
                ButtonVariant.Get => "icon-btn-get",
                ButtonVariant.BluetoothConnected => "icon-btn-bt-connected",
                ButtonVariant.BluetoothDisconnected => "icon-btn-bt-disconnected",
                _ => string.Empty
            },

            // Add size class
            Size switch
            {
                ButtonSize.Small => "icon-btn-small",
                ButtonSize.Grid => "icon-btn-grid",
                _ => string.Empty
            }
        };

        // Add text class if text is provided
        if (!string.IsNullOrEmpty(Text))
            classes.Add("icon-btn-with-text");

        // Add custom CSS class
        if (!string.IsNullOrEmpty(CssClass))
            classes.Add(CssClass);

        return string.Join(" ", classes.Where(c => !string.IsNullOrEmpty(c)));
    }
}

public enum IconType
{
    Excel,
    Pdf,
    View,
    Refresh,
    Delete,
    Back,
    Add,
    Bluetooth,
    Get
}

public enum ButtonVariant
{
    Default,
    Excel,
    Pdf,
    View,
    Delete,
    Add,
    BluetoothConnected,
    BluetoothDisconnected,
    Get
}

public enum ButtonSize
{
    Small,
    Medium,
    Large,
    Grid
}
