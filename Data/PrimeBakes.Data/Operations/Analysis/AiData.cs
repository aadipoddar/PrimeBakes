using System.Net;
using System.Text;
using System.Text.Json;

namespace PrimeBakes.Data.Operations.Analysis;

public static class AiData
{
	private static readonly HttpClient _httpClient = new() { Timeout = Timeout.InfiniteTimeSpan };

	public static async Task<string> GeminiAsk(string prompt, CancellationToken cancellationToken = default)
	{
		string[] _models = ["gemini-3.8-flash", "gemini-3.5-flash-lite"];

		if (string.IsNullOrWhiteSpace(prompt))
			throw new ArgumentException("A prompt is required.", nameof(prompt));

		if (prompt.Length > 100_000)
			throw new ArgumentException("The prompt is too long.", nameof(prompt));

		var apiKey = Secrets.GeminiApiKey;
		if (string.IsNullOrWhiteSpace(apiKey))
			throw new InvalidOperationException("The Gemini API key is not configured.");

		var body = new { contents = new[] { new { parts = new[] { new { text = prompt } } } } };
		var errors = new List<string>();

		foreach (var model in _models)
		{
			try
			{
				// Linked, so a timeout is told apart from the caller cancelling.
				using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
				timeout.CancelAfter(TimeSpan.FromMinutes(3));

				using var request = new HttpRequestMessage(HttpMethod.Post,
					$"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent")
				{
					Content = JsonContent.Create(body)
				};
				request.Headers.Add("x-goog-api-key", apiKey);

				using var response = await _httpClient.SendAsync(request, timeout.Token);
				var json = await response.Content.ReadAsStringAsync(timeout.Token);

				if (!response.IsSuccessStatusCode)
				{
					errors.Add($"{model}: HTTP {(int)response.StatusCode}");

					// Wrong model, quota or a server fault: try the next model.
					// Anything else (bad key, bad request) will fail the same way again.
					if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.TooManyRequests or HttpStatusCode.RequestTimeout
						|| (int)response.StatusCode >= 500)
						continue;

					break;
				}

				using var document = JsonDocument.Parse(json);
				if (document.RootElement.TryGetProperty("candidates", out var candidates)
					&& candidates.GetArrayLength() > 0
					&& candidates[0].TryGetProperty("content", out var content)
					&& content.TryGetProperty("parts", out var parts))
				{
					var answer = new StringBuilder();
					foreach (var part in parts.EnumerateArray())
						if (part.TryGetProperty("text", out var text))
							answer.Append(text.GetString());

					if (answer.Length > 0)
						return answer.ToString();
				}

				errors.Add($"{model}: response did not contain answer text");
			}
			catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
			{
				errors.Add($"{model}: timed out");
			}
			catch (HttpRequestException ex)
			{
				errors.Add($"{model}: {ex.Message}");
			}
			catch (JsonException)
			{
				errors.Add($"{model}: unreadable response");
			}
		}

		throw new InvalidOperationException($"Gemini could not generate an answer. {string.Join("; ", errors)}");
	}
}