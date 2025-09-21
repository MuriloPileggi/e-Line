using e_line.Api.Ocm;

namespace e_line.Api;

public class OpenChargeMapService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public OpenChargeMapService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["OpenChargeMap:ApiKey"] ?? throw new InvalidOperationException("OCM API Key not configured.");
        _httpClient.BaseAddress = new Uri("https://api.openchargemap.io/v3");
    }

    public async Task<List<ChargingStation>> GetChargersNearPoint(M_GeoPoint point)
    {
        var chargers = new List<ChargingStation>();

        // Construct the query URL for the OCM API
        var requestUri = $"/poi/?output=json&latitude={point.Latitude}&longitude={point.Longitude}&distance=20&distanceunit=km&maxresults=10&key={_apiKey}";

        try
        {
            var ocmPois = await _httpClient.GetFromJsonAsync<List<OcmPoi>>(requestUri);

            if (ocmPois is null) return chargers;

            // Map the detailed OCM data to our simple ChargingStation record
            foreach (var poi in ocmPois)
            {
                if (poi.AddressInfo?.Title is null) continue;

                chargers.Add(new ChargingStation(
                    Name: poi.AddressInfo.Title,
                    Location: new M_GeoPoint(poi.AddressInfo.Latitude, poi.AddressInfo.Longitude),
                    PowerKw: (int)(poi.Connections?.Max(c => c.PowerKw) ?? 50) // Get highest power available
                ));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching from OpenChargeMap: {ex.Message}");
        }

        return chargers;
    }
}