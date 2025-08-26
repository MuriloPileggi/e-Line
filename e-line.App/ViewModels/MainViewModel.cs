using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace e_line.App.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private HttpClient _httpClient;
        private string _apiResponseMessage = "Press the button...";

        public string ApiResponseMessage
        {
            get => _apiResponseMessage;
            set
            {
                _apiResponseMessage = value;
                OnPropertyChanged();
            }
        }

        public MainViewModel(HttpClient httpClient)
        {
            // Http client is injected via Dependency Injection
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri("http://10.0.2.2:5148");
        }

        public async Task CallApi()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/test");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var json = JsonDocument.Parse(content);
                    var message = json.RootElement.GetProperty("message").GetString();
                    ApiResponseMessage = $"Success! {message}";
                }
                else
                {
                    ApiResponseMessage = $"Failed with status: {response.StatusCode}";
                }
            }
            catch (Exception ex)
            {
                ApiResponseMessage = $"Request failed: {ex.Message}";
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}