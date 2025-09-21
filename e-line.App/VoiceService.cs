using Microsoft.CognitiveServices.Speech;
using System.Net.Http.Json;

namespace e_line.App;

public class VoiceService
{
    private SpeechConfig? _speechConfig;
    private bool _isInitialized = false;

    public VoiceService() { }

    // Async initialization method
    public async Task InitializeAsync(HttpClient httpClient)
    {
        if (_isInitialized) return;

        try
        {
            // Call backend to get a temporary auth token
            var response = await httpClient.GetFromJsonAsync<SpeechTokenResponse>("/api/auth/speech-token");
            if (response?.Token is not null && response?.Region is not null)
            {
                // Configure SDK using the token 
                _speechConfig = SpeechConfig.FromAuthorizationToken(response.Token, response.Region);
                _speechConfig.SpeechRecognitionLanguage = "pt-BR";
                _isInitialized = true;
                System.Diagnostics.Debug.WriteLine("[DEBUG] Voice Service Initialized SUCCESSFULLY.");
            }
            else
            {
                // If we get a valid response but it's empty, create a specific error.
                throw new Exception("Received a null token or region from the API.");
            }
        }
        catch (Exception ex)
        {
            _isInitialized = false;
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Voice service failed with exception: {ex.Message}");
            throw;
        }
    }

    public async Task<string> ListenOnceAsync()
    {
        // Ensure service is initialized before trying to use it
        if (!_isInitialized)
        {
            return "Erro: Serviço de voz não inicializado.";
        }
        
        try
        {
            using var speechRecognizer = new SpeechRecognizer(_speechConfig);

            var result = await speechRecognizer.RecognizeOnceAsync();

            return result.Reason switch
            {
                ResultReason.RecognizedSpeech => result.Text,
                ResultReason.NoMatch => "Não consegui entender. Tente de novo.",
                ResultReason.Canceled => "Ocorreu um erro. Tente mais tarde.",
                _ => "Ação de voz cancelada."
            };
        }
        catch (Exception ex)
        {
            return $"Erro: {ex.Message}";
        }
    }
}

// Helper class to deserialize the token response
public record SpeechTokenResponse(string Token, string Region);