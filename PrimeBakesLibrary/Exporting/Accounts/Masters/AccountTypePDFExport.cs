using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Accounts.Masters;

namespace PrimeBakesLibrary.Exporting.Accounts.Masters;

public static class AccountTypePDFExport
{
	public static async Task<(MemoryStream stream, string fileName)> ExportMaster(IEnumerable<AccountTypeModel> accountTypeData)
	{
		var enrichedData = accountTypeData.Select(accountType => new
		{
			accountType.Id,
			accountType.Name,
			accountType.Remarks,
			Status = accountType.Status ? "Active" : "Deleted"
		});

		var columnSettings = new Dictionary<string, PDFReportExportUtil.ColumnSetting>
		{
			[nameof(AccountTypeModel.Id)] = new()
			{
				DisplayName = "ID",
				StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
				{
					Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
					LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
				},
				IncludeInTotal = false
			},

			[nameof(AccountTypeModel.Name)] = new() { DisplayName = "Account Type Name", IncludeInTotal = false },
			[nameof(AccountTypeModel.Remarks)] = new() { DisplayName = "Remarks", IncludeInTotal = false },

			[nameof(AccountTypeModel.Status)] = new()
			{
				DisplayName = "Status",
				StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
				{
					Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
					LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
				},
				IncludeInTotal = false
			}
		};

		List<string> columnOrder =
		[
			nameof(AccountTypeModel.Id),
			nameof(AccountTypeModel.Name),
			nameof(AccountTypeModel.Remarks),
			nameof(AccountTypeModel.Status)
		];

		var stream = await PDFReportExportUtil.ExportToPdf(
			enrichedData,
			"ACCOUNT TYPE MASTER",
			null,
			null,
			columnSettings,
			columnOrder,
			useLandscape: false
		);

		var currentDateTime = CommonData.LoadCurrentDateTime();
		var fileName = $"Account_Type_Master_{currentDateTime:yyyyMMdd_HHmmss}.pdf";
		return (stream, fileName);
	}
}
