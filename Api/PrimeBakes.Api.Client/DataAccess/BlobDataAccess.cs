using PrimeBakes.Models.Common;
using PrimeBakes.Models.DataAccess;

namespace PrimeBakes.Library.DataAccess;

public static class BlobStorageAccess
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(BlobStorageAccess));

	public static async Task<string> UploadFileToBlobStorage(Stream file, string fileName, BlobStorageContainers container) =>
		await ApiClient.Upload<string>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(UploadFileToBlobStorage)), file, fileName, new { fileName, container });

	public static async Task DeleteFileFromBlobStorage(string fileName, BlobStorageContainers container) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(DeleteFileFromBlobStorage)), new { }, new { fileName, container });

	public static async Task<List<string>> ListFilesInBlobStorage(BlobStorageContainers container) =>
		await ApiClient.Get<List<string>>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(ListFilesInBlobStorage)), new { container });

	public static async Task<(MemoryStream fileStream, string contentType)> DownloadFileFromBlobStorage(string url, BlobStorageContainers container) =>
		await ApiClient.GetForFile(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(DownloadFileFromBlobStorage)), new { url, container });
}
