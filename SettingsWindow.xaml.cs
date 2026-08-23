using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace VoicevoxEnterPlayer;

public partial class SettingsWindow : Window
{
    private HttpClient? _httpClient;
    private VoicevoxClient _voicevoxClient;
    private List<SpeakerItem> _allSpeakers = new();
    private bool _isInitializing = true;

    public int SelectedSpeakerId { get; private set; }
    public double PrePhonemeLength { get; private set; }
    public string EngineUrl { get; private set; }

    public SettingsWindow(string currentEngineUrl, int currentSpeakerId, double currentPrePhonemeLength)
    {
        InitializeComponent();
        EngineUrl = NormalizeUrl(currentEngineUrl);
        SelectedSpeakerId = currentSpeakerId;
        PrePhonemeLength = currentPrePhonemeLength;

        _voicevoxClient = CreateClient(EngineUrl);

        Loaded += SettingsWindow_Loaded;
        SpeakerComboBox.SelectionChanged += SpeakerComboBox_SelectionChanged;
    }

    private VoicevoxClient CreateClient(string url)
    {
        _httpClient?.Dispose();
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        return new VoicevoxClient(_httpClient, url);
    }

    public static string NormalizeUrl(string url)
    {
        var trimmed = (url ?? "").Trim().TrimEnd('/');
        if (!trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = "http://" + trimmed;
        }
        return trimmed;
    }

    private async void SettingsWindow_Loaded(object sender, RoutedEventArgs e)
    {
        EngineUrlBox.Text = EngineUrl;
        PrePhonemeSlider.Value = PrePhonemeLength;
        UpdatePrePhonemeLabel();
        _isInitializing = false;

        await ReloadSpeakersSafeAsync();
    }

    private async Task ReloadSpeakersSafeAsync()
    {
        try
        {
            await LoadSpeakersAsync();
        }
        catch (Exception ex)
        {
            StatusText.Text = $"接続エラー: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"SettingsWindow LoadSpeakersAsync error: {ex}");
        }
    }

    private async void EngineUrlBox_LostFocus(object sender, RoutedEventArgs e)
        => await ApplyUrlChangeIfNeededAsync();

    private async void EngineUrlBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            await ApplyUrlChangeIfNeededAsync();
        }
    }

    private async Task ApplyUrlChangeIfNeededAsync()
    {
        var newUrl = NormalizeUrl(EngineUrlBox.Text);
        if (string.Equals(newUrl, EngineUrl, StringComparison.OrdinalIgnoreCase))
            return;

        EngineUrl = newUrl;
        EngineUrlBox.Text = newUrl;
        _voicevoxClient = CreateClient(newUrl);

        SpeakerComboBox.ItemsSource = null;
        _allSpeakers.Clear();
        StatusText.Text = $"{newUrl} に接続中...";
        await ReloadSpeakersSafeAsync();
    }

    private void PrePhonemeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isInitializing) return;
        PrePhonemeLength = Math.Round(e.NewValue, 2);
        UpdatePrePhonemeLabel();
    }

    private void UpdatePrePhonemeLabel()
    {
        if (PrePhonemeValueText != null)
            PrePhonemeValueText.Text = $"{PrePhonemeLength:F2} 秒";
    }

    private async Task LoadSpeakersAsync()
    {
        try
        {
            StatusText.Text = "話者一覧を取得中...";
            var speakers = await _voicevoxClient.GetSpeakersAsync();
            
            if (speakers != null)
            {
                _allSpeakers.Clear();
                foreach (var speaker in speakers)
                {
                    foreach (var style in speaker.Styles)
                    {
                        _allSpeakers.Add(new SpeakerItem
                        {
                            SpeakerName = speaker.Name,
                            StyleName = style.Name,
                            SpeakerId = style.Id,
                            DisplayName = $"{speaker.Name} - {style.Name} (ID: {style.Id})"
                        });
                    }
                }

                SpeakerComboBox.ItemsSource = _allSpeakers.Select(s => s.DisplayName).ToList();
                
                // 現在選択されている話者を選択状態に
                var currentSpeaker = _allSpeakers.FirstOrDefault(s => s.SpeakerId == SelectedSpeakerId);
                if (currentSpeaker != null)
                {
                    SpeakerComboBox.SelectedIndex = _allSpeakers.IndexOf(currentSpeaker);
                }
                else if (_allSpeakers.Count > 0)
                {
                    SpeakerComboBox.SelectedIndex = 0;
                    SelectedSpeakerId = _allSpeakers[0].SpeakerId;
                }

                StatusText.Text = $"{_allSpeakers.Count} 種類の話者スタイルが利用可能";
            }
        }
        catch (Exception ex)
        {
            StatusText.Text = $"エラー: {ex.Message}";
        }
    }

    private void SpeakerComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_allSpeakers == null || _allSpeakers.Count == 0)
            return;
            
        if (SpeakerComboBox.SelectedIndex >= 0 && SpeakerComboBox.SelectedIndex < _allSpeakers.Count)
        {
            SelectedSpeakerId = _allSpeakers[SpeakerComboBox.SelectedIndex].SpeakerId;
        }
    }

    private void BtnOk_Click(object sender, RoutedEventArgs e)
    {
        EngineUrl = NormalizeUrl(EngineUrlBox.Text);
        if (_allSpeakers != null && _allSpeakers.Count > 0 && SelectedSpeakerId == 0)
        {
            SelectedSpeakerId = _allSpeakers[0].SpeakerId;
        }
        DialogResult = true;
        Close();
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            DialogResult = false;
            Close();
        }
        else if (e.Key == Key.Enter)
        {
            DialogResult = true;
            Close();
        }
    }
}

public class SpeakerItem
{
    public string SpeakerName { get; set; } = "";
    public string StyleName { get; set; } = "";
    public int SpeakerId { get; set; }
    public string DisplayName { get; set; } = "";
}