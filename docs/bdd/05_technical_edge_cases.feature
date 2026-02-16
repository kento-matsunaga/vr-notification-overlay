# VR通知オーバーレイシステム - 技術的エッジケース・非機能要件BDD

# ============================================================
# Feature 1: 接続の安定性と障害復旧
# ============================================================

Feature: 接続の安定性と障害復旧
  VR通知オーバーレイシステムは、外部サービスやVR基盤との接続が
  不安定な状況でも、自律的に復旧し、通知の欠損を最小限に抑える。

  Background:
    Given アプリケーションが正常に起動している
    And SteamVRが起動しオーバーレイが登録済みである
    And Discord Gatewayに接続済みである
    And Slack Socket Modeに接続済みである

  # --- Discord Gateway ---

  Scenario: Discord Gatewayが一時的に切断された場合、自動再接続する
    Given Discord Gatewayとの接続が確立されている
    When Discord Gatewayとの接続がOpCode 7 (Reconnect) で切断される
    Then 1秒以内にresume接続を試行する
    And resume接続に成功した場合、切断中に送信されたイベントをsequence番号ベースで再取得する
    And ユーザーには「Discord再接続中…」のオーバーレイ通知を表示する

  Scenario: Discord Gatewayのresume接続が失敗した場合、フルリコネクトする
    Given Discord Gatewayとの接続がOpCode 9 (Invalid Session) で切断された
    When resume接続が拒否される
    Then 1〜5秒のランダムな遅延後にフルリコネクト（Identify）を試行する
    And resume不可だったため一部の通知が欠損した可能性がある旨をログに記録する

  Scenario: Discord Gatewayへの再接続が連続して失敗する場合、Exponential Backoffで再試行する
    Given Discord Gatewayとの接続が切断された
    When 再接続が3回連続で失敗する
    Then 再接続間隔をExponential Backoff（1秒, 2秒, 4秒, 8秒, …最大60秒）で増加させる
    And 各再試行の間隔にはジッタ（±20%のランダム変動）を加える
    And 再接続試行が10回を超えた場合、5分間隔の定期再試行モードに移行する

  Scenario: Discord APIのレート制限を受けた場合、適切に待機する
    Given Discord REST APIを使用して通知の補足情報を取得している
    When APIレスポンスがHTTP 429 (Too Many Requests) を返す
    Then レスポンスヘッダのRetry-Afterで指定された秒数だけ待機する
    And 待機中は他のAPIリクエストをキューに蓄積する

  # --- Slack Socket Mode ---

  Scenario: Slack Socket Modeが切断された場合、自動再接続する
    Given Slack Socket Mode (WebSocket) が確立されている
    When Slack Socket Modeの接続が切断される
    Then apps.connections.open APIを呼び出して新しいWebSocket URLを取得する
    And 新しいWebSocket URLに接続する

  Scenario: Slack App-Level Tokenが無効化された場合
    Given Slack Socket Mode接続が確立されている
    When apps.connections.open APIがHTTP 401/403を返す
    Then ユーザーに「Slackトークンが無効です。再設定してください。」と通知する
    And Slack接続の自動再試行を停止する
    And Discord等の他のサービスの通知は継続する

  # --- SteamVR / OpenVR ---

  Scenario: SteamVRが予期せず終了した場合
    Given SteamVRが稼働しオーバーレイが表示されている
    When SteamVRプロセスが予期せず終了する
    Then オーバーレイリソースを安全に解放する
    And Discord/Slackの接続は維持したまま待機モードに移行する
    And SteamVRの再起動を5秒間隔で監視する
    And SteamVRが再起動されたら自動的にオーバーレイを再登録する

  Scenario: SteamVRが正常にシャットダウンされた場合
    Given SteamVRが稼働しオーバーレイが表示されている
    When SteamVRからVREvent_Quitイベントを受信する
    Then OpenVRShutdown()を呼び出してリソースを解放する
    And アプリケーションはシステムトレイに最小化して待機する
    And Discord/Slackの接続は維持する

  Scenario: VRChatがクラッシュした場合
    Given VRChatが起動しSteamVRセッション内で動作している
    When VRChatプロセスがクラッシュする
    Then オーバーレイの表示は継続する（SteamVRに属するため）
    And 通知の受信と表示は影響を受けない

  # --- ネットワーク ---

  Scenario: ネットワークが完全に断絶した場合
    Given Discord GatewayとSlack Socket Modeに接続している
    When ネットワーク接続が完全に失われる
    Then 両方のWebSocket接続が切断される
    And オーバーレイに「ネットワーク接続なし」のステータスを表示する
    And 30秒間隔でネットワーク接続の回復を確認する

  Scenario: ネットワークが復旧した場合
    Given ネットワーク断絶によりDiscordとSlackが切断されている
    When ネットワーク接続が復旧する
    Then Discord GatewayとSlack Socket Modeの再接続を並行して試行する
    And 「ネットワーク復旧」のオーバーレイ通知を表示する

  Scenario: ネットワークが不安定で断続的に切断される場合
    Given ネットワーク接続が不安定である
    When 5分間に3回以上の切断・復旧が発生する
    Then 不安定ネットワークモードに移行する
    And 再接続間隔を通常より長め（最小10秒）に設定する

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
    And フレームタイムのスパイクが4ms以内である

  Scenario: 複数の通知が同時に表示されている場合のFPS影響
    Given VRChatが動作中である
    When 同時に5件の通知オーバーレイが表示されている
    Then VRChatのフレームレート低下が合計5fps以内である

  Scenario: 長時間稼働時のメモリ使用量が安定している
    Given アプリケーションが起動直後で初期メモリ使用量が記録されている
    When アプリケーションが8時間連続稼働する
    And その間に合計500件の通知を受信・表示・破棄する
    Then メモリ使用量が初期値から100MB以上増加しない
    And メモリリークのパターン（単調増加）が検出されない

  Scenario: 通知テクスチャのメモリが適切に解放される
    Given 通知がオーバーレイに表示されている
    When 通知の表示期間が終了しオーバーレイが非表示になる
    Then 通知用に確保されたテクスチャメモリが5秒以内に解放される
    And テクスチャプールのサイズが上限（20テクスチャ）を超えない

  Scenario: アイドル時のCPU使用率が低い
    Given アプリケーションが起動しており通知がない状態である
    When 1分間通知を受信しない
    Then アプリケーションのCPU使用率が平均1%以下である

  Scenario: ログファイルの肥大化を防止する
    Given アプリケーションが長時間稼働している
    When ログファイルのサイズが50MBを超える
    Then ログファイルをローテーションする（最大5世代保持）

# ============================================================
# Feature 3: 通知のバースト対応
# ============================================================

Feature: 通知のバースト対応
  大量の通知が短時間に発生した場合でも、
  システムの安定性とユーザー体験を維持する。

  Scenario: 短時間に大量の通知を受信した場合、レート制限を適用する
    Given 通知の表示レートが「最大3件/5秒」に設定されている
    When 1秒間に20件の通知を受信する
    Then 最初の3件を即座に表示する
    And 残りの17件をキューに格納する
    And 「他17件の通知があります」のようなサマリーをオーバーレイに表示する

  Scenario: 通知キューが上限に達した場合
    Given 通知キューの上限が100件に設定されている
    When キューに100件の通知が蓄積された状態で新しい通知を受信する
    Then キュー内の最も古い通知を破棄して新しい通知を格納する
    And 破棄された通知の件数をログに記録する

  Scenario: 同一チャンネルからの連続通知をグルーピングする
    Given 通知のグルーピング機能が有効である
    When 同一Discordチャンネルから5秒以内に5件の通知を受信する
    Then 「#general: 5件の新着メッセージ」のようにグルーピングして1件で表示する

  Scenario: 非常に長いメッセージを受信した場合
    Given 通知テキストの最大表示文字数が300文字に設定されている
    When 5000文字のDiscordメッセージを受信する
    Then 先頭300文字を表示し末尾に「…（続きあり）」を付与する
    And メモリ使用量が通常の通知と大きく変わらない

  Scenario: 画像付きメッセージを受信した場合
    Given 通知に画像サムネイルを表示する設定が有効である
    When 画像が添付されたDiscordメッセージを受信する
    Then 画像を128x128px以下にリサイズしてサムネイル表示する
    And 画像のダウンロードが5秒以内に完了しない場合はプレースホルダーを表示する

# ============================================================
# Feature 4: セキュリティとプライバシー
# ============================================================

Feature: セキュリティとプライバシー
  ユーザーの認証情報と通知内容を保護し、
  悪意のあるコンテンツからシステムを防御する。

  Scenario: APIトークンがWindows DPAPIで暗号化して保存される
    Given ユーザーがDiscord Botトークンを設定画面で入力する
    When トークンが保存される
    Then トークンはWindows DPAPI (ProtectedData.Protect) で暗号化される
    And 平文のトークンはメモリ上から即座にクリアされる

  Scenario: APIトークンがログに出力されない
    Given アプリケーションがデバッグログを出力している
    When Discord/SlackのAPI通信でエラーが発生する
    Then ログにはトークンの先頭4文字 + "****" のマスク形式のみ記録される

  Scenario: プライバシーモードが有効な場合、通知内容を隠す
    Given プライバシーモードが「有効」に設定されている
    When Discordの通知を受信する
    Then オーバーレイには「Discordから新着メッセージ」のみ表示する
    And 送信者名、チャンネル名、メッセージ内容は表示しない

  Scenario: 極端に長いユーザー名を受信した場合
    Given 悪意のあるユーザーが非常に長いユーザー名（10000文字）を設定している
    When そのユーザーからの通知を受信する
    Then ユーザー名を50文字で切り詰めて表示する
    And テキストレンダリングがバッファオーバーフローを起こさない

  Scenario: 制御文字やゼロ幅文字を含むメッセージを受信した場合
    When 制御文字やゼロ幅文字を含む通知テキストをレンダリングする
    Then 制御文字は除去またはエスケープされる
    And レンダリング結果が視覚的に不正にならない

  Scenario: メッセージに含まれるHTML/スクリプトタグの無害化
    Given メッセージに "<script>alert('xss')</script>" が含まれている
    When 通知テキストをレンダリングする
    Then HTMLタグはエスケープされて平文として表示される

# ============================================================
# Feature 5: SteamVR/OpenVRとの連携
# ============================================================

Feature: SteamVR/OpenVRとの連携
  OpenVR IVROverlay APIとの連携において、
  様々なエッジケースに対応する。

  Scenario: SteamVRが起動していない状態でアプリケーションを起動する
    Given SteamVRがインストールされているが起動していない
    When アプリケーションを起動する
    Then システムトレイにアイコンを表示し「SteamVRの起動を待機中…」と通知する
    And Discord/Slackへの接続は開始する
    And 5秒間隔でSteamVRの起動を監視する

  Scenario: SteamVRが後から起動された場合にオーバーレイを初期化する
    Given アプリケーションがSteamVR待機モードで動作している
    When SteamVRが起動される
    Then OpenVRの初期化に成功する
    And キューに蓄積されていた通知を表示する

  Scenario: XSOverlayと同時に動作する
    Given XSOverlayが起動しオーバーレイを表示している
    When 本アプリケーションのオーバーレイを作成する
    Then オーバーレイキーをユニークな値で登録する
    And XSOverlayのオーバーレイと衝突しない

  Scenario: OpenVRオーバーレイが上限に達して作成に失敗した場合
    Given システム全体でOpenVRオーバーレイが128個登録されている
    When 本アプリが新しいオーバーレイを作成しようとする
    Then ユーザーに上限到達を通知する
    And 既存の1つのオーバーレイを使い回すフォールバックモードに移行する

  Scenario: HMDを脱いだ（近接センサーOFF）場合
    Given HMDを装着しVRChatが動作中である
    When HMDを脱ぐ
    Then オーバーレイのテクスチャ更新を一時停止する（省電力モード）
    And 通知の受信は継続する

  Scenario: HMDを再装着した場合
    Given HMDを脱いだ状態で省電力モードになっている
    When HMDを再装着する
    Then オーバーレイのテクスチャ更新を再開する
    And 省電力モード中に受信した未表示通知の件数をサマリーとして表示する

  Scenario: Quest (Air Link経由) でのオーバーレイ表示
    Given Meta QuestがAir Link経由でPCに接続しSteamVRが起動している
    When 通知をオーバーレイに表示する
    Then オーバーレイはPCVR経由で正常に表示される

# ============================================================
# Feature 6: 設定の永続化と復旧
# ============================================================

Feature: アップデートと設定の永続化
  設定ファイルの破損、APIの仕様変更において
  後方互換性と復旧能力を保証する。

  Scenario: 設定ファイルが破損している場合、バックアップから復旧する
    Given 正常な設定ファイルと自動バックアップが存在する
    When アプリケーションを起動し設定ファイルのJSONパースに失敗する
    Then バックアップ設定ファイルからの復旧を試行する
    And 破損した設定ファイルを ".corrupt.bak" としてリネーム保存する

  Scenario: 設定ファイルの保存がアトミックに行われる
    Given ユーザーが設定を変更する
    When 設定ファイルの保存処理を実行する
    Then まず一時ファイル（.tmp）に書き込む
    And 一時ファイルを元のファイル名にアトミックにリネームする

  Scenario: 設定ファイルのバージョンマイグレーション
    Given 設定ファイルのスキーマバージョンが "1.0" である
    When アプリケーションの新バージョン（スキーマ "2.0"）で起動する
    Then マイグレーション処理を実行して "1.0" → "2.0" に変換する
    And 新しいフィールドにはデフォルト値を設定する
    And マイグレーション前のバックアップを作成する

  Scenario: Discord APIのバージョンアップに対する耐性
    When Discordが未知のイベントタイプを送信してきた
    Then 未知のイベントは無視してログに記録する
    And アプリケーションはクラッシュしない

  Scenario: 新しいサービスプラグインを追加する
    Given Discord/Slackプラグインが動作している
    When Chatworkプラグインをプラグインディレクトリに配置する
    Then アプリケーション再起動時にChatworkプラグインが自動検出される
    And 既存のDiscord/Slack設定は影響を受けない

  Scenario: アプリケーションの多重起動を防止する
    Given アプリケーションが既に起動している
    When ユーザーがアプリケーションを再度起動しようとする
    Then Named Mutexにより多重起動を検知する
    And 既存のインスタンスをフォアグラウンドに表示する
    And 新しいインスタンスは即座に終了する

  Scenario: マルチバイト文字（日本語・絵文字）の正しい表示
    When 「こんにちは🎉テストです」という通知を表示する
    Then 日本語テキストが文字化けなく表示される
    And テキストの切り詰めがマルチバイト文字の途中で発生しない

  Scenario: アプリケーション終了時のグレースフルシャットダウン
    Given アプリケーションが稼働中でDiscord/Slackに接続している
    When ユーザーがアプリケーションの終了を指示する
    Then 各サービスの接続を正常に切断する
    And OpenVRオーバーレイを解除しシャットダウンする
    And 未保存の設定変更を保存する
    And シャットダウン処理全体が5秒以内に完了する
