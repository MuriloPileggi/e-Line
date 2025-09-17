using Microsoft.CognitiveServices.Speech;

namespace e_line.App;

public class VoiceService
{
    private readonly SpeechConfig _speechConfig;

    public VoiceService()
    {
        string speechKey = "";
        string speechRegion = "";

        _speechConfig = SpeechConfig.FromSubscription(speechKey, speechRegion);
        _speechConfig.SpeechRecognitionLanguage = "pt-BR";
    }

    public async Task<string> ListenOnceAsync()
    {
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