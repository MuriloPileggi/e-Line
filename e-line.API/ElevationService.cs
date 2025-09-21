using System.Text.Json;
using Azure.Core.GeoJson;
using e_line.Api.Elevation;

namespace e_line.Api;

public class ElevationService
{
    private readonly HttpClient _httpClient;

    public ElevationService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("https://api.open-elevation.com/");
    }

    public async Task<List<double>> GetElevationForRouteAsync(IReadOnlyList<GeoPosition> routePoints)
    {
        // Convert route points into format the api expects
        var locations = routePoints.Select(p => new LocationPoint(p.Latitude, p.Longitude)).ToList();
        var request = new OpenElevationRequest(locations);

        // Make post request 
        var response = await _httpClient.PostAsJsonAsync("/api/v1/lookup", request);
        response.EnsureSuccessStatusCode();

        // Deserialize the response and extract just elevation values
        var elevationResponse = await response.Content.ReadFromJsonAsync<OpenElevationResponse>();

        return elevationResponse?.Results.Select(r => r.Elevation).ToList() ?? new List<double>();
    }
}