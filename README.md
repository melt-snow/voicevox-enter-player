# voicevox-enter-player

[VOICEVOX](https://voicevox.hiroshiba.jp/) にテキストを入力して、Enter キーで即座に読み上げる Windows 11 向けの小型アプリです。

## ダウンロード

[Releases](../../releases) ページから `VoicevoxEnterPlayer-vX.Y.Z-win-x64.exe` をダウンロードしてそのまま実行できます（.NET のインストール不要）。

## 必要なもの

- Windows 11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)（ビルドする場合）
- 下記のいずれかの音声合成エンジン（ローカル起動）

| エンジン | URL（例） | 備考 |
|---|---|---|
| [VOICEVOX](https://voicevox.hiroshiba.jp/) | `http://localhost:50021` | 既定 |
| [VOICEVOX Nemo](https://voicevox.hiroshiba.jp/nemo/) | `http://localhost:50160` | VOICEVOX のマルチエンジン機能で導入。ポートは環境により異なる場合あり |
| [AivisSpeech](https://aivis-project.com/) | `http://localhost:10101` | VOICEVOX 互換 API |

## 使い方

1. エンジンを起動する
2. 本アプリを起動する
3. テキストを入力して **Enter** で読み上げ
4. **⚙ 設定** で以下を変更できる（設定は自動保存され、次回起動時に復元されます）
   - **エンジンURL** — 入力して Enter すると話者一覧を再取得
   - **話者** — 接続中エンジンの話者スタイルから選択
   - **発声前の無音** — 頭切れする場合は増やす（0〜1秒）

### 発声キューについて

読み上げ中でも次のテキストを入力できます。Enter を押すと入力欄が即クリアされてキューに追加され、順番に発声されます。

### 既知の制限

- 入力は **最大 1000 文字**（エンジン API の URL 長制限への対策）
- **二重起動不可** — 既に起動している場合は既存ウィンドウを前面表示します
- マルチエンジンのポート番号は環境により異なる場合があります（接続できない場合は `netstat -ano | findstr <PID>` 等で確認してください）

## ビルド

```bash
dotnet build VoicevoxEnterPlayer.csproj
dotnet run --project VoicevoxEnterPlayer.csproj
```

## 配布用ビルド（単一 exe）

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

出力: `bin/Release/net8.0-windows/win-x64/publish/VoicevoxEnterPlayer.exe`

タグ（`v1.0.0` など）を push すると GitHub Actions が自動ビルドし、Releases へ登録します。

## ライセンス

MIT License。詳細は [LICENSE](LICENSE) を参照してください。

※ 各音声ライブラリの利用にあたっては、それぞれのキャラクター・ソフトウェアの利用規約（VOICEVOX / VOICEVOX Nemo / AivisSpeech）に従ってください。
