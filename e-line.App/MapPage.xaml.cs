using System.Text.Json;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Maui.Extensions;

namespace e_line.App;

public partial class MapPage : ContentPage
{
    private HttpClient _httpClient;
    private readonly VoiceService _voiceService;

    // Flag to make sure we initialize only once
    private bool _isInitialized = false;

    public MapPage()
    {
        InitializeComponent();

        // Instantitate Voice Service
        _voiceService = new VoiceService();

        // We point the WebView to our local HTML file
        // which contains the Azure Maps logic.
        mapWebView.Source = new UrlWebViewSource { Url = "map.html" };

        // Use HTTP for local development against the API
        //_httpClient = new HttpClient();
        //const string apiPort = "5148";
        //_httpClient.BaseAddress = new Uri($"http://10.0.2.2:{apiPort}"); // Emulator

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://e-line-ase2dnd5haacbnhj.eastus-01.azurewebsites.net")
        };

        // Subscribe to the Unloaded event for cleanup (Force close media player)
        Unloaded += OnMapPageUnloaded;
    }

    // Wait for webview to be loaded before initializing services
    private async void OnMapWebViewNavigated(object? sender, WebNavigatedEventArgs e)
    {
        if (_isInitialized) return;
        _isInitialized = true; // Set flag to prevent running more than once

        try
        {
            // Fetch the maps key
            var response = await _httpClient.GetFromJsonAsync<MapsKeyResponse>("/api/config/maps-key");

            if (response?.Key is not null)
            {
                // Initialize the map with the key
                await mapWebView.EvaluateJavaScriptAsync($"initializeMap('{response.Key}')");
            }
            else
            {
                await DisplayAlert("Map Error", "Did not receive a valid maps key", "OK");
            }

            // Initialize voice service
            await _voiceService.InitializeAsync(_httpClient);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to initialize services: {ex.Message}", "OK");
        }

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
                StartSoC: 0.20
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
            tripPopup.BindingContext = new TripPreviewViewModel
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

    private async void OnVoiceClicked(object sender, EventArgs e)
    {
        // Check for permissions
        var hasPermission = await CheckAndRequestMicrophonePermissionAsync();
        if (!hasPermission) return;

        // Show the label and provide feedback to user
        TranscriptionLabel.Text = "Ouvindo...";
        TranscriptionLabel.IsVisible = true;
        VoiceButton.BackgroundColor = Colors.Red;

        // Call the service to listen for speech
        var recognizedText = await _voiceService.ListenOnceAsync();

        // Update UI with result
        TranscriptionLabel.Text = recognizedText;

        // Only proceed if speech was actually recognized 
        if (recognizedText != "Não consegui entender. Tente de novo." &&
            recognizedText != "Ocorreu um erro. Tente mais tarde." &&
            !recognizedText.StartsWith("Erro:"))
        {
            try
            {
                // Send the recognized text to voice api endpoint
                var request = new VoiceIntentRequest(recognizedText);
                var response = await _httpClient.PostAsJsonAsync("/api/voice/intent", request);

                if (response.IsSuccessStatusCode)
                {
                    // Read the audio MP3 data from the response
                    var audioData = await response.Content.ReadAsByteArrayAsync();

                    var tcs = new TaskCompletionSource<bool>();

                    void OnStateChanged(object? s, CommunityToolkit.Maui.Core.MediaStateChangedEventArgs e)
                    {
                        // Check if playback has stopped
                        if (e.NewState == CommunityToolkit.Maui.Core.MediaElementState.Stopped)
                        {
                            // Unsubscribe to avoid memory leaks
                            AudioPlayer.StateChanged -= OnStateChanged;
                            // Signal that task is complete
                            tcs.TrySetResult(true);
                        }
                    }

                    AudioPlayer.StateChanged += OnStateChanged;

                    // Create a path for a temporary file in the app's cache directory
                    var tempFilePath = Path.Combine(FileSystem.CacheDirectory, "response.mp3");

                    // Write the audio bytes to this temporary file
                    await File.WriteAllBytesAsync(tempFilePath, audioData);

                    // Play the audio through media element in map page
                    AudioPlayer.Source = MediaSource.FromFile(tempFilePath);
                    AudioPlayer.Play();

                    await tcs.Task;
                }
                else
                {
                    TranscriptionLabel.Text = "Erro na API de voz.";
                }
            }
            catch (Exception)
            {
                TranscriptionLabel.Text = "Não foi possível conectar à API.";
            }
        }

        VoiceButton.BackgroundColor = Colors.DodgerBlue;

        // Hide the label after a few seconds
        await Task.Delay(5000);
        TranscriptionLabel.IsVisible = false;
    }

    private async Task<bool> CheckAndRequestMicrophonePermissionAsync()
    {
        var status = await Permissions.CheckStatusAsync<Permissions.Microphone>();

        if (status == PermissionStatus.Granted) return true;

        // If permission to mic is not granted we ask for it
        status = await Permissions.RequestAsync<Permissions.Microphone>();

        if (status == PermissionStatus.Granted) return true; // Check permission status again after requesting

        // If after second check we still don't have permission throw error on screen
        await DisplayAlert("Permission Denied", "A permissão do microfone é necessária para usar o assistente de voz.", "OK");
        return false;
    }

    private void OnMapPageUnloaded(object? sender, EventArgs e)
    {
        // Stop playback and release the native resources used by the media element
        AudioPlayer.Stop();
        AudioPlayer.Handler?.DisconnectHandler();
    }

}

// --- Data Contracts ---
// These need to be accessible to the MapPage, so we define them here
// In a larger app, these would be in a shared project.
public record RoutePlanRequest(M_GeoPoint Origin, M_GeoPoint Destination, int EvModelId ,double StartSoC);
public record RoutePlanResponse(string Polyline, List<ChargingStation> Stops, double TotalDistanceKm, int TotalTimeMinutes);
public record ChargingStation(string Name, M_GeoPoint Location, int PowerKw);
public record M_GeoPoint(double Latitude, double Longitude);
public record VoiceIntentRequest(string Text);
public record MapsKeyResponse(string Key);