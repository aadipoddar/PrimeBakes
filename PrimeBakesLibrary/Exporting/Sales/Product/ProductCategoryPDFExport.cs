using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Sales.Product;

namespace PrimeBakesLibrary.Exporting.Sales.Product;

public static class ProductCategoryPDFExport
{
    public static async Task<(MemoryStream stream, string fileName)> ExportMaster(IEnumerable<ProductCategoryModel> productCategoryData)
    {
        var enrichedData = productCategoryData.Select(productCategory => new
        {
            productCategory.Id,
            productCategory.Name,
            productCategory.Remarks,
            Status = productCategory.Status ? "Active" : "Deleted"
        });

        var columnSettings = new Dictionary<string, PDFReportExportUtil.ColumnSetting>
        {
            [nameof(ProductCategoryModel.Id)] = new()
            {
                DisplayName = "ID",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                },
                IncludeInTotal = false
            },

            [nameof(ProductCategoryModel.Name)] = new() { DisplayName = "Product Category Name", IncludeInTotal = false },
            [nameof(ProductCategoryModel.Remarks)] = new() { DisplayName = "Remarks", IncludeInTotal = false },

            [nameof(ProductCategoryModel.Status)] = new()
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
            nameof(ProductCategoryModel.Id),
            nameof(ProductCategoryModel.Name),
            nameof(ProductCategoryModel.Remarks),
            nameof(ProductCategoryModel.Status)
        ];

        var stream = await PDFReportExportUtil.ExportToPdf(
            enrichedData,
            "PRODUCT CATEGORY MASTER",
            null,
            null,
            columnSettings,
            columnOrder,
            useLandscape: false
        );

        var currentDateTime = await CommonData.LoadCurrentDateTime();
        var fileName = $"ProductCategory_Master_{currentDateTime:yyyyMMdd_HHmmss}.pdf";
        return (stream, fileName);
    }
}
