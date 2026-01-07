using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Sales.Product;

namespace PrimeBakesLibrary.Exporting.Sales.Product;

public static class ProductPDFExport
{
    public static async Task<(MemoryStream stream, string fileName)> ExportMaster<T>(IEnumerable<T> productData)
    {
        var formattedData = productData.Select(product =>
        {
            var props = typeof(T).GetProperties();
            var id = props.FirstOrDefault(p => p.Name == "Id")?.GetValue(product);
            var name = props.FirstOrDefault(p => p.Name == "Name")?.GetValue(product)?.ToString();
            var code = props.FirstOrDefault(p => p.Name == "Code")?.GetValue(product)?.ToString();
            var category = props.FirstOrDefault(p => p.Name == "Category")?.GetValue(product)?.ToString();
            var rate = props.FirstOrDefault(p => p.Name == "Rate")?.GetValue(product);
            var tax = props.FirstOrDefault(p => p.Name == "Tax")?.GetValue(product)?.ToString();
            var remarks = props.FirstOrDefault(p => p.Name == "Remarks")?.GetValue(product)?.ToString();
            var status = props.FirstOrDefault(p => p.Name == "Status")?.GetValue(product);

            return new
            {
                Id = id,
                Name = name,
                Code = code,
                Category = category,
                Rate = rate is decimal rateVal ? $"{rateVal:N2}" : "0.00",
                Tax = tax,
                Remarks = remarks,
                Status = status is bool and true ? "Active" : "Deleted"
            };
        });

        var columnSettings = new Dictionary<string, PDFReportExportUtil.ColumnSetting>
        {
            [nameof(ProductModel.Id)] = new()
            {
                DisplayName = "ID",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                },
                IncludeInTotal = false
            },

            [nameof(ProductModel.Name)] = new() { DisplayName = "Product Name", IncludeInTotal = false },

            [nameof(ProductModel.Code)] = new() { DisplayName = "Code", IncludeInTotal = false },

            ["Category"] = new() { DisplayName = "Category", IncludeInTotal = false },

            [nameof(ProductModel.Rate)] = new()
            {
                DisplayName = "Rate",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                },
                IncludeInTotal = false
            },

            ["Tax"] = new()
            {
                DisplayName = "Tax Code",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                },
                IncludeInTotal = false
            },

            [nameof(ProductModel.Status)] = new()
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
            nameof(ProductModel.Id),
            nameof(ProductModel.Name),
            nameof(ProductModel.Code),
            "Category",
            nameof(ProductModel.Rate),
            "Tax",
            nameof(ProductModel.Remarks),
            nameof(ProductModel.Status)
        ];

        var stream = await PDFReportExportUtil.ExportToPdf(
            formattedData,
            "PRODUCT MASTER",
            null,
            null,
            columnSettings,
            columnOrder,
            useLandscape: true
        );

        var currentDateTime = await CommonData.LoadCurrentDateTime();
        var fileName = $"Product_Master_{currentDateTime:yyyyMMdd_HHmmss}.pdf";
        return (stream, fileName);
    }
}
