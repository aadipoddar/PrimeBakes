using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Sales.Product;

namespace PrimeBakesLibrary.Exporting.Sales.Product;

public static class TaxExcelExport
{
    public static async Task<(MemoryStream stream, string fileName)> ExportMaster(IEnumerable<TaxModel> taxData)
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

        var columnSettings = new Dictionary<string, ExcelReportExportUtil.ColumnSetting>
        {
            [nameof(TaxModel.Id)] = new() { DisplayName = "ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(TaxModel.Code)] = new() { DisplayName = "Tax Code", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IsRequired = true },
            [nameof(TaxModel.CGST)] = new() { DisplayName = "CGST %", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, Format = "0.00", IncludeInTotal = false },
            [nameof(TaxModel.SGST)] = new() { DisplayName = "SGST %", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, Format = "0.00", IncludeInTotal = false },
            [nameof(TaxModel.IGST)] = new() { DisplayName = "IGST %", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, Format = "0.00", IncludeInTotal = false },
            ["Total"] = new() { DisplayName = "Total GST %", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, Format = "0.00", IncludeInTotal = false },
            [nameof(TaxModel.Inclusive)] = new() { DisplayName = "Inclusive", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(TaxModel.Extra)] = new() { DisplayName = "Extra", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(TaxModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(TaxModel.Status)] = new() { DisplayName = "Status", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false }
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

        var stream = await ExcelReportExportUtil.ExportToExcel(
            enrichedData,
            "TAX MASTER",
            "Tax Data",
            null,
            null,
            columnSettings,
            columnOrder
        );

        var currentDateTime = await CommonData.LoadCurrentDateTime();
        var fileName = $"Tax_Master_{currentDateTime:yyyyMMdd_HHmmss}.xlsx";
        return (stream, fileName);
    }
}
