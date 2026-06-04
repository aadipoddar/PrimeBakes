using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Store.Product;

namespace PrimeBakesLibrary.Exporting.Store.Product;

public static class KOTCategoryExport
{
    public static async Task<(MemoryStream stream, string fileName)> ExportMaster(
        IEnumerable<KOTCategoryModel> kotCategoryData,
        ReportExportType exportType)
    {
        var enrichedData = kotCategoryData.Select(kotCategory => new
        {
            kotCategory.Id,
            kotCategory.Name,
            kotCategory.Remarks,
            Status = kotCategory.Status ? "Active" : "Deleted"
        });

        var columnSettings = new Dictionary<string, ReportColumnSetting>
        {
            [nameof(KOTCategoryModel.Id)] = new() { DisplayName = "ID", Alignment = CellAlignment.Center, IncludeInTotal = false },
            [nameof(KOTCategoryModel.Name)] = new() { DisplayName = "KOT Category Name", Alignment = CellAlignment.Left, IsRequired = true },
            [nameof(KOTCategoryModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = CellAlignment.Left },
            [nameof(KOTCategoryModel.Status)] = new() { DisplayName = "Status", Alignment = CellAlignment.Center, IncludeInTotal = false }
        };

        List<string> columnOrder =
        [
            nameof(KOTCategoryModel.Id),
            nameof(KOTCategoryModel.Name),
            nameof(KOTCategoryModel.Remarks),
            nameof(KOTCategoryModel.Status)
        ];

        var currentDateTime = await CommonData.LoadCurrentDateTime();
        var fileName = $"KOTCategory_Master_{currentDateTime:yyyyMMdd_HHmmss}";

        if (exportType == ReportExportType.PDF)
        {
            var stream = await PDFReportExportUtil.ExportToPdf(
                enrichedData,
                "KOT CATEGORY MASTER",
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
                "KOT CATEGORY",
                "KOT Category Data",
                null,
                null,
                columnSettings,
                columnOrder
            );

            return (stream, fileName + ".xlsx");
        }
    }
}
