using PrimeBakes.Exports.Utils.Exports;
using PrimeBakes.Models.Accounts.Masters;
using PrimeBakes.Models.Common;

namespace PrimeBakes.Exports.Accounts.Masters;

public static class VoucherExport
{
	public static (MemoryStream stream, string fileName) ExportMaster(
		IEnumerable<VoucherModel> voucherData,
		DateTime currentDateTime,
		ReportExportType exportType)
	{
		var enrichedData = voucherData.Select(voucher => new
		{
			voucher.Id,
			voucher.Name,
			voucher.Remarks,
			Status = voucher.Status ? "Active" : "Deleted"
		});

		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			[nameof(VoucherModel.Id)] = new() { DisplayName = "ID", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(VoucherModel.Name)] = new() { DisplayName = "Voucher Name", Alignment = CellAlignment.Left, IsRequired = true },
			[nameof(VoucherModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = CellAlignment.Left },
			[nameof(VoucherModel.Status)] = new() { DisplayName = "Status", Alignment = CellAlignment.Center, IncludeInTotal = false }
		};

		List<string> columnOrder =
		[
			nameof(VoucherModel.Id),
			nameof(VoucherModel.Name),
			nameof(VoucherModel.Remarks),
			nameof(VoucherModel.Status)
		];

		var fileName = $"Voucher_Master_{currentDateTime:yyyyMMdd_HHmmss}";

		if (exportType == ReportExportType.PDF)
		{
			var stream = PDFReportExportUtil.ExportToPdf(
				enrichedData,
				"VOUCHER MASTER",
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
				"VOUCHER",
				"Voucher Data",
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
