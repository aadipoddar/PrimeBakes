using PrimeBakes.Data;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Inventory.RawMaterial;

namespace PrimeBakes.Data.Inventory.RawMaterial;

public static class RawMaterialData
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(RawMaterialData));

	public static async Task<int> InsertRawMaterial(RawMaterialModel rawMaterial) =>
		await ApiClient.Post<int>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(InsertRawMaterial)), rawMaterial);

	public static async Task DeleteTransaction(RawMaterialModel rawMaterial, int userId, string platform) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(DeleteTransaction)), rawMaterial, new { userId, platform });

	public static async Task RecoverTransaction(RawMaterialModel rawMaterial, int userId, string platform) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(RecoverTransaction)), rawMaterial, new { userId, platform });

	public static async Task<int> SaveTransaction(RawMaterialModel rawMaterial, int userId, string platform) =>
		await ApiClient.Post<int>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(SaveTransaction)), rawMaterial, new { userId, platform });
}
