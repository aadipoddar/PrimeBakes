using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Models.Accounts.Masters;

namespace PrimeBakesLibrary.Exporting.Accounts.Masters;

public static class FinancialYearPDFExport
{
    public static async Task<(MemoryStream stream, string fileName)> ExportMaster(IEnumerable<FinancialYearModel> financialYearData)
    {
        var enrichedData = financialYearData.Select(fy => new
        {
            fy.Id,
            StartDate = fy.StartDate.ToString("dd-MMM-yyyy"),
            EndDate = fy.EndDate.ToString("dd-MMM-yyyy"),
            fy.YearNo,
            fy.Remarks,
            Locked = fy.Locked ? "Yes" : "No",
            Status = fy.Status ? "Active" : "Deleted"
        });

        var columnSettings = new Dictionary<string, PDFReportExportUtil.ColumnSetting>
        {
            [nameof(FinancialYearModel.Id)] = new()
            {
                DisplayName = "ID",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                },
                IncludeInTotal = false
            },

            [nameof(FinancialYearModel.StartDate)] = new()
            {
                DisplayName = "Start Date",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                },
                IncludeInTotal = false
            },

            [nameof(FinancialYearModel.EndDate)] = new()
            {
                DisplayName = "End Date",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                },
                IncludeInTotal = false
            },

            [nameof(FinancialYearModel.YearNo)] = new()
            {
                DisplayName = "Year No",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                },
                IncludeInTotal = false
            },

            [nameof(FinancialYearModel.Remarks)] = new() { DisplayName = "Remarks", IncludeInTotal = false },

            [nameof(FinancialYearModel.Locked)] = new()
            {
                DisplayName = "Locked",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                },
                IncludeInTotal = false
            },

            [nameof(FinancialYearModel.Status)] = new()
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
            nameof(FinancialYearModel.Id),
            nameof(FinancialYearModel.StartDate),
            nameof(FinancialYearModel.EndDate),
            nameof(FinancialYearModel.YearNo),
            nameof(FinancialYearModel.Remarks),
            nameof(FinancialYearModel.Locked),
            nameof(FinancialYearModel.Status)
        ];

        var stream = await PDFReportExportUtil.ExportToPdf(
            enrichedData,
            "FINANCIAL YEAR MASTER",
            null,
            null,
            columnSettings,
            columnOrder,
            useLandscape: false
        );

        var currentDateTime = await CommonData.LoadCurrentDateTime();
        var fileName = $"FinancialYear_Master_{currentDateTime:yyyyMMdd_HHmmss}.pdf";
        return (stream, fileName);
    }
}
