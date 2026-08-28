using PrimeBakes.Models.Operations.Location;

using System.Text;
using System.Text.Json;

namespace PrimeBakes.Data.Operations.Location;

public static class GoogleMapsData
{
	private static readonly HttpClient _httpClient = new();

	public static async Task<List<PlaceModel>> SearchPlaces(string input)
	{
		using var request = new HttpRequestMessage(HttpMethod.Post, "https://places.googleapis.com/v1/places:searchText");
		request.Headers.Add("X-Goog-Api-Key", Secrets.GoogleMapsApiKey);
		request.Headers.Add("X-Goog-FieldMask", "places.formattedAddress,places.location");
		request.Content = new StringContent(JsonSerializer.Serialize(new { textQuery = input }), Encoding.UTF8, "application/json");

		using var response = await _httpClient.SendAsync(request);
		using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

		if (!json.RootElement.TryGetProperty("places", out var places))
			return [];

		return [.. places.EnumerateArray().Select(place => new PlaceModel
		{
			Description = place.GetProperty("formattedAddress").GetString(),
			Latitude = place.GetProperty("location").GetProperty("latitude").GetDecimal(),
			Longitude = place.GetProperty("location").GetProperty("longitude").GetDecimal()
		})];
	}
}
