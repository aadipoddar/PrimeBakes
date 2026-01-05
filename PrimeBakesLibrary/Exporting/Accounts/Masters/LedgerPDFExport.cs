using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Models.Accounts.Masters;

namespace PrimeBakesLibrary.Exporting.Accounts.Masters;

public static class LedgerPDFExport
{
    public static async Task<(MemoryStream stream, string fileName)> ExportMaster(IEnumerable<LedgerModel> ledgerData)
    {
        var groups = await CommonData.LoadTableData<GroupModel>(TableNames.Group);
        var accountTypes = await CommonData.LoadTableData<AccountTypeModel>(TableNames.AccountType);
        var stateUTs = await CommonData.LoadTableData<StateUTModel>(TableNames.StateUT);

        var enrichedData = ledgerData.Select(ledger => new
        {
            ledger.Id,
            ledger.Name,
            ledger.Code,
            Group = groups.FirstOrDefault(g => g.Id == ledger.GroupId)?.Name ?? "N/A",
            AccountType = accountTypes.FirstOrDefault(at => at.Id == ledger.AccountTypeId)?.Name ?? "N/A",
            StateUT = stateUTs.FirstOrDefault(su => su.Id == ledger.StateUTId)?.Name ?? "N/A",
            ledger.GSTNo,
            ledger.PANNo,
            ledger.CINNo,
            ledger.Alias,
            ledger.Phone,
            ledger.Email,
            ledger.Address,
            ledger.Remarks,
            Status = ledger.Status ? "Active" : "Deleted"
        });

        var columnSettings = new Dictionary<string, PDFReportExportUtil.ColumnSetting>
        {
            [nameof(LedgerModel.Id)] = new()
            {
                DisplayName = "ID",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                },
                IncludeInTotal = false
            },

            [nameof(LedgerModel.Name)] = new() { DisplayName = "Ledger Name", IncludeInTotal = false },
            [nameof(LedgerModel.Code)] = new() { DisplayName = "Code", IncludeInTotal = false },
            ["Group"] = new() { DisplayName = "Group", IncludeInTotal = false },
            ["AccountType"] = new() { DisplayName = "Account Type", IncludeInTotal = false },
            ["StateUT"] = new() { DisplayName = "State/UT", IncludeInTotal = false },
            [nameof(LedgerModel.GSTNo)] = new() { DisplayName = "GST No", IncludeInTotal = false },
            [nameof(LedgerModel.PANNo)] = new() { DisplayName = "PAN No", IncludeInTotal = false },
            [nameof(LedgerModel.CINNo)] = new() { DisplayName = "CIN No", IncludeInTotal = false },
            [nameof(LedgerModel.Alias)] = new() { DisplayName = "Alias", IncludeInTotal = false },
            [nameof(LedgerModel.Phone)] = new() { DisplayName = "Phone", IncludeInTotal = false },
            [nameof(LedgerModel.Email)] = new() { DisplayName = "Email", IncludeInTotal = false },
            [nameof(LedgerModel.Address)] = new() { DisplayName = "Address", IncludeInTotal = false },
            [nameof(LedgerModel.Remarks)] = new() { DisplayName = "Remarks", IncludeInTotal = false },

            [nameof(LedgerModel.Status)] = new()
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
            nameof(LedgerModel.Id),
            nameof(LedgerModel.Name),
            nameof(LedgerModel.Code),
            "Group",
            "AccountType",
            "StateUT",
            nameof(LedgerModel.GSTNo),
            nameof(LedgerModel.PANNo),
            nameof(LedgerModel.CINNo),
            nameof(LedgerModel.Alias),
            nameof(LedgerModel.Phone),
            nameof(LedgerModel.Email),
            nameof(LedgerModel.Address),
            nameof(LedgerModel.Remarks),
            nameof(LedgerModel.Status)
        ];

        var stream = await PDFReportExportUtil.ExportToPdf(
            enrichedData,
            "LEDGER MASTER",
            null,
            null,
            columnSettings,
            columnOrder,
            useLandscape: true
        );

        var currentDateTime = await CommonData.LoadCurrentDateTime();
        var fileName = $"Ledger_Master_{currentDateTime:yyyyMMdd_HHmmss}.pdf";
        return (stream, fileName);
    }
}
