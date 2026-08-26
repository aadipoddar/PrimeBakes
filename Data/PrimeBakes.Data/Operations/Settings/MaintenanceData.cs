using PrimeBakes.Models.Common;

namespace PrimeBakes.Data.Operations.Settings;

public static class MaintenanceData
{
	public static async Task RebuildIndexes() =>
		await SqlDataAccess.LoadData<int, dynamic>(OperationNames.RebuildIndexes, new { });
}
