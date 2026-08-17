using PrimeBakes.Data;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Restaurant.Dining;

namespace PrimeBakes.Data.Restaurant.Dining;

public static class DiningTableData
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(DiningTableData));

	public static async Task<int> InsertDiningTable(DiningTableModel diningTable) =>
		await ApiClient.Post<int>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(InsertDiningTable)), diningTable);

	public static async Task DeleteTransaction(DiningTableModel diningTable, int userId, string platform) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(DeleteTransaction)), diningTable, new { userId, platform });

	public static async Task RecoverTransaction(DiningTableModel diningTable, int userId, string platform) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(RecoverTransaction)), diningTable, new { userId, platform });

	public static async Task<int> SaveTransaction(DiningTableModel diningTable, int userId, string platform) =>
		await ApiClient.Post<int>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(SaveTransaction)), diningTable, new { userId, platform });
}
