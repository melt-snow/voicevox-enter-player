using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using NAudio.Wave;

namespace VoicevoxEnterPlayer;

public partial class MainWindow : Window
{
    private HttpClient _httpClient = null!;
    private VoicevoxClient _voicevoxClient = null!;
    private readonly Queue<string> _speechQueue = new();
    private bool _isProcessing = false;
    private int _selectedSpeakerId = 3;
    private double _prePhonemeLength = 0.10;
    private string _engineUrl = "http://localhost:50021";

    public MainWindow()
    {
        InitializeComponent();

        var settings = AppSettings.Load();
        _selectedSpeakerId = settings.SpeakerId;
        _prePhonemeLength = settings.PrePhonemeLength;
        _engineUrl = SettingsWindow.NormalizeUrl(settings.EngineUrl);

        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        _voicevoxClient = new VoicevoxClient(_httpClient, _engineUrl);

        Loaded += MainWindow_Loaded;
        Closed += MainWindow_Closed;

        Title = BuildTitle();
        InputTextBox.Focus();
    }

    private static string BuildTitle()
    {
        var v = typeof(MainWindow).Assembly.GetName().Version;
        if (v == null || (v.Major == 0 && v.Minor == 0 && v.Build == 0))
            return "VOICEVOX Enter Player";
        return $"VOICEVOX Enter Player (v{v.Major}.{v.Minor}.{v.Build})";
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await CheckVoicevoxConnection();
    }

    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        _httpClient?.Dispose();
        _waveOut.Dispose();
    }

    private async Task CheckVoicevoxConnection()
    {
        try
        {
            var speakers = await _voicevoxClient.GetSpeakersAsync();
            if (speakers != null && speakers.Length > 0)
            {
                StatusText.Text = $"✅ エンジン接続OK - {speakers.Length}話者利用可能";
                StatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4EC9B0"));
            }
            else
            {
                StatusText.Text = "⚠ 接続できましたが話者が見つかりません";
                StatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFD700"));
            }
        }
        catch (Exception ex)
        {
            var detail = ex.InnerException?.Message ?? ex.Message;
            StatusText.Text = $"❌ エンジンに接続できません: {_engineUrl}（起動状態とURLを確認） [{detail}]";
            StatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F44747"));
        }
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        // ESCで最小化
        if (e.Key == Key.Escape)
        {
            WindowState = WindowState.Minimized;
        }
    }

    private void InputTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        // Enterキーで発声（Shift+Enterは改行）
        if (e.Key == Key.Enter && !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
        {
            e.Handled = true;
            EnqueueText();
        }
    }

    private void InputTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        // 文字数表示などの更新が必要ならここに
    }

    private void BtnSpeak_Click(object sender, RoutedEventArgs e)
    {
        EnqueueText();
    }

    private void EnqueueText()
    {
        var text = InputTextBox.Text?.Trim();
        if (string.IsNullOrEmpty(text))
        {
            StatusText.Text = "⚠ テキストを入力してください";
            StatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFD700"));
            return;
        }

        _speechQueue.Enqueue(text);
        InputTextBox.Clear();
        InputTextBox.Focus();

        UpdateQueueStatus();
        _ = ProcessQueueAsync();
    }

    private async Task ProcessQueueAsync()
    {
        if (_isProcessing) return;

        _isProcessing = true;
        BtnSpeak.IsEnabled = false;
        BtnSpeak.Content = "🔊 発声中...";

        try
        {
            while (_speechQueue.Count > 0)
            {
                var text = _speechQueue.Dequeue();
                try
                {
                    StatusText.Text = _speechQueue.Count > 0
                        ? $"🔄 音声合成中... (残り {_speechQueue.Count} 件)"
                        : "🔄 音声合成中...";
                    StatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFD700"));

                    var query = await _voicevoxClient.CreateAudioQueryAsync(text, _selectedSpeakerId);
                    query.PrePhonemeLength = _prePhonemeLength;
                    var audioData = await _voicevoxClient.SynthesisAsync(query, _selectedSpeakerId);

                    StatusText.Text = _speechQueue.Count > 0
                        ? $"🔊 再生中... (残り {_speechQueue.Count} 件)"
                        : "🔊 再生中...";
                    await PlayAudioAsync(audioData);
                }
                catch (Exception ex)
                {
                    StatusText.Text = $"❌ エラー: {ex.Message}";
                    StatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F44747"));
                }
            }
        }
        finally
        {
            _isProcessing = false;
            BtnSpeak.IsEnabled = true;
            BtnSpeak.Content = "▶ 発声 (Enter)";
            InputTextBox.Focus();

            if (StatusText.Text.StartsWith("🔊"))
            {
                StatusText.Text = "✅ 発声完了";
                StatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4EC9B0"));
            }
        }
    }

    private void UpdateQueueStatus()
    {
        var pending = _speechQueue.Count + (_isProcessing ? 1 : 0);
        if (pending > 0)
        {
            StatusText.Text = $"⏳ キュー追加 ({pending} 件待ち)";
            StatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFD700"));
        }
    }

    private readonly WaveOutEvent _waveOut = new();

    private async Task PlayAudioAsync(byte[] audioData)
    {
        using var ms = new MemoryStream(audioData);
        using var reader = new WaveFileReader(ms);

        var tcs = new TaskCompletionSource<bool>();
        void OnStopped(object? s, StoppedEventArgs e) => tcs.TrySetResult(true);
        _waveOut.PlaybackStopped += OnStopped;

        try
        {
            _waveOut.Init(reader);
            _waveOut.Play();
            await tcs.Task;
        }
        finally
        {
            _waveOut.PlaybackStopped -= OnStopped;
        }
    }

    private SettingsWindow? _settingsWindow;

    private async void BtnSettings_Click(object sender, RoutedEventArgs e)
    {
        if (_settingsWindow != null && _settingsWindow.IsLoaded)
        {
            _settingsWindow.Activate();
            _settingsWindow.Focus();
            return;
        }

        _settingsWindow = new SettingsWindow(_engineUrl, _selectedSpeakerId, _prePhonemeLength);
        _settingsWindow.Owner = this;
        
        var result = _settingsWindow.ShowDialog();
        var selectedId = _settingsWindow.SelectedSpeakerId;
        var prePhoneme = _settingsWindow.PrePhonemeLength;
        var newUrl = _settingsWindow.EngineUrl;
        _settingsWindow = null;
        
        if (result == true)
        {
            bool urlChanged = !string.Equals(_engineUrl, newUrl, StringComparison.OrdinalIgnoreCase);
            if (urlChanged)
            {
                _engineUrl = newUrl;
                _voicevoxClient = new VoicevoxClient(_httpClient, _engineUrl);
            }

            _selectedSpeakerId = selectedId;
            _prePhonemeLength = prePhoneme;

            new AppSettings
            {
                EngineUrl = _engineUrl,
                SpeakerId = _selectedSpeakerId,
                PrePhonemeLength = _prePhonemeLength
            }.Save();

            if (urlChanged)
            {
                await CheckVoicevoxConnection();
            }
            else
            {
                StatusText.Text = $"🎭 設定更新: 話者 ID {_selectedSpeakerId} / 無音 {_prePhonemeLength:F2}秒 (保存済み)";
                StatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4EC9B0"));
            }
        }
    }
}