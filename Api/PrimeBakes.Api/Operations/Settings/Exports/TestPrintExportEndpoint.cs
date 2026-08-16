using PrimeBakes.Library.Operations.Settings.Exports;
using PrimeBakes.Models.Common;

namespace PrimeBakes.Api.Operations.Settings.Exports;

public class TestPrintExportEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(TestPrintExportEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapGet(nameof(TestPrintExport.GenerateTestReceipt),
			(string printerName, string printerAddress, string platform) => TestPrintExport.GenerateTestReceipt(printerName, printerAddress, platform));

		group.MapGet(nameof(TestPrintExport.GenerateTestReceiptPng),
			(string printerName, string printerAddress, string platform) => TestPrintExport.GenerateTestReceiptPng(printerName, printerAddress, platform));
	}
}
