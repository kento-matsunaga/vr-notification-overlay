# VRNotify リリース手順書

## 前提条件

- .NET 8 SDK がインストール済み
- NSIS がインストール済み (`winget install NSIS.NSIS`)
- 全テストが PASS していること

## リリースビルド手順

### 1. バージョン番号の更新

以下のファイルのバージョンを一括更新する:

| ファイル | 更新箇所 |
|---------|---------|
| `installer/installer.nsi` | `!define VERSION "X.Y.Z"` と `VIProductVersion "X.Y.Z.0"` |
| `packaging/AppxManifest.xml` | `<Identity Version="X.Y.Z.0" ...>` |
| `build.ps1` | ZIP ファイル名 `VRNotify-X.Y.Z-Portable.zip` |
| `installer/installer.nsi` | OutFile `VRNotify-X.Y.Z-Installer.exe` |

> **重要**: AppxManifest.xml の Version は前のバージョンより大きい必要がある。
> MSIX の仕様でダウングレードは許可されない。

### 2. テスト実行

```powershell
dotnet test VRNotify.sln -c Release
```

全テストが PASS することを確認。

### 3. ビルド実行

```powershell
.\build.ps1
```

これにより以下が自動実行される:
1. `dotnet publish` (self-contained, single-file, win-x64)
2. パッケージングアセットのコピー + PDB 除去
3. NSIS インストーラー作成 → `dist/VRNotify-X.Y.Z-Installer.exe`
4. ポータブル ZIP 作成 → `dist/VRNotify-X.Y.Z-Portable.zip`

### 4. 成果物の確認

```
dist/
├── VRNotify-X.Y.Z-Installer.exe   (~75 MB, NSIS インストーラー)
└── VRNotify-X.Y.Z-Portable.zip    (~75 MB, ポータブル版)
```

## 成果物の内容

### インストーラー版 (`VRNotify-X.Y.Z-Installer.exe`)

ダブルクリックで起動。以下を自動実行:
1. ファイルを `%ProgramFiles%\VRNotify` にコピー
2. 自己署名証明書の作成 + TrustedPeople ストアに登録
3. Sparse MSIX パッケージ登録 (Package Identity 付与)
4. レジストリ登録 (「設定 → アプリ」にアンインストール項目追加)
5. スタートメニューにショートカット作成
6. アンインストーラー (`Uninstall.exe`) 生成

### ポータブル版 (`VRNotify-X.Y.Z-Portable.zip`)

ZIP 展開後、管理者 PowerShell で初回セットアップが必要:
```powershell
.\setup-portable.ps1
```

ZIP の中身:
```
VRNotify.Desktop.exe       # メインアプリ
AppxManifest.xml            # Sparse MSIX 登録用
placeholder.png             # パッケージアイコン
manifest.vrmanifest          # SteamVR 自動起動用
create-cert.ps1              # 証明書作成スクリプト
setup-portable.ps1           # 初回セットアップ (証明書 + MSIX 登録)
uninstall-portable.ps1       # アンインストール
```

## アップデート時の注意事項

### バージョンアップ手順 (ユーザー側)

**インストーラー版**:
1. 既存のアンインストールは不要
2. 新バージョンのインストーラーを実行するだけで上書きインストール
3. MSIX パッケージは `Add-AppxPackage -Register` で上書き登録される
4. 設定ファイル (`%APPDATA%/VRNotify/`) は保持される

**ポータブル版**:
1. 古いフォルダの exe を新しいもので上書き
2. `setup-portable.ps1` を再実行 (MSIX 再登録)
3. 設定ファイルは別ディレクトリなので影響なし

### 開発者がアップデートで気をつけること

1. **AppxManifest.xml の Version を必ずインクリメント**
   - `1.0.0.0` → `1.1.0.0` のように上げる
   - 同じバージョンだと `Add-AppxPackage` が失敗する場合がある

2. **設定ファイルの後方互換性**
   - `settings.json` のスキーマを変更した場合、JsonSettingsRepository が旧フォーマットでも読み込めるようにする
   - 新フィールド追加時は JSON デシリアライズのデフォルト値で対応

3. **SQLite 履歴 DB のマイグレーション**
   - テーブルにカラム追加が必要な場合は `ALTER TABLE` マイグレーションを実装
   - 現在は `CREATE TABLE IF NOT EXISTS` のみ

4. **Certificate の CN は変更しない**
   - `CN=VRNotify` で統一。変更すると既存ユーザーの再セットアップが必要になる

5. **NSIS スクリプトの `File /r` に注意**
   - `publish/` 内の全ファイルをパッケージするため、不要ファイルが混入しないよう注意

## Booth 配布時のチェックリスト

- [ ] バージョン番号が全ファイルで統一されている
- [ ] `dotnet test` 全 PASS
- [ ] `build.ps1` がエラーなく完了
- [ ] クリーン環境でインストーラー実行 → トレイ常駐を確認
- [ ] SteamVR 起動 → VR 内に通知表示を確認
- [ ] 設定ウィンドウが開ける
- [ ] アンインストール → 完全削除を確認
- [ ] README.md のバージョン番号が更新されている

## トラブルシューティング

### NSIS ビルドでエラー

```
NSIS not found. Skipping installer creation.
```
→ `winget install NSIS.NSIS` でインストール

### dotnet publish でエラー

```
error NETSDK1136: The target platform must be set to Windows
```
→ `VRNotify.Desktop.csproj` の `TargetFramework` が `net8.0-windows10.0.19041.0` であることを確認

### インストーラー実行時に SmartScreen 警告

自己署名のため Windows SmartScreen が警告を表示する。
「詳細情報」→「実行」で続行可能。
コードサイニング証明書を購入すれば警告を回避できる（将来対応）。
