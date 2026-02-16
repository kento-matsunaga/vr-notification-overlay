# VRNotify - ビジネスドメイン視点BDD
# 更新: 2026-02-17 Windows通知リスナー方式 + Booth MVP

# ============================================================
# Feature 1: 通知ソース管理
# ============================================================

Feature: 通知ソースの管理
  VRユーザーとして
  Windows通知を通知ソースとして自動的に受信したい
  VR中に重要な通知を見逃さないために

  Background:
    Given システムが起動している
    And Package Identity が登録済みである

  @implemented @booth-mvp
  Scenario: Windows通知リスナーが自動的に接続される
    When VRNotifyが起動する
    Then Windows通知リスナーが有効になる
    And あらゆるWindowsアプリの通知をキャプチャ可能になる

  @implemented @booth-mvp
  Scenario: Package Identity がない場合は通知リスナーが使えない
    Given Sparse MSIXパッケージが登録されていない
    When VRNotifyが起動する
    Then Package Identity エラーが表示される
    And Windows通知のキャプチャは利用できない

  @future @phase3
  Scenario: Discord Bot を通知ソースとして追加する
    Given Windows通知リスナーが有効である
    When ユーザーがDiscord Bot トークンを設定画面で入力する
    Then Discord通知ソースが追加される
    And DM/メンションの優先度判定が有効になる

# ============================================================
# Feature 2: 通知フィルタリング
# ============================================================

Feature: 通知フィルタリング（アプリ名ベース）
  VRユーザーとして
  受信する通知をアプリ名でフィルタリングしたい
  不要なアプリの通知でVR体験を中断されないために

  Background:
    Given Windows通知リスナーが有効である
    And アクティブプロファイル「デフォルト」が適用されている

  @implemented @booth-mvp
  Scenario: フィルタルールなしでは全通知を許可する
    Given フィルタルールが設定されていない
    When Discordアプリから通知を受信する
    Then 通知がフィルタを通過する
    And 通知カードが生成される

  @implemented @booth-mvp
  Scenario: 除外リスト方式で特定アプリの通知を遮断する
    Given フィルタモードが「除外リスト」方式である
    And アプリ名「Microsoft Store」を除外するフィルタルールが設定されている
    When Microsoft Storeから通知を受信する
    Then 通知がフィルタで遮断される
    And 通知カードは生成されない

  @implemented @booth-mvp
  Scenario: 許可リスト方式で特定アプリの通知のみ許可する
    Given フィルタモードが「許可リスト」方式である
    And アプリ名「Discord」を許可するフィルタルールが設定されている
    When Discordから通知を受信する
    Then 通知がフィルタを通過する
    When Slackから通知を受信する
    Then 通知がフィルタで遮断される

  @implemented @booth-mvp
  Scenario: フィルタはアプリ名を大文字小文字区別なく評価する
    Given アプリ名「discord」を除外するフィルタルールが設定されている
    When アプリ名「Discord」から通知を受信する
    Then 通知がフィルタで遮断される

  @implemented @booth-mvp
  Scenario: フィルタチェーンはOrder順に評価し最初のマッチで確定する
    Given 以下のフィルタルールが順に設定されている:
      | 順序 | アプリ名  | 判定 |
      | 1    | Discord  | 許可 |
      | 2    | Discord  | 除外 |
    When Discordから通知を受信する
    Then ルール1がマッチし通知は許可される

  @implemented @booth-mvp
  Scenario: 通知を受信するとアプリ名が自動的にフィルタ設定に表示される
    Given フィルタ設定画面を開いている
    When 新しいアプリ「Teams」から初めて通知を受信する
    Then フィルタ設定画面のアプリ一覧に「Teams」が自動追加される

# ============================================================
# Feature 3: 通知ライフサイクル
# ============================================================

Feature: 通知のライフサイクル管理
  VRユーザーとして
  通知が受信から消去まで適切に管理されることを期待する

  Background:
    Given システムが起動している
    And SteamVRが起動している
    And Windows通知リスナーが有効である

  @implemented @booth-mvp
  Scenario: 通知が正常フローで受信から表示まで処理される
    Given 表示スロットに空きがある
    When Discordアプリから通知を受信する
    Then 通知イベントが生成される
    And フィルタチェーンで評価される
    And 通知カードが生成される
    And 通知履歴（SQLite）に保存される
    And 通知キューに追加される
    And 空きスロットに通知カードが即座に表示される

  @implemented @booth-mvp
  Scenario: 表示スロットが満杯の場合はキューで待機する
    Given 3つの表示スロットがすべて使用中である
    When 通知を受信する
    Then 通知カードがキューに追加される
    And スロットが空いたらキューから取り出されて表示される

  @implemented @booth-mvp
  Scenario: すべての通知カードが履歴に保存される
    When 通知カードが生成される
    Then 通知履歴（SQLite）に1件追加される
    And 履歴にはソースタイプ、タイトル、本文、アプリ名、タイムスタンプが記録される

  @implemented @booth-mvp
  Scenario: おやすみモードを有効にすると全通知が抑制される
    Given おやすみモードが「全抑制」で有効化されている
    When 通知を受信する
    Then 通知カードは表示されない
    And 通知は履歴にのみ記録される

  @implemented @booth-mvp
  Scenario: おやすみモードで高優先度のみ通過させる
    Given おやすみモードが「高優先度のみ通過」で有効化されている
    When 通知を受信する
    Then Booth MVPでは全通知が Low のため表示されない
    # Phase 3: Discord Bot連携で High 優先度通知が実装されたら通過する

  @implemented @booth-mvp
  Scenario: SteamVRが起動していない場合はオーバーレイ表示をスキップする
    Given SteamVRが起動していない
    When 通知を受信する
    Then 通知カードは生成され履歴に記録される
    And オーバーレイ表示はスキップされる

# ============================================================
# Feature 4: 設定管理
# ============================================================

Feature: ユーザー設定管理
  VRユーザーとして
  設定を永続化し次回起動時に復元したい

  @implemented @booth-mvp
  Scenario: 設定変更がJSONファイルに永続化される
    Given システムが起動している
    When ユーザーが表示スロット数を「4」に変更する
    Then 設定がJSONファイル(%APPDATA%/VRNotify/settings.json)に保存される
    When システムを再起動する
    Then 表示スロット数が「4」で復元される

  @implemented @booth-mvp
  Scenario: 設定ファイルが破損している場合はデフォルト設定で起動する
    Given 設定ファイルが破損している
    When システムが起動する
    Then デフォルト設定で起動する

  @implemented @booth-mvp
  Scenario: 設定ファイルのアトミック書き込み
    Given ユーザーが設定を変更する
    When 設定ファイルの保存処理を実行する
    Then まず一時ファイル（.tmp）に書き込む
    And 一時ファイルを元のファイル名にアトミックにリネームする

  @implemented @booth-mvp
  Scenario: 初回起動時のデフォルト設定
    Given システムが初めて起動される
    Then 以下のデフォルト設定が適用される:
      | 設定項目         | デフォルト値 |
      | 表示スロット数    | 3           |
      | 表示時間         | 5秒         |
      | 不透明度         | 1.0         |
      | スケール         | 1.0         |
      | 履歴保持期間     | 7日         |
      | 履歴上限件数     | 1000件      |
      | おやすみモード    | Off         |
      | フィルタルール    | なし（全許可）|

  @implemented @booth-mvp
  Scenario: 通知履歴を設定画面から確認できる
    Given 過去に10件の通知を受信している
    When 設定画面の「履歴」タブを開く
    Then 通知履歴が新しい順に一覧表示される
    And 各通知のアプリ名、タイトル、本文、受信時刻が表示される

  @implemented @booth-mvp
  Scenario: 通知履歴をクリアできる
    Given 通知履歴に100件の記録がある
    When 「履歴をクリア」ボタンをクリックする
    Then 通知履歴が全件削除される
