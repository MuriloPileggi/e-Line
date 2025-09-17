using Microsoft.CognitiveServices.Speech;

namespace e_line.Api;

public class TextToSpeechService
{
    private readonly SpeechConfig _speechConfig;

    public TextToSpeechService(IConfiguration configuration)
    {
        var speechKey = configuration["AzureSpeech:SubscriptionKey"];
        var speechRegion = configuration["AzureSpeech:Region"];

        if (string.IsNullOrEmpty(speechKey) || string.IsNullOrEmpty(speechRegion))
        {
            throw new InvalidOperationException("Azure Speech Key or Region is empty");
        }

        _speechConfig = SpeechConfig.FromSubscription(speechKey, speechRegion);
        _speechConfig.SpeechSynthesisVoiceName = "pt-BR-FranciscaNeural";
        _speechConfig.SetSpeechSynthesisOutputFormat(SpeechSynthesisOutputFormat.Audio16Khz32KBitRateMonoMp3);
    }

    public async Task<byte[]?> SynthesizeSpeechAsync(string text)
    {
        using var synthesizer = new SpeechSynthesizer(_speechConfig, null);
        var result = await synthesizer.SpeakTextAsync(text);

        if (result.Reason == ResultReason.SynthesizingAudioCompleted)
        {
            return result.AudioData;
        }

        return null;
    }
}