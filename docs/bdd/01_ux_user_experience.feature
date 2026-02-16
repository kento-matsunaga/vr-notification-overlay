# VRNotify - UX/ユーザー体験視点BDD
# 更新: 2026-02-17 Windows通知リスナー方式 + Booth MVP

# ============================================================
# Feature 1: VR空間内での通知表示体験
# ============================================================

Feature: VR空間内での通知表示体験
  VRChatユーザーとして
  VR空間の没入感を維持しながらWindows通知を確認したい
  そのために通知は視界の邪魔にならない位置に、適切な視認性で表示される必要がある

  Background:
    Given SteamVRが起動している
    And 本アプリ「VRNotify」がSteamVRオーバーレイとして登録されている
    And Windows通知リスナーが有効である
    And VRChatが起動しワールドに入室している

  # --- 通知の表示位置 ---

  @implemented @booth-mvp
  Scenario: デフォルトの通知表示位置はHMD追従の視界上部である
    Given 通知の表示位置が「HMD上部」に設定されている
    When Discordアプリから通知を受信する
    Then 通知がHMDの視界上部にオーバーレイとして表示される
    And 通知はユーザーの頭の動きに追従する
    And 通知は視界の中央を遮らない位置に表示される

  # --- 通知の視認性 ---

  @implemented @booth-mvp
  Scenario: 通知のデフォルトの見た目
    When Windowsアプリから通知を受信する
    Then 通知カードが半透明の暗色背景で表示される
    And テキストは白色で読みやすいフォントで表示される
    And 通知カードにアプリ名と通知タイトル・本文が表示される
    And メッセージが長い場合は末尾が省略される

  @implemented @booth-mvp
  Scenario: 通知のサイズと不透明度をユーザーがカスタマイズできる
    Given 設定画面でスケールを「150%」、不透明度を「60%」に変更している
    When 通知が表示される
    Then 通知カードは設定されたスケールと不透明度で表示される

  # --- 通知の表示時間 ---

  @implemented @booth-mvp
  Scenario: 通知はデフォルト5秒表示後に消える
    When Windowsアプリから通知を受信する
    Then 通知が表示される
    And 5秒間表示が維持される
    And 表示時間経過後に消える

  @implemented @booth-mvp
  Scenario: 通知の表示時間をユーザーが変更できる
    Given 設定画面で通知表示時間を「10秒」に変更している
    When 通知を受信する
    Then 通知は10秒間表示された後に消える

  # --- 複数通知のスタック表示 ---

  @implemented @booth-mvp
  Scenario: 複数通知が同時に届いた場合のスタック表示
    When 3件の通知が短時間に届く
    Then 各通知が別々の表示スロットに表示される
    And 表示スロット数はデフォルト3（最大5）である

  @implemented @booth-mvp
  Scenario: スタック上限を超えた場合の処理
    Given 表示スロット数が「3」に設定されている
    And 3つのスロットがすべて使用中である
    When 4件目の通知が届く
    Then 通知はキューに蓄積される
    And スロットが空いたらキューから取り出されて表示される

  # --- 没入感の維持 ---

  @future @phase2
  Scenario: 通知音はVRChatのワールドBGMを邪魔しない
    Given 通知音が「有効」に設定されている
    And 通知音量が「30%」に設定されている
    When 通知を受信する
    Then 短い通知音が設定音量で再生される
    And 通知音はSteamVRの通知サウンドチャンネルから出力される

  @future @phase2
  Scenario: 配信モードでの通知内容マスク
    Given 配信モードが「有効」に設定されている
    When 通知を受信する
    Then 通知の送信者名・本文がマスクされる
