using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Inventory;

namespace PrimeBakesLibrary.Exporting.Inventory.RawMaterial;

public static class RawMaterialCategoryExport
{
    public static async Task<(MemoryStream stream, string fileName)> ExportMaster(
        IEnumerable<RawMaterialCategoryModel> rawMaterialCategoryData,
        ReportExportType exportType)
    {
        var enrichedData = rawMaterialCategoryData.Select(rawMaterialCategory => new
        {
            rawMaterialCategory.Id,
            rawMaterialCategory.Name,
            rawMaterialCategory.Remarks,
            Status = rawMaterialCategory.Status ? "Active" : "Deleted"
        });

        var columnSettings = new Dictionary<string, ReportColumnSetting>
        {
            [nameof(RawMaterialCategoryModel.Id)] = new() { DisplayName = "ID", Alignment = CellAlignment.Center, IncludeInTotal = false },
            [nameof(RawMaterialCategoryModel.Name)] = new() { DisplayName = "Raw Material Category Name", Alignment = CellAlignment.Left, IsRequired = true },
            [nameof(RawMaterialCategoryModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = CellAlignment.Left },
            [nameof(RawMaterialCategoryModel.Status)] = new() { DisplayName = "Status", Alignment = CellAlignment.Center, IncludeInTotal = false }
        };

        List<string> columnOrder =
        [
            nameof(RawMaterialCategoryModel.Id),
            nameof(RawMaterialCategoryModel.Name),
            nameof(RawMaterialCategoryModel.Remarks),
            nameof(RawMaterialCategoryModel.Status)
        ];

        var currentDateTime = await CommonData.LoadCurrentDateTime();
        var fileName = $"RawMaterialCategory_Master_{currentDateTime:yyyyMMdd_HHmmss}";

        if (exportType == ReportExportType.PDF)
        {
            var stream = await PDFReportExportUtil.ExportToPdf(
                enrichedData,
                "RAW MATERIAL CATEGORY MASTER",
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
                "RAW MATERIAL CATEGORY",
                "Raw Material Category Data",
                null,
                null,
                columnSettings,
                columnOrder
            );

            return (stream, fileName + ".xlsx");
        }
    }
}
