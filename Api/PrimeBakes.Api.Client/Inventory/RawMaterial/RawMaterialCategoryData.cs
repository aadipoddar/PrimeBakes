using PrimeBakes.Data;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Inventory.RawMaterial;

namespace PrimeBakes.Data.Inventory.RawMaterial;

public static class RawMaterialCategoryData
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(RawMaterialCategoryData));

	public static async Task DeleteTransaction(RawMaterialCategoryModel category, int userId, string platform) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(DeleteTransaction)), category, new { userId, platform });

	public static async Task RecoverTransaction(RawMaterialCategoryModel category, int userId, string platform) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(RecoverTransaction)), category, new { userId, platform });

	public static async Task<int> SaveTransaction(RawMaterialCategoryModel category, int userId, string platform) =>
		await ApiClient.Post<int>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(SaveTransaction)), category, new { userId, platform });
}
