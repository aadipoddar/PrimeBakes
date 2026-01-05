using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Models.Accounts.Masters;

namespace PrimeBakesLibrary.Exporting.Accounts.Masters;

public static class VoucherExcelExport
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

        var columnSettings = new Dictionary<string, ExcelReportExportUtil.ColumnSetting>
        {
            [nameof(VoucherModel.Id)] = new() { DisplayName = "ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },

            [nameof(VoucherModel.Name)] = new() { DisplayName = "Voucher Name", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IsRequired = true },
            [nameof(VoucherModel.Code)] = new() { DisplayName = "Code", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IsRequired = true },
            [nameof(VoucherModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },

            [nameof(VoucherModel.Status)] = new() { DisplayName = "Status", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false }
        };

        List<string> columnOrder =
        [
            nameof(VoucherModel.Id),
            nameof(VoucherModel.Name),
            nameof(VoucherModel.Code),
            nameof(VoucherModel.Remarks),
            nameof(VoucherModel.Status)
        ];

        var stream = await ExcelReportExportUtil.ExportToExcel(
           enrichedData,
           "VOUCHER",
           "Voucher Data",
           null,
           null,
           columnSettings,
           columnOrder
       );

        var currentDateTime = await CommonData.LoadCurrentDateTime();
        var fileName = $"Voucher_Master_{currentDateTime:yyyyMMdd_HHmmss}.xlsx";
        return (stream, fileName);
    }
}
