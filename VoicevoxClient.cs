using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace VoicevoxEnterPlayer;

public class VoicevoxClient
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly JsonSerializerOptions _jsonOptions;

    public VoicevoxClient(HttpClient httpClient, string baseUrl = "http://localhost:50021")
    {
        _httpClient = httpClient;
        _baseUrl = baseUrl.TrimEnd('/');
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<SpeakerInfo[]?> GetSpeakersAsync()
    {
        var response = await _httpClient.GetAsync($"{_baseUrl}/speakers");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<SpeakerInfo[]>(json, _jsonOptions);
    }

    public async Task<AudioQuery> CreateAudioQueryAsync(string text, int speakerId)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("テキストが空です", nameof(text));
            
        var encodedText = Uri.EscapeDataString(text);
        var url = $"{_baseUrl}/audio_query?text={encodedText}&speaker={speakerId}";
        
        var response = await _httpClient.PostAsync(url, null);
        var json = await response.Content.ReadAsStringAsync();
        
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"audio_query 失敗: {(int)response.StatusCode} {response.StatusCode} - {json}");
        }
        
        return JsonSerializer.Deserialize<AudioQuery>(json, _jsonOptions)!;
    }

    public async Task<byte[]> SynthesisAsync(AudioQuery query, int speakerId)
    {
        var json = JsonSerializer.Serialize(query, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var url = $"{_baseUrl}/synthesis?speaker={speakerId}";
        var response = await _httpClient.PostAsync(url, content);
        var responseJson = await response.Content.ReadAsStringAsync();
        
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"synthesis 失敗: {(int)response.StatusCode} {response.StatusCode} - {responseJson}");
        }
        
        return await response.Content.ReadAsByteArrayAsync();
    }

    // 便利メソッド: テキストから直接音声データを取得
    public async Task<byte[]> TextToSpeechAsync(string text, int speakerId)
    {
        var query = await CreateAudioQueryAsync(text, speakerId);
        return await SynthesisAsync(query, speakerId);
    }
}

// VOICEVOX API のレスポンスモデル
public class SpeakerInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";
    
    [JsonPropertyName("speakerUuid")]
    public string SpeakerUuid { get; set; } = "";
    
    [JsonPropertyName("styles")]
    public SpeakerStyle[] Styles { get; set; } = Array.Empty<SpeakerStyle>();
    
    [JsonPropertyName("version")]
    public string Version { get; set; } = "";
}

public class SpeakerStyle
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";
    
    [JsonPropertyName("id")]
    public int Id { get; set; }
}

public class AudioQuery
{
    [JsonPropertyName("speedScale")]
    public double SpeedScale { get; set; } = 1.0;
    
    [JsonPropertyName("pitchScale")]
    public double PitchScale { get; set; } = 0.0;
    
    [JsonPropertyName("intonationScale")]
    public double IntonationScale { get; set; } = 1.0;
    
    [JsonPropertyName("volumeScale")]
    public double VolumeScale { get; set; } = 1.0;
    
    [JsonPropertyName("prePhonemeLength")]
    public double PrePhonemeLength { get; set; } = 0.0;
    
    [JsonPropertyName("postPhonemeLength")]
    public double PostPhonemeLength { get; set; } = 0.0;
    
    [JsonPropertyName("outputSamplingRate")]
    public double OutputSamplingRate { get; set; } = 24000;
    
    [JsonPropertyName("outputStereo")]
    public bool OutputStereo { get; set; } = false;
    
    [JsonPropertyName("kana")]
    public string Kana { get; set; } = "";
    
    [JsonPropertyName("accent_phrases")]
    public AccentPhrase[] AccentPhrases { get; set; } = Array.Empty<AccentPhrase>();
}

public class AccentPhrase
{
    [JsonPropertyName("moras")]
    public Mora[] Moras { get; set; } = Array.Empty<Mora>();
    
    [JsonPropertyName("accent")]
    public int Accent { get; set; }
    
    [JsonPropertyName("isInterrogative")]
    public bool IsInterrogative { get; set; }
}

public class Mora
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = "";
    
    [JsonPropertyName("vowel")]
    public string Vowel { get; set; } = "";
    
    [JsonPropertyName("consonant")]
    public string Consonant { get; set; } = "";
    
    [JsonPropertyName("pitch")]
    public double Pitch { get; set; }
    
    [JsonPropertyName("vowel_length")]
    public double VowelLength { get; set; }
    
    [JsonPropertyName("consonant_length")]
    public double? ConsonantLength { get; set; }
}