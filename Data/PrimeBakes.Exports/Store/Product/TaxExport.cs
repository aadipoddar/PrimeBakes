using PrimeBakes.Exports.Utils.Exports;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Store.Product;

namespace PrimeBakes.Exports.Store.Product;

public static class TaxExport
{
	public static (MemoryStream stream, string fileName) ExportMaster(
		IEnumerable<TaxModel> taxData,
		DateTime currentDateTime,
		ReportExportType exportType)
	{
		var enrichedData = taxData.Select(tax => new
		{
			tax.Id,
			tax.Code,
			tax.CGST,
			tax.SGST,
			tax.IGST,
			Total = tax.CGST + tax.SGST,
			Inclusive = tax.Inclusive ? "Yes" : "No",
			Extra = tax.Extra ? "Yes" : "No",
			tax.Remarks,
			Status = tax.Status ? "Active" : "Deleted"
		});

		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			[nameof(TaxModel.Id)] = new() { DisplayName = "ID", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(TaxModel.Code)] = new() { DisplayName = "Tax Code", Alignment = CellAlignment.Left, IsRequired = true },
			[nameof(TaxModel.CGST)] = new() { DisplayName = "CGST %", Alignment = CellAlignment.Right, Format = "0.00", IncludeInTotal = false },
			[nameof(TaxModel.SGST)] = new() { DisplayName = "SGST %", Alignment = CellAlignment.Right, Format = "0.00", IncludeInTotal = false },
			[nameof(TaxModel.IGST)] = new() { DisplayName = "IGST %", Alignment = CellAlignment.Right, Format = "0.00", IncludeInTotal = false },
			["Total"] = new() { DisplayName = "Total GST %", Alignment = CellAlignment.Right, Format = "0.00", IncludeInTotal = false },
			[nameof(TaxModel.Inclusive)] = new() { DisplayName = "Inclusive", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(TaxModel.Extra)] = new() { DisplayName = "Extra", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(TaxModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = CellAlignment.Left },
			[nameof(TaxModel.Status)] = new() { DisplayName = "Status", Alignment = CellAlignment.Center, IncludeInTotal = false }
		};

		List<string> columnOrder =
		[
			nameof(TaxModel.Id),
			nameof(TaxModel.Code),
			nameof(TaxModel.CGST),
			nameof(TaxModel.SGST),
			nameof(TaxModel.IGST),
			"Total",
			nameof(TaxModel.Inclusive),
			nameof(TaxModel.Extra),
			nameof(TaxModel.Remarks),
			nameof(TaxModel.Status)
		];

		var fileName = $"Tax_Master_{currentDateTime:yyyyMMdd_HHmmss}";

		if (exportType == ReportExportType.PDF)
		{
			var stream = PDFReportExportUtil.ExportToPdf(
				enrichedData,
				"TAX MASTER",
				currentDateTime,
				null,
				null,
				columnSettings,
				columnOrder,
				useBuiltInStyle: false,
				useLandscape: true
			);

			return (stream, fileName + ".pdf");
		}
		else
		{
			var stream = ExcelReportExportUtil.ExportToExcel(
				enrichedData,
				"TAX MASTER",
				"Tax Data",
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
