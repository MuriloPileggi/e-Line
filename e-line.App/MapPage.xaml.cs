using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Maui.Extensions;

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
                new M_GeoPoint(-23.4356, -46.4778),
                new M_GeoPoint(-23.573964069279068, -46.62321774553537),
                EvModelId: 2, // BYD Dolphin
                StartSoC: 0.95
            );

            // Call our mock API endpoint
            var response = await _httpClient.PostAsJsonAsync("/api/route/plan", request);
            response.EnsureSuccessStatusCode();
            // Read response as raw string
            var responseBody = await response.Content.ReadAsStringAsync();
            // Parse the string into a generic JSON Object
            var routePlanNode = JsonNode.Parse(responseBody);

            if (routePlanNode is null)
            {
                await DisplayAlert("Error", "Failed to parse route plan.", "OK");
                return;
            }

            // Extract data required for popup
            var totalTime = routePlanNode["totalTimeMinutes"]?.GetValue<int>() ?? 0;
            var totalDistance = routePlanNode["totalDistanceKm"]?.GetValue<double>() ?? 0.0;
            var stopsArray = routePlanNode["stops"]?.AsArray();
            
           // Create an instance of our new popup
            var tripPopup = new TripPreviewPopup();

            // Set its BindingContext with the route data
            tripPopup.BindingContext = new
            {
                TotalTime = $"{totalTime} min",
                TotalDistance = $"{totalDistance:F1} km",
                HasChargingStop = stopsArray?.Count > 0,
                ChargingStopSummary = stopsArray?.Count > 0 ? $"{stopsArray.Count} paradas(s) planejadas" : ""
            };

            // Use ShowPopupAsync method to display our newly created popup on screen
            await this.ShowPopupAsync(tripPopup);
            
            var startConfirmed = await tripPopup.ResultTaskCompletionSource.Task;

            // Check the result, the popup can be dismissed by tapping outside which returns null
            if (startConfirmed)
            {
                await DrawRouteOnMap(routePlanNode);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to plan route: {ex.Message}", "OK");
        }
    }

    private async Task DrawRouteOnMap(JsonNode? routePlanResponse)
    {
        if (routePlanResponse is null) return;

        var polylineNode = routePlanResponse["polyline"];
        var stopsNode = routePlanResponse["stops"];
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

        PlanRouteButton.IsVisible = false;
    }
}

// --- Data Contracts ---
// These need to be accessible to the MapPage, so we define them here
// In a larger app, these would be in a shared project.
public record RoutePlanRequest(M_GeoPoint Origin, M_GeoPoint Destination, int EvModelId ,double StartSoC);

public record RoutePlanResponse(string Polyline, List<ChargingStation> Stops, double TotalDistanceKm, int TotalTimeMinutes);
public record ChargingStation(string Name, M_GeoPoint Location, int PowerKw);
public record M_GeoPoint(double Latitude, double Longitude);