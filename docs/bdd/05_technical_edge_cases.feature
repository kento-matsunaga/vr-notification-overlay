# VRNotify - 技術的エッジケース・非機能要件BDD
# 更新: 2026-02-17 Windows通知リスナー方式 + Booth MVP

# ============================================================
# Feature 1: SteamVR/OpenVRとの連携
# ============================================================

Feature: SteamVR/OpenVRとの連携
  OpenVR IVROverlay APIとの連携において、
  様々な状況に対応する。

  @implemented @booth-mvp
  Scenario: SteamVRが起動していない状態でアプリケーションを起動する
    Given SteamVRがインストールされているが起動していない
    When アプリケーションを起動する
    Then システムトレイにアイコンを表示して動作を開始する
    And Windows通知のキャプチャは開始する
    And 5秒間隔でSteamVRの起動を監視する

  @implemented @booth-mvp
  Scenario: SteamVRが後から起動された場合にオーバーレイを初期化する
    Given アプリケーションがSteamVR待機モードで動作している
    When SteamVRが起動される
    Then OpenVRの初期化に成功する
    And 以降の通知がVR内に表示される

  @implemented @booth-mvp
  Scenario: SteamVRが予期せず終了した場合
    Given SteamVRが稼働しオーバーレイが表示されている
    When SteamVRプロセスが予期せず終了する
    Then オーバーレイリソースを安全に解放する
    And Windows通知のキャプチャは維持したまま待機モードに移行する
    And SteamVRの再起動を5秒間隔で監視する

  @implemented @booth-mvp
  Scenario: SteamVRが正常にシャットダウンされた場合
    Given SteamVRが稼働しオーバーレイが表示されている
    When SteamVRからシャットダウンイベントを受信する
    Then OpenVRリソースを解放する
    And アプリケーションはシステムトレイに残り動作を継続する

  @implemented @booth-mvp
  Scenario: XSOverlayと同時に動作する
    Given XSOverlayが起動しオーバーレイを表示している
    When 本アプリケーションのオーバーレイを作成する
    Then オーバーレイキーをユニークな値で登録する
    And XSOverlayのオーバーレイと衝突しない

  Scenario: Quest (Air Link経由) でのオーバーレイ表示
    Given Meta QuestがAir Link経由でPCに接続しSteamVRが起動している
    When 通知をオーバーレイに表示する
    Then オーバーレイはPCVR経由で正常に表示される

# ============================================================
# Feature 2: パフォーマンスと資源管理
# ============================================================

Feature: パフォーマンスと資源管理
  VR通知オーバーレイシステムは、VRChatのフレームレートや
  システムリソースに対する影響を最小限に抑える。

  Background:
    Given アプリケーションが正常に起動している
    And SteamVRが起動しVRChatが動作中である

  Scenario: 通知表示時にVRChatのFPS低下が許容範囲内である
    Given VRChatが90fps（HMDのネイティブリフレッシュレート）で動作している
    When 1件の通知がオーバーレイに表示される
    Then VRChatのフレームレートの低下が3fps以内である

  Scenario: アイドル時のCPU使用率が低い
    Given アプリケーションが起動しており通知がない状態である
    When 1分間通知を受信しない
    Then アプリケーションのCPU使用率が平均1%以下である

  Scenario: 通知テクスチャのメモリが適切に解放される
    Given 通知がオーバーレイに表示されている
    When 通知の表示期間が終了しオーバーレイが非表示になる
    Then 通知用に確保されたテクスチャメモリが適切に解放される

  @implemented @booth-mvp
  Scenario: ログファイルの出力先
    Given アプリケーションが稼働している
    Then ログファイルは%APPDATA%/VRNotify/logs/に出力される
    And Serilogによる構造化ログで記録される

# ============================================================
# Feature 3: 通知のバースト対応
# ============================================================

Feature: 通知のバースト対応
  大量の通知が短時間に発生した場合でも、
  システムの安定性とユーザー体験を維持する。

  @implemented @booth-mvp
  Scenario: 短時間に大量の通知を受信した場合、キューに蓄積される
    Given 表示スロットが3つ設定されている
    When 1秒間に10件の通知を受信する
    Then 最初の3件がスロットに表示される
    And 残りの7件がキューに格納される
    And スロットが空くたびにキューから順次表示される

  Scenario: 非常に長いメッセージを受信した場合
    Given 通知テキストが非常に長い
    When 5000文字のメッセージを含む通知を受信する
    Then 先頭部分を表示し末尾は省略される
    And メモリ使用量が通常の通知と大きく変わらない

# ============================================================
# Feature 4: セキュリティとプライバシー
# ============================================================

Feature: セキュリティとプライバシー
  ユーザーの通知内容を保護し、
  悪意のあるコンテンツからシステムを防御する。

  @implemented @booth-mvp
  Scenario: 極端に長いテキストを受信した場合
    Given 悪意のあるアプリが非常に長い通知（10000文字）を送信する
    When その通知を受信する
    Then テキストは適切な長さで切り詰めて表示される
    And テキストレンダリングがバッファオーバーフローを起こさない

  Scenario: 制御文字やゼロ幅文字を含むメッセージを受信した場合
    When 制御文字やゼロ幅文字を含む通知テキストをレンダリングする
    Then 制御文字は除去またはエスケープされる
    And レンダリング結果が視覚的に不正にならない

  @implemented @booth-mvp
  Scenario: マルチバイト文字（日本語・絵文字）の正しい表示
    When 「こんにちは🎉テストです」という通知を表示する
    Then 日本語テキストが文字化けなく表示される

# ============================================================
# Feature 5: 設定の永続化と復旧
# ============================================================

Feature: 設定の永続化と復旧
  設定ファイルの破損やアップデートにおいて
  後方互換性と復旧能力を保証する。

  @implemented @booth-mvp
  Scenario: 設定ファイルが破損している場合、デフォルト設定で起動する
    Given 設定ファイルのJSONが壊れている
    When アプリケーションを起動する
    Then デフォルト設定で起動する
    And 破損した設定についてログに記録される

  @implemented @booth-mvp
  Scenario: 設定ファイルの保存がアトミックに行われる
    Given ユーザーが設定を変更する
    When 設定ファイルの保存処理を実行する
    Then まず一時ファイル（.tmp）に書き込む
    And 一時ファイルを元のファイル名にアトミックにリネームする
    And 保存中に電源断が起きてもデータが消失しない

  @implemented @booth-mvp
  Scenario: アプリケーションの多重起動を防止する
    Given アプリケーションが既に起動している
    When ユーザーがアプリケーションを再度起動しようとする
    Then 多重起動が防止される

  @implemented @booth-mvp
  Scenario: アプリケーション終了時のグレースフルシャットダウン
    Given アプリケーションが稼働中である
    When ユーザーがアプリケーションの終了を指示する
    Then OpenVRオーバーレイを解除しシャットダウンする
    And Windows通知リスナーを停止する
    And 未保存の設定変更を保存する

# ============================================================
# Feature 6: インストーラーとパッケージング
# ============================================================

Feature: インストーラーとパッケージング
  一般ユーザーがダウンロードして簡単にセットアップできる。

  @implemented @booth-mvp
  Scenario: NSISインストーラーでワンクリックインストール
    When VRNotify-1.0.0-Installer.exe を実行する
    Then ファイルが%ProgramFiles%\VRNotifyにインストールされる
    And 自己署名証明書がTrustedPeopleストアに登録される
    And Sparse MSIXパッケージが登録される
    And スタートメニューにショートカットが追加される
    And アンインストーラーが生成される

  @implemented @booth-mvp
  Scenario: アンインストールで完全に削除される
    Given VRNotifyがインストール済みである
    When 「設定→アプリ→VRNotify→アンインストール」を実行する
    Then Sparse MSIXパッケージが解除される
    And インストール先フォルダが削除される
    And スタートメニューショートカットが削除される
    And レジストリ登録が削除される
    And ユーザー設定(%APPDATA%/VRNotify/)は保持される
