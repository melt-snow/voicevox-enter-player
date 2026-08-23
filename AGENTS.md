# AGENTS.md

VOICEVOX にテキストを入力し、Enter キーで即座に読み上げる Windows 11 向けの小型アプリ。

## 要件

- Windows 11 専用
- C# + WPF / .NET 8
- VOICEVOX Engine API（`http://127.0.0.1:50021`）を使用
  - `audio_query` → `synthesis` の2段階リクエスト
  - 生成された WAV を NAudio で再生
- Enter で送信・読み上げ（Shift+Enter 等の複雑な操作は不要）
- 読み上げ中でも次のテキストを入力できる
- UI は極めてシンプルに。常に入力欄へフォーカスを置く
- エンジンが起動していない場合は分かりやすいエラーを表示
- 話者 ID を設定画面から選択できる
- エンジン URL を設定画面から切り替えられる（既定: `http://localhost:50021` / Nemo 例: `http://localhost:50160` / AivisSpeech 例: `http://localhost:10101`）
- 発声前の無音時間（prePhonemeLength）を設定画面から調整できる
- 設定は %APPDATA%\VoicevoxEnterPlayer\settings.json に保存・起動時に復元される

## 構成

| ファイル | 役割 |
|---|---|
| `MainWindow.xaml(.cs)` | メインUI。テキスト入力、Enter発声（キュー処理）、ステータス表示 |
| `SettingsWindow.xaml(.cs)` | 話者選択コンボボックス + prePhonemeLength スライダー |
| `VoicevoxClient.cs` | VOICEVOX REST API クライアントとレスポンスモデル |
| `AppSettings.cs` | 設定の保存・読込（%APPDATA%\VoicevoxEnterPlayer\settings.json） |
| `Styles/ModernStyles.xaml` | ダークテーマの共通スタイル |

## 注意点

- VOICEVOX API の JSON プロパティ名は snake_case（例: `accent_phrases`, `vowel_length`）。モデルには必ず `[JsonPropertyName]` を付けること
- `consonant` / `consonant_length` は null を取り得る（例: 「ン」）
