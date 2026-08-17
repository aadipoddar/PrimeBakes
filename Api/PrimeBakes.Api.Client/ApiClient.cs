using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PrimeBakes.Data;

public static class ApiClient
{
	#region Setup
	private static HttpClient _http;

	private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web)
	{
		IncludeFields = true,
		Converters = { new DateOnlyJsonConverter(), new TimeOnlyJsonConverter() }
	};

	private static HttpClient Http => _http
		?? throw new InvalidOperationException("ApiClient.Init(HttpClient) must be called during startup before any request.");

	public static void Init(HttpClient http) => _http = http;
	#endregion

	#region Requests
	public static async Task<T> Get<T>(string route, object query = null) =>
		await ReadJson<T>(await Send(HttpMethod.Get, route, query));

	public static async Task Post(string route, object body, object query = null) =>
		await Send(HttpMethod.Post, route, query, Json(body));

	public static async Task<T> Post<T>(string route, object body, object query = null) =>
		await ReadJson<T>(await Send(HttpMethod.Post, route, query, Json(body)));
	#endregion

	#region Files
	public static async Task<(MemoryStream stream, string contentType)> GetForFile(string route, object query = null)
	{
		var response = await Send(HttpMethod.Get, route, query);
		return (await ReadStream(response), response.Content.Headers.ContentType?.ToString());
	}

	public static async Task<(MemoryStream stream, string fileName)> PostForFile(string route, object body, object query = null)
	{
		var response = await Send(HttpMethod.Post, route, query, Json(body));
		return (await ReadStream(response), ReadFileName(response));
	}

	public static async Task<T> Upload<T>(string route, Stream file, string fileName, object query = null)
	{
		using var content = new MultipartFormDataContent { { new StreamContent(file), "file", fileName } };
		return await ReadJson<T>(await Send(HttpMethod.Post, route, query, content));
	}
	#endregion

	#region Send
	private static JsonContent Json(object body) =>
		JsonContent.Create(body, options: _json);

	private static async Task<HttpResponseMessage> Send(HttpMethod method, string route, object query, HttpContent content = null)
	{
		using var request = new HttpRequestMessage(method, route + ToQuery(query)) { Content = content };

		var response = await Http.SendAsync(request);
		await EnsureSuccess(response);

		return response;
	}
	#endregion

	#region Responses
	private static async Task<T> ReadJson<T>(HttpResponseMessage response) =>
		typeof(T) == typeof(string)
			? (T)(object)await response.Content.ReadAsStringAsync()
			: await response.Content.ReadFromJsonAsync<T>(_json);

	private static async Task<MemoryStream> ReadStream(HttpResponseMessage response) =>
		new(await response.Content.ReadAsByteArrayAsync());

	private static string ReadFileName(HttpResponseMessage response) =>
		(response.Content.Headers.ContentDisposition?.FileNameStar
			?? response.Content.Headers.ContentDisposition?.FileName)?.Trim('"')
			?? "download";

	private static async Task EnsureSuccess(HttpResponseMessage response)
	{
		if (response.IsSuccessStatusCode)
			return;

		string message = null;
		try { message = (await response.Content.ReadFromJsonAsync<ApiError>(_json))?.Message; }
		catch { }

		throw new Exception(string.IsNullOrWhiteSpace(message)
			? $"Request failed ({(int)response.StatusCode})."
			: message);
	}

	private sealed record ApiError(string Message);
	#endregion

	#region Query
	private static string ToQuery(object query)
	{
		if (query is null)
			return string.Empty;

		var pairs = query.GetType().GetProperties()
			.Select(p => (p.Name, Value: p.GetValue(query)))
			.Where(p => p.Value is not null)
			.Select(p => $"{p.Name}={Uri.EscapeDataString(Format(p.Value))}");

		return string.Join("&", pairs) is { Length: > 0 } queryString ? "?" + queryString : string.Empty;
	}

	private static string Format(object value) => value switch
	{
		DateOnly d => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
		DateTime dt => dt.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture),
		TimeOnly t => t.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
		IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
		_ => value.ToString()
	};
	#endregion
}

public sealed class DateOnlyJsonConverter : JsonConverter<DateOnly>
{
	private const string _format = "yyyy-MM-dd";

	public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var value = reader.GetString();

		if (string.IsNullOrWhiteSpace(value))
			return default;

		if (DateOnly.TryParse(value, CultureInfo.InvariantCulture, out var date))
			return date;

		if (DateTime.TryParse(value, CultureInfo.InvariantCulture, out var dateTime))
			return DateOnly.FromDateTime(dateTime);

		throw new JsonException($"Could not convert \"{value}\" to {nameof(DateOnly)}.");
	}

	public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options) =>
		writer.WriteStringValue(value.ToString(_format, CultureInfo.InvariantCulture));
}

public sealed class TimeOnlyJsonConverter : JsonConverter<TimeOnly>
{
	private const string _format = "HH:mm:ss";

	public override TimeOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var value = reader.GetString();

		if (string.IsNullOrWhiteSpace(value))
			return default;

		if (TimeOnly.TryParse(value, CultureInfo.InvariantCulture, out var time))
			return time;

		if (DateTime.TryParse(value, CultureInfo.InvariantCulture, out var dateTime))
			return TimeOnly.FromDateTime(dateTime);

		throw new JsonException($"Could not convert \"{value}\" to {nameof(TimeOnly)}.");
	}

	public override void Write(Utf8JsonWriter writer, TimeOnly value, JsonSerializerOptions options) =>
		writer.WriteStringValue(value.ToString(_format, CultureInfo.InvariantCulture));
}
