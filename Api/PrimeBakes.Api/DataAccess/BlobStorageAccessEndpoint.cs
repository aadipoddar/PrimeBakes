using PrimeBakes.Data.DataAccess;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.DataAccess;

namespace PrimeBakes.Api.DataAccess;

public class BlobStorageAccessEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(BlobStorageAccessEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapPost(nameof(BlobStorageAccess.UploadFileToBlobStorage), async (IFormFile file, string fileName, BlobStorageContainers container) =>
		{
			await using var stream = file.OpenReadStream();
			return TypedResults.Text(await BlobStorageAccess.UploadFileToBlobStorage(stream, fileName, container));
		}).DisableAntiforgery();

		group.MapPost(nameof(BlobStorageAccess.DeleteFileFromBlobStorage),
			(string fileName, BlobStorageContainers container) => BlobStorageAccess.DeleteFileFromBlobStorage(fileName, container));

		group.MapGet(nameof(BlobStorageAccess.ListFilesInBlobStorage),
			(BlobStorageContainers container) => BlobStorageAccess.ListFilesInBlobStorage(container));

		group.MapGet(nameof(BlobStorageAccess.DownloadFileFromBlobStorage), async (string url, BlobStorageContainers container) =>
		{
			var (fileStream, contentType) = await BlobStorageAccess.DownloadFileFromBlobStorage(url, container);
			return TypedResults.File(fileStream.ToArray(), contentType ?? Helper.ExportContentType);
		});
	}
}
