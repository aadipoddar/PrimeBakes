using PrimeBakes.Models.Common;

namespace PrimeBakes.Data.Operations.Analysis;

public static class GeminiData
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(GeminiData));

	public static Task<string> Ask(string prompt) =>
		ApiClient.Post<string>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(Ask)), prompt);
}
