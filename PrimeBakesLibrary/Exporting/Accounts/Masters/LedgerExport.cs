using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Accounts.Masters;

namespace PrimeBakesLibrary.Exporting.Accounts.Masters;

public static class LedgerExport
{
	public static async Task<(MemoryStream stream, string fileName)> ExportMaster(
		IEnumerable<LedgerModel> ledgerData,
		ReportExportType exportType)
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

		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			[nameof(LedgerModel.Id)] = new() { DisplayName = "ID", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(LedgerModel.Name)] = new() { DisplayName = "Ledger Name", Alignment = CellAlignment.Left, IsRequired = true },
			[nameof(LedgerModel.Code)] = new() { DisplayName = "Code", Alignment = CellAlignment.Left, IsRequired = true },
			["Group"] = new() { DisplayName = "Group", Alignment = CellAlignment.Center },
			["AccountType"] = new() { DisplayName = "Account Type", Alignment = CellAlignment.Center },
			["StateUT"] = new() { DisplayName = "State/UT", Alignment = CellAlignment.Center },
			[nameof(LedgerModel.GSTNo)] = new() { DisplayName = "GST No", Alignment = CellAlignment.Left },
			[nameof(LedgerModel.PANNo)] = new() { DisplayName = "PAN No", Alignment = CellAlignment.Left },
			[nameof(LedgerModel.CINNo)] = new() { DisplayName = "CIN No", Alignment = CellAlignment.Left },
			[nameof(LedgerModel.Alias)] = new() { DisplayName = "Alias", Alignment = CellAlignment.Left },
			[nameof(LedgerModel.Phone)] = new() { DisplayName = "Phone", Alignment = CellAlignment.Left },
			[nameof(LedgerModel.Email)] = new() { DisplayName = "Email", Alignment = CellAlignment.Left },
			[nameof(LedgerModel.Address)] = new() { DisplayName = "Address", Alignment = CellAlignment.Left },
			[nameof(LedgerModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = CellAlignment.Left },
			[nameof(LedgerModel.Status)] = new() { DisplayName = "Status", Alignment = CellAlignment.Center, IncludeInTotal = false }
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

		var currentDateTime = await CommonData.LoadCurrentDateTime();
		var fileName = $"Ledger_Master_{currentDateTime:yyyyMMdd_HHmmss}";

		if (exportType == ReportExportType.PDF)
		{
			var stream = await PDFReportExportUtil.ExportToPdf(
				enrichedData,
				"LEDGER MASTER",
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
				"LEDGER",
				"Ledger Data",
				null,
				null,
				columnSettings,
				columnOrder
			);

			return (stream, fileName + ".xlsx");
		}
	}
}
