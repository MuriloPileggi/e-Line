using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace e_line.App;

public partial class MapPage : ContentPage
{
    private HttpClient _httpClient;

    public MapPage()
    {
        InitializeComponent();

        // We point the WebView to our local HTML file
        // which contains the Azure Maps logic.
        mapWebView.Source = new UrlWebViewSource { Url = "map.html" };

        // Use HTTP for local development against the API
        _httpClient = new HttpClient();
        const string apiPort = "5148";
        _httpClient.BaseAddress = new Uri($"http://10.0.2.2:{apiPort}");
    }

    private async void OnPlanRouteClicked(object sender, EventArgs e)
    {
        try
        {
            // Create a dummy request object
            var request = new RoutePlanRequest(
            new GeoPoint(-23.4356, -46.4778),
            new GeoPoint(-23.5613, -46.6565),
            1);

            // Call our mock API endpoint
            var response = await _httpClient.PostAsJsonAsync("/api/route/plan", request);
            response.EnsureSuccessStatusCode();

            // Read response as raw string
            var responseBody = await response.Content.ReadAsStringAsync();

            // Parse the string into a generic JSON Object
            var routePlanNode = JsonNode.Parse(responseBody);

            if (routePlanNode != null)
            {
                var polylineNode = routePlanNode["polyline"];
                var stopsNode = routePlanNode["stops"];
                // 1. Extract the polyline data as raw JSON string
                if (polylineNode != null)
                {
                    var polylineJson = polylineNode.ToJsonString();
                    await mapWebView.EvaluateJavaScriptAsync($"drawRoute({polylineJson})");
                }

                // 2. Extract the stops array as raw JSON string
                if (stopsNode != null)
                {
                    var stopsJson = stopsNode.ToJsonString();
                    await mapWebView.EvaluateJavaScriptAsync($"addChargingStops('{stopsJson}')");
                }
            }

            /* var routePlan = await response.Content.ReadFromJsonAsync<RoutePlanResponse>();

                if (routePlan != null)
                {
                    // Use the webview to execute our JS functions
                    // We serialize our C# objects back into JSON strings for JS to understand

                    // 1. Draw the route polyline
                    var polylineJson = JsonSerializer.Serialize(routePlan.Polyline);
                    await mapWebView.EvaluateJavaScriptAsync($"drawRoute({polylineJson})");

                    // 2. Add the charging stations pins
                    var stopsJson = JsonSerializer.Serialize(routePlan.Stops);
                    await mapWebView.EvaluateJavaScriptAsync($"addChargingStops({stopsJson})");
                } */

                PlanRouteButton.IsVisible = false;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to plan route: {ex.Message}", "OK");
        }
    }
}

// --- Data Contracts ---
// These need to be accessible to the MapPage, so we define them here
// In a larger app, these would be in a shared project.
public record RoutePlanRequest(GeoPoint Origin, GeoPoint Destination, int EvModelId);
public record RoutePlanResponse(string Polyline, List<ChargingStation> Stops);
public record ChargingStation(string Name, GeoPoint Location, int PowerKw);
public record GeoPoint(double Latitude, double Longitude);