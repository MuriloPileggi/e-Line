using System.Net.Http.Json;

namespace e_line.App;

public partial class SettingsPage : ContentPage
{
    private List<EVModel> _evModels = new();
    private readonly HttpClient _httpClient;
    private bool _isInitialized = false;

    public SettingsPage()
    {
        InitializeComponent();
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://e-line-ase2dnd5haacbnhj.eastus-01.azurewebsites.net")
        };
    }

    // Lifecycle method that runs every time the page is shown
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_isInitialized) return; // Only load the list from API once

        try
        {
            // Load list of car models from Database
            _evModels = await _httpClient.GetFromJsonAsync<List<EVModel>>("/api/evmodels") ?? new();
            EVPicker.ItemsSource = _evModels;
            _isInitialized = true;

            // After loading, set the picker to the user's previously saved choice
            var savedModelId = Preferences.Get("SelectedEVModelId", 1); // Default to ID 1
            var selectedModel = _evModels.FirstOrDefault(m => m.Id == savedModelId);
            if (selectedModel is not null)
            {
                EVPicker.SelectedItem = selectedModel;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Could not load EV models: {ex.Message}", "OK");
        }
    }

    private void OnEVPickerSelectedIndexChanged(object? sender, EventArgs e)
    {
        if (EVPicker.SelectedItem is EVModel selectedModel)
        {
            // When the user selects a car, save its ID to device's local storage
            Preferences.Set("SelectedEVModelId", selectedModel.Id);
        }
    }
}