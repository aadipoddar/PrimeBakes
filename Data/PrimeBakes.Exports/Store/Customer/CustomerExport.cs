using PrimeBakes.Exports.Utils.Exports;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Store.Customer;

namespace PrimeBakes.Exports.Store.Customer;

public static class CustomerExport
{
	public static (MemoryStream stream, string fileName) ExportMaster(
		IEnumerable<CustomerModel> customerData,
		DateTime currentDateTime,
		ReportExportType exportType)
	{
		var enrichedData = customerData.Select(customer => new
		{
			customer.Id,
			customer.Name,
			customer.Number
		});

		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			[nameof(CustomerModel.Id)] = new() { DisplayName = "ID", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(CustomerModel.Name)] = new() { DisplayName = "Customer Name", Alignment = CellAlignment.Left, IsRequired = true },
			[nameof(CustomerModel.Number)] = new() { DisplayName = "Customer Number", Alignment = CellAlignment.Left }
		};

		List<string> columnOrder =
		[
			nameof(CustomerModel.Id),
			nameof(CustomerModel.Name),
			nameof(CustomerModel.Number)
		];

		var fileName = $"Customer_Master_{currentDateTime:yyyyMMdd_HHmmss}";

		if (exportType == ReportExportType.PDF)
		{
			var stream = PDFReportExportUtil.ExportToPdf(
				enrichedData,
				"CUSTOMER MASTER",
				currentDateTime,
				null,
				null,
				columnSettings,
				columnOrder,
				useBuiltInStyle: false,
				useLandscape: false
			);

			return (stream, fileName + ".pdf");
		}
		else
		{
			var stream = ExcelReportExportUtil.ExportToExcel(
				enrichedData,
				"CUSTOMER",
				"Customer Data",
				currentDateTime,
				null,
				null,
				columnSettings,
				columnOrder
			);

			return (stream, fileName + ".xlsx");
		}
	}
}
