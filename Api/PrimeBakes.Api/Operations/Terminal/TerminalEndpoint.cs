using PrimeBakes.Data.Operations.Terminal;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Operations.Terminal;

namespace PrimeBakes.Api.Operations.Terminal;

public class TerminalEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(TerminalEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapGet(nameof(TerminalData.LoadTerminalByMachineId),
			(string machineId) => TerminalData.LoadTerminalByMachineId(machineId));

		group.MapPost(nameof(TerminalData.SaveTransaction),
			(TerminalModel terminal, int userId, string formFactor, string platform, decimal? latitude, decimal? longitude) =>
				TerminalData.SaveTransaction(terminal, userId, formFactor, platform, latitude, longitude));
	}
}
