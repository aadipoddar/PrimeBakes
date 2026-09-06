using PrimeBakes.Data.Common;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Operations.Terminal;

namespace PrimeBakes.Data.Operations.Terminal;

public static class TerminalData
{
	private static async Task<int> InsertTerminal(TerminalModel terminal, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(OperationNames.InsertTerminal, terminal, sqlDataAccessTransaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert Terminal.");

	public static async Task<TerminalModel> LoadTerminalByMachineId(string machineId, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		(await SqlDataAccess.LoadData<TerminalModel, dynamic>(OperationNames.LoadTerminalByMachineId, new { MachineId = machineId }, sqlDataAccessTransaction)).FirstOrDefault();

	internal static async Task UpdateLastSyncedAt(string machineId)
	{
		if (string.IsNullOrWhiteSpace(machineId))
			return;

		var terminal = await LoadTerminalByMachineId(machineId);
		if (terminal is null)
			return;

		terminal.LastSyncedAt = await CommonData.LoadCurrentDateTime();
		await InsertTerminal(terminal);
	}

	private static async Task ValidateTransaction(TerminalModel terminal, SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		terminal.MachineId = terminal.MachineId?.Trim();
		terminal.MachineName = terminal.MachineName?.Trim();

		if (string.IsNullOrWhiteSpace(terminal.MachineId) || string.IsNullOrWhiteSpace(terminal.MachineName))
			throw new Exception("Could not identify this machine. Terminal registration is not possible.");

		if (terminal.UserId <= 0)
			throw new Exception("User is required. Please select a valid user.");

		var existing = await LoadTerminalByMachineId(terminal.MachineId, sqlDataAccessTransaction);

		terminal.Id = existing?.Id ?? 0;
		terminal.TerminalNo = existing?.TerminalNo
			?? (await CommonData.LoadTableData<TerminalModel>(OperationNames.Terminal, sqlDataAccessTransaction))
			.Select(t => t.TerminalNo).DefaultIfEmpty(0).Max() + 1;

		terminal.LastSyncedAt ??= existing?.LastSyncedAt;
	}

	public static async Task<int> SaveTransaction(TerminalModel terminal)
	{
		await ValidateTransaction(terminal);
		return terminal.Id = await InsertTerminal(terminal);
	}
}
