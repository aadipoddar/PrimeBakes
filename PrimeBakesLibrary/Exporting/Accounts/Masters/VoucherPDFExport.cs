using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Accounts.Masters;

namespace PrimeBakesLibrary.Exporting.Accounts.Masters;

public static class VoucherPDFExport
{
    public static async Task<(MemoryStream stream, string fileName)> ExportMaster(IEnumerable<VoucherModel> voucherData)
    {
        var enrichedData = voucherData.Select(voucher => new
        {
            voucher.Id,
            voucher.Name,
            voucher.Code,
            voucher.Remarks,
            Status = voucher.Status ? "Active" : "Deleted"
        });

        var columnSettings = new Dictionary<string, PDFReportExportUtil.ColumnSetting>
        {
            [nameof(VoucherModel.Id)] = new()
            {
                DisplayName = "ID",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                },
                IncludeInTotal = false
            },

            [nameof(VoucherModel.Name)] = new() { DisplayName = "Voucher Name", IncludeInTotal = false },
            [nameof(VoucherModel.Code)] = new() { DisplayName = "Code", IncludeInTotal = false },
            [nameof(VoucherModel.Remarks)] = new() { DisplayName = "Remarks", IncludeInTotal = false },

            [nameof(VoucherModel.Status)] = new()
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
            nameof(VoucherModel.Id),
            nameof(VoucherModel.Name),
            nameof(VoucherModel.Code),
            nameof(VoucherModel.Remarks),
            nameof(VoucherModel.Status)
        ];

        var stream = await PDFReportExportUtil.ExportToPdf(
            enrichedData,
            "VOUCHER MASTER",
            null,
            null,
            columnSettings,
            columnOrder,
            useLandscape: false
        );

        var currentDateTime = await CommonData.LoadCurrentDateTime();
        var fileName = $"Voucher_Master_{currentDateTime:yyyyMMdd_HHmmss}.pdf";
        return (stream, fileName);
    }
}
