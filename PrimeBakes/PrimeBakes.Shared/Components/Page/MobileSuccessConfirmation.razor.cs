using Microsoft.AspNetCore.Components;

namespace PrimeBakes.Shared.Components.Page;

public partial class MobileSuccessConfirmation
{
    [Parameter]
    public string Title { get; set; } = "Confirmed!";

    [Parameter]
    public string Subtitle { get; set; } = "Your transaction has been placed successfully";

    [Parameter]
    public string RedirectText { get; set; } = "Redirecting...";
}