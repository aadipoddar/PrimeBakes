using PrimeBakes.Library.Store.Customer.Exports;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Exports;
using PrimeBakes.Models.Store.Customer;

namespace PrimeBakes.Api.Store.Customer.Exports;

public class CustomerExportEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(CustomerExportEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapPost(nameof(CustomerExport.ExportMaster), async (List<CustomerModel> customerData, ReportExportType exportType) =>
		{
			var (stream, fileName) = await CustomerExport.ExportMaster(customerData, exportType);
			return TypedResults.File(stream.ToArray(), Helper.ExportContentType, fileName);
		});
	}
}
