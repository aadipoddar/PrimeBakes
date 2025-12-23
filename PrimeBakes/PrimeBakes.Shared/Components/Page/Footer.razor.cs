using Microsoft.AspNetCore.Components;
using System.Reflection;
using PrimeBakesLibrary.Models.Common;

namespace PrimeBakes.Shared.Components.Page;

public partial class Footer
{
    [Parameter]
    public string CopyrightUrl { get; set; } = "https://aadisoft.vercel.app";

    private string Factor =>
        FormFactor.GetFormFactor();

    private string Platform =>
        FormFactor.GetPlatform();

    private static string AppVersion =>
        Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0.0";
}