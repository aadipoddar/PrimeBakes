using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Store.Product;

namespace PrimeBakesLibrary.Exporting.Store.Product;

public static class ProductExport
{
    public static async Task<(MemoryStream stream, string fileName)> ExportMaster(
        IEnumerable<ProductModel> productData,
        ReportExportType exportType)
    {
        var categories = await CommonData.LoadTableData<ProductCategoryModel>(TableNames.ProductCategory);
        var kotCategories = await CommonData.LoadTableData<KOTCategoryModel>(TableNames.KOTCategory);
        var taxes = await CommonData.LoadTableData<TaxModel>(TableNames.Tax);

		var enrichedData = productData.Select(p => new
		{
			p.Id,
			p.Name,
			p.Code,
			Category = categories.FirstOrDefault(c => c.Id == p.ProductCategoryId)?.Name ?? "N/A",
			KOTCategory = kotCategories.FirstOrDefault(k => k.Id == p.KOTCategoryId)?.Name ?? "N/A",
			p.Rate,
			Tax = taxes.FirstOrDefault(t => t.Id == p.TaxId)?.Code ?? "N/A",
			p.Remarks,
			p.Status
		}).ToList();

        var columnSettings = new Dictionary<string, ReportColumnSetting>
        {
            [nameof(ProductModel.Id)] = new() { DisplayName = "ID", Alignment = CellAlignment.Center, IncludeInTotal = false },
            [nameof(ProductModel.Name)] = new() { DisplayName = "Product Name", Alignment = CellAlignment.Left, IsRequired = true },
            [nameof(ProductModel.Code)] = new() { DisplayName = "Product Code", Alignment = CellAlignment.Left, IsRequired = true },
            ["Category"] = new() { DisplayName = "Category", Alignment = CellAlignment.Left },
            ["KOTCategory"] = new() { DisplayName = "KOT Category", Alignment = CellAlignment.Left },
            [nameof(ProductModel.Rate)] = new() { DisplayName = "Rate", Alignment = CellAlignment.Right, Format = "0.00" },
            ["Tax"] = new() { DisplayName = "Tax Code", Alignment = CellAlignment.Center },
            [nameof(ProductModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = CellAlignment.Left },
            [nameof(ProductModel.Status)] = new() { DisplayName = "Status", Alignment = CellAlignment.Center, IncludeInTotal = false }
        };

        List<string> columnOrder =
        [
            nameof(ProductModel.Id),
            nameof(ProductModel.Name),
            nameof(ProductModel.Code),
            "Category",
            "KOTCategory",
			nameof(ProductModel.Rate),
            "Tax",
            nameof(ProductModel.Remarks),
            nameof(ProductModel.Status)
        ];

        var currentDateTime = await CommonData.LoadCurrentDateTime();
        var fileName = $"Product_Master_{currentDateTime:yyyyMMdd_HHmmss}";

        if (exportType == ReportExportType.PDF)
        {
            var stream = await PDFReportExportUtil.ExportToPdf(
                enrichedData,
                "PRODUCT MASTER",
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
            var stream = await ExcelReportExportUtil.ExportToExcel(
				enrichedData,
                "PRODUCT MASTER",
                "Product Data",
                null,
                null,
                columnSettings,
                columnOrder
            );

            return (stream, fileName + ".xlsx");
        }
    }
}
