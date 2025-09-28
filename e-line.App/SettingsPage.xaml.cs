using System.Net.Http.Json;

namespace e_line.App;

public partial class SettingsPage : ContentPage
{
    private readonly HttpClient _httpClient;
    public SettingsPage()
    {
        InitializeComponent();

        // This handler is to bypass SSL certificate validation for local development
        // This should be removed or handled properly for production
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };

        _httpClient = new HttpClient(handler);

        // Use 10.0.2.2 to connect from the android emulator to host machine
        _httpClient.BaseAddress = new Uri("https://e-line-ase2dnd5haacbnhj.eastus-01.azurewebsites.net");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await TestApiConnection();
    }

    private async Task TestApiConnection()
    {
        try
        {
            // Call our /api/test endpoint and read the JSON response
            var response = await _httpClient.GetFromJsonAsync<ApiResponse>("/api/test");

            if (response != null && !string.IsNullOrEmpty(response.Message))
            {
                ApiMessageLabel.Text = $"Success! {response.Message}";
                ApiMessageLabel.TextColor = Colors.Green;
            }
        }
        catch (Exception ex)
        {
            // Display error if connection fails
            ApiMessageLabel.Text = $"Connection failed: {ex.Message}";
            ApiMessageLabel.TextColor = Colors.Red;
        }
    }
}

// A simple class to match JSON structure from our API
public class ApiResponse
{
    public string? Message { get; set; }
}