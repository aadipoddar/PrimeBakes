using PrimeBakes.Models.Common;

namespace PrimeBakes.Data.Operations.Analysis;

public static class AiData
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(AiData));

	public static Task<string> GeminiAsk(string prompt) =>
		ApiClient.Post<string>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(GeminiAsk)), prompt);
}
