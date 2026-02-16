# VRNotify - インタラクション・セットアップ・日常フロー
# 更新: 2026-02-17 Windows通知リスナー方式 + Booth MVP

# ============================================================
# Feature 2: アプリのセットアップ
# ============================================================

Feature: アプリの初期セットアップ
  新規ユーザーとして
  できるだけ簡単にWindows通知をVR内で受け取れるようにしたい
  そのためにインストーラーでワンクリックセットアップが完了する

  @implemented @booth-mvp
  Scenario: インストーラー版でセットアップする
    Given VRNotifyのインストーラー(.exe)をダウンロードしている
    When インストーラーを実行する
    Then 管理者権限の確認ダイアログが表示される
    And 「はい」を選択するとインストールが開始される
    And 自己署名証明書がTrustedPeopleストアに登録される
    And Sparse MSIXパッケージが登録される（Package Identity付与）
    And スタートメニューにVRNotifyのショートカットが追加される
    And 「設定→アプリ」にアンインストール項目が追加される

  @implemented @booth-mvp
  Scenario: ポータブル版でセットアップする
    Given VRNotifyのZIPファイルを任意のフォルダに展開している
    When 管理者PowerShellで setup-portable.ps1 を実行する
    Then 自己署名証明書が作成・登録される
    And Sparse MSIXパッケージが登録される
    And スタートメニューからVRNotifyを起動できるようになる

  @implemented @booth-mvp
  Scenario: Package Identity がない状態で起動した場合
    Given VRNotify.Desktop.exe を直接ダブルクリックで起動している
    And Sparse MSIXパッケージが登録されていない
    When アプリが起動する
    Then Package Identity エラーダイアログが表示される
    And インストーラーの使用または setup-portable.ps1 の実行を案内する

# ============================================================
# Feature 3: システムトレイ操作
# ============================================================

Feature: システムトレイでの操作
  VRNotifyユーザーとして
  バックグラウンドで動作するアプリをトレイアイコンから管理したい

  Background:
    Given VRNotifyが起動しシステムトレイにアイコンが表示されている

  @implemented @booth-mvp
  Scenario: トレイアイコンをダブルクリックして設定画面を開く
    When システムトレイのVRNotifyアイコンをダブルクリックする
    Then WPF設定ウィンドウが表示される
    And 設定ウィンドウは5つのタブ（一般/フィルタ/表示/履歴/About）を持つ

  @implemented @booth-mvp
  Scenario: トレイメニューからDNDモードを切り替える
    When システムトレイのVRNotifyアイコンを右クリックする
    Then コンテキストメニューが表示される
    And 「DND」サブメニューから「Off / SuppressAll / HighPriorityOnly」を選択できる

  @implemented @booth-mvp
  Scenario: トレイメニューからアプリを終了する
    When トレイメニューの「Exit」を選択する
    Then VRNotifyがグレースフルに終了する
    And OpenVRオーバーレイが解放される
    And 通知リスナーが停止する

# ============================================================
# Feature 4: 日常的な使用フロー
# ============================================================

Feature: 日常的なVRChatセッションでの使用フロー
  VRChatを日常的に利用するユーザーとして
  通知システムの存在をあまり意識せず自然に使いたい

  Background:
    Given VRNotifyがインストール済みで起動している
    And Windows通知リスナーが有効である

  @implemented @booth-mvp
  Scenario: SteamVR起動時にVRNotifyが自動的にオーバーレイを有効にする
    Given VRNotifyがシステムトレイで動作している
    When SteamVRを起動する
    Then VRNotifyが5秒以内にSteamVRを検出する
    And OpenVRオーバーレイが初期化される
    And 以降のWindows通知がVR内に表示されるようになる

  @implemented @booth-mvp
  Scenario: VRChat中にDiscord通知を受信する
    Given SteamVRが起動しVRChatでプレイ中である
    When DiscordアプリのWindows通知が発生する
    Then VR視界にDiscordの通知カードが表示される
    And アプリ名「Discord」、通知タイトル、本文が表示される
    And 設定された表示時間後に自動で消える

  @implemented @booth-mvp
  Scenario: SteamVRが起動していない場合は通知を蓄積する
    Given SteamVRが起動していない
    When Windows通知が発生する
    Then 通知は通知履歴（SQLite）に保存される
    And VRオーバーレイ表示はスキップされる

  @implemented @booth-mvp
  Scenario: SteamVR終了後もアプリは動作を継続する
    Given SteamVRが起動しオーバーレイが表示されている
    When SteamVRを終了する
    Then VRNotifyはシステムトレイに残り動作を継続する
    And OpenVRリソースが安全に解放される
    And SteamVRの再起動を5秒間隔で監視する

  @implemented @booth-mvp
  Scenario: 設定ウィンドウを閉じても非表示になるだけでアプリは終了しない
    Given 設定ウィンドウが開いている
    When 設定ウィンドウの「×」ボタンをクリックする
    Then ウィンドウは非表示になる（閉じない）
    And 設定変更は自動的にJSONファイルに保存される
    And アプリはシステムトレイで動作を継続する
