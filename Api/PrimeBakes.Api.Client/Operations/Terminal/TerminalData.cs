using PrimeBakes.Models.Common;
using PrimeBakes.Models.Operations.Terminal;

namespace PrimeBakes.Data.Operations.Terminal;

public static class TerminalData
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(TerminalData));

	public static async Task<TerminalModel> LoadTerminalByMachineId(string machineId) =>
		await ApiClient.Get<TerminalModel>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(LoadTerminalByMachineId)), new { machineId });

	public static async Task<int> SaveTransaction(TerminalModel terminal) =>
		await ApiClient.Post<int>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(SaveTransaction)), terminal);
}
