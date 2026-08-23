using System;
using System.IO;
using System.Text.Json;

namespace VoicevoxEnterPlayer;

public class AppSettings
{
    public string EngineUrl { get; set; } = "http://localhost:50021";
    public int SpeakerId { get; set; } = 3;
    public double PrePhonemeLength { get; set; } = 0.10;

    private static string GetSettingsPath()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "VoicevoxEnterPlayer");
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, "settings.json");
    }

    public static AppSettings Load()
    {
        try
        {
            var path = GetSettingsPath();
            if (!File.Exists(path))
                return new AppSettings();

            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"settings load failed: {ex.Message}");
            return new AppSettings();
        }
    }

    public void Save()
    {
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(GetSettingsPath(), JsonSerializer.Serialize(this, options));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"settings save failed: {ex.Message}");
        }
    }
}