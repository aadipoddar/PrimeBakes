using PrimeBakes.Data.Common;
using PrimeBakes.Data.Operations.AuditTrail;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Operations.AuditTrail;
using PrimeBakes.Models.Operations.Terminal;

namespace PrimeBakes.Data.Operations.Terminal;

public static class TerminalData
{
	private static async Task<int> InsertTerminal(TerminalModel terminal, SqlDataAccessTransaction transaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(OperationNames.InsertTerminal, terminal, transaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert Terminal.");

	public static async Task<TerminalModel> LoadTerminalByMachineId(string machineId, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		(await SqlDataAccess.LoadData<TerminalModel, dynamic>(OperationNames.LoadTerminalByMachineId, new { MachineId = machineId }, sqlDataAccessTransaction)).FirstOrDefault();

	private static void ValidateTransaction(TerminalModel terminal)
	{
		terminal.MachineId = terminal.MachineId?.Trim();
		terminal.MachineName = string.IsNullOrWhiteSpace(terminal.MachineName) ? null : terminal.MachineName.Trim();
		terminal.Status = true;

		if (string.IsNullOrWhiteSpace(terminal.MachineId))
			throw new Exception("Could not identify this machine. Terminal registration is not possible.");

		if (terminal.UserId <= 0)
			throw new Exception("User is required. Please select a valid user.");
	}

	public static async Task<int> SaveTransaction(TerminalModel terminal, int userId, string formFactor, string platform, decimal? latitude, decimal? longitude)
	{
		ValidateTransaction(terminal);

		var terminals = await CommonData.LoadTableData<TerminalModel>(OperationNames.Terminal);
		var existing = terminals.FirstOrDefault(t => t.MachineId == terminal.MachineId);

		terminal.Id = existing?.Id ?? 0;
		terminal.TerminalNo = existing?.TerminalNo ?? terminals.Select(t => t.TerminalNo).DefaultIfEmpty(0).Max() + 1;

		return await SqlDataAccessTransaction.Run(async transaction =>
		{
			terminal.Id = await InsertTerminal(terminal, transaction);

			var diff = AuditTrailData.GetDifference(existing, terminal);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = existing is not null ? AuditTrailActionTypes.Update.ToString() : AuditTrailActionTypes.Insert.ToString(),
				TableName = OperationNames.Terminal,
				RecordNo = $"T{terminal.TerminalNo}",
				RecordValue = existing is not null ? diff : null,
				CreatedBy = userId,
				CreatedFormFactor = formFactor,
				CreatedPlatform = platform,
				CreatedLatitude = latitude,
				CreatedLongitude = longitude
			}, transaction);

			return terminal.Id;
		});
	}
}