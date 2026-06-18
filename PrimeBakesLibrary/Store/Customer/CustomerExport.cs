using PrimeBakesLibrary.Common;
using PrimeBakesLibrary.Store.Customer;
using PrimeBakesLibrary.Utils.Exports;

namespace PrimeBakesLibrary.Store.Product.Exports;

public static class CustomerExport
{
	public static async Task<(MemoryStream stream, string fileName)> ExportMaster(
		IEnumerable<CustomerModel> customerData,
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

		var currentDateTime = await CommonData.LoadCurrentDateTime();
		var fileName = $"Customer_Master_{currentDateTime:yyyyMMdd_HHmmss}";

		if (exportType == ReportExportType.PDF)
		{
			var stream = await PDFReportExportUtil.ExportToPdf(
				enrichedData,
				"CUSTOMER MASTER",
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
			var stream = await ExcelReportExportUtil.ExportToExcel(
				enrichedData,
				"CUSTOMER",
				"Customer Data",
				null,
				null,
				columnSettings,
				columnOrder
			);

			return (stream, fileName + ".xlsx");
		}
	}
}
