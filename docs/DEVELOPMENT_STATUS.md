# Wonderland Private Server — 開發狀態與未實作功能分析

> 最後更新：2026-03-15

---

## 目錄

1. [專案架構概覽](#1-專案架構概覽)
2. [核心系統完成度](#2-核心系統完成度)
3. [Action Code 封包完成度](#3-action-code-封包完成度)
4. [各系統詳細分析](#4-各系統詳細分析)
   - [4.1 戰鬥系統](#41-戰鬥系統)
   - [4.2 技能系統](#42-技能系統)
   - [4.3 經驗值與升級系統](#43-經驗值與升級系統)
   - [4.4 寵物系統](#44-寵物系統)
   - [4.5 NPC 與商店系統](#45-npc-與商店系統)
   - [4.6 任務系統](#46-任務系統)
   - [4.7 組隊系統](#47-組隊系統)
   - [4.8 交易系統](#48-交易系統)
   - [4.9 公會系統](#49-公會系統)
   - [4.10 帳篷系統](#410-帳篷系統)
   - [4.11 好友系統](#411-好友系統)
   - [4.12 郵件系統](#412-郵件系統)
   - [4.13 聊天系統](#413-聊天系統)
   - [4.14 裝備強化系統](#414-裝備強化系統)
   - [4.15 副本系統](#415-副本系統)
   - [4.16 事件系統](#416-事件系統)
   - [4.17 Bot 系統](#417-bot-系統)
5. [資料庫層狀態](#5-資料庫層狀態)
6. [建議開發順序](#6-建議開發順序)

---

## 1. 專案架構概覽

| 專案 | 類型 | 用途 |
|------|------|------|
| **Wonderland Private Server** | WinExe（主程式） | GUI、網路層、伺服器協調、Action Code 處理 |
| **wlo.pserver.core** | Class Library | 核心遊戲邏輯：戰鬥、角色、地圖、物品、NPC |
| **wlo.pserver.maps** | Class Library | 地圖擴展（目前為空殼） |
| **wlo.pserver.Bot** | Class Library | Bot 功能（目前為空殼） |

**技術棧**：.NET Framework 4.5.2 / C# / Windows Forms / MySQL 5.7 / RCLibrary（自訂網路庫）

---

## 2. 核心系統完成度

```
✅ 完成    ⚠️ 部分完成    ❌ 未實作    💬 被註解
```

| 系統 | 狀態 | 備註 |
|------|------|------|
| 伺服器啟動/監聽 | ✅ | TCP Port 6414 |
| 帳號登入驗證 | ✅ | MD5 + 鹽值驗證 |
| 角色創建 | ✅ | 外觀、屬性、新手裝備 |
| 角色選擇/刪除 | ✅ | |
| 角色移動 | ✅ | 方向 + 座標廣播 |
| 背包系統 | ✅ | 50 格、穿脫裝/撿取/丟棄/移動 |
| 表情系統 | ✅ | |
| 玩家設定 | ✅ | |
| 地圖傳送 | ⚠️ | 基本傳送有，多數子動作為空 |
| 帳篷開關/進入 | ⚠️ | 開關/進入有、裝飾系統完整重寫：物品擺放/移動/拾取、多樓層、地板/壁紙自訂 |
| 公會管理 | ⚠️ | 完整遷移至新命名空間：GuildSystem/Guild/GuildMember、AC39 封包處理、公會倉庫、成員管理 |
| 戰鬥系統 | ⚠️ | 核心戰鬥迴圈已恢復：傷害計算、行動佇列、封包發送、NPC AI、獎勵分配 |
| 技能系統 | ⚠️ | BattleSkill 框架 + Skill.dat 整合完成（AC50 接收→SkillManager 查詢→BattleSkill 轉換） |
| 經驗/升級 | ✅ | EquipManager 已實作：經驗公式、自動升級、技能點分配、屬性成長 |
| NPC 商店 | ✅ | ShopKeeper 完整實作：商品清單、買入/賣出、定價公式、AC27 封包處理 |
| 任務系統 | ⚠️ | QuestManager + QuestTemplate + QuestNpc 框架完成、DB 表已存在，任務資料載入待開發 |
| 組隊系統 | ⚠️ | TeamManager 完整重寫：加入/邀請/踢出/解散/轉讓隊長、AC13 封包處理 |
| 交易系統 | ⚠️ | TradeManager 完整重寫：請求/接受/確認/完成交易、物品&金幣雙向交換、AC25 封包處理 |
| 寵物系統 | ✅ | PetList 完整重寫 + 捕捉機制 + 戰鬥經驗 + DB持久化、AC15 封包處理 |
| 裝備強化 | ⚠️ | 鍛造/嵌寶/轟炸/縫紉完整實作、Item 強化欄位、DB 讀寫整合、AC29 封包處理 |
| 轉生系統 | ⚠️ | PerformReborn 完整實作：等級/屬性重置、職業選擇、潛力點獎勵、AC86 封包處理 |

---

## 3. Action Code 封包完成度

| AC | 功能 | 狀態 | 詳細說明 |
|----|------|------|----------|
| AC0 | 伺服器版本 | ✅ | 發送版本與裝備欄位數 |
| AC02 | 聊天系統 | ⚠️ | Sub1:私聊 Sub2:區域聊天+GM指令 Sub3:隊伍聊天 Sub5:世界聊天 Sub6:系統公告 |
| AC06 | 角色移動 | ✅ | 方向、X/Y 座標、地圖廣播 |
| AC08 | 屬性配點 | ✅ | Sub1: 消耗潛力點分配基礎屬性（Str/Int/Wis/Con/Agi） |
| AC09 | 角色創建 | ✅ | Recv1: 建立角色；Recv2: 名稱驗證 |
| AC11 | 逃跑/PK 發起 | ⚠️ | Recv1: 逃跑；Recv2: PK（部分）；觀戰被註解 |
| AC12 | 傳送/互動狀態 | ⚠️ | 部分實作 |
| AC13 | 組隊系統 | ✅ | 加入/邀請/接受/離開/踢出/轉讓隊長 |
| AC20 | 傳送門/互動 | ⚠️ | Recv8: 傳送門；Recv1: 商店/任務NPC互動；Recv9: 對話回答 |
| AC23 | 背包操作 | ✅ | 撿取/丟棄/移動/穿戴/脫下/開帳篷/銷毀確認 |
| AC32 | 表情 | ✅ | 表情動作顯示 |
| AC33 | 玩家設定 | ✅ | |
| AC35 | 刪除角色 | ✅ | |
| AC39 | 公會管理 | ✅ | 完整遷移：邀請/加入/退出/解散/副會長/權限/規則/徽章/公會倉庫（存取/檢視） |
| AC14 | 好友系統 | ✅ | 請求(Sub1)/接受(Sub2)/刪除(Sub3) + 上線通知 |
| AC15 | 寵物系統 | ✅ | 釋放(Sub3)/召喚(Sub5)/休息(Sub6)/騎乘(Sub16)/下馬(Sub17) |
| AC25 | 交易系統 | ✅ | 請求(Sub1)/接受(Sub2)/確認(Sub3)/完成(Sub4)/取消(Sub5) |
| AC27 | NPC 商店 | ✅ | 買入(Sub1)/賣出(Sub2) 完整處理 |
| AC29 | 裝備強化 | ✅ | 鍛造(Sub1)/嵌寶(Sub2)/拆寶(Sub3)/轟炸(Sub4)/縫紉(Sub5) |
| AC50 | 戰鬥動作 | ⚠️ | Recv1: 接收攻擊指令→SkillManager查詢→BattleSkill轉換→戰鬥動作執行 |
| AC62 | 帳篷管理 | ✅ | 擺放(Sub1)/移動(Sub3)/拾取(Sub4)/外觀設定(Sub8) |
| AC63 | 登入選角 | ⚠️ | Recv2/4: 選角/登入有效；大量登入序列被註解 |
| AC64 | 帳篷建造 | ✅ | 創建/繼續/停止建造 |
| AC65 | 帳篷進入 | ✅ | 進入帳篷 + 物品取消 |
| AC82 | 公會訊息 | ❌ | 大部分方法為空（Recv7/9） |
| AC85 | 副本系統 | ✅ | 完整遷移：建立/列表/預覽/加入/退出/查看成員/踢除、等級&人數限制 |
| AC86 | 轉生系統 | ✅ | 資格檢查(Sub1)/確認轉生+職業選擇(Sub2)/地圖廣播(Sub3) |

---

## 4. 各系統詳細分析

### 4.1 戰鬥系統

**檔案**：
- `wlo.pserver.core/Game/Battle/Battle.cs` — 主戰鬥邏輯、傷害計算、回合處理
- `wlo.pserver.core/Game/Battle/BattleScene.cs` — 戰鬥場景管理、獎勵分配
- `wlo.pserver.core/Game/Battle/BattleSkill.cs` — 戰鬥技能封裝（含 SPCost）
- `wlo.pserver.core/Game/Battle/MobFighter.cs` — 怪物戰鬥者（AI + 技能）
- `Src/Network/ActionCodes/AC50_Battle.cs` — 戰鬥動作封包處理

#### ✅ 已實作
- `Battle` / `BattleScene` / `BattleAction` 類別
- 戰鬥狀態機：`Active` → `PrepState` → `ReadyState` → `CalculatingState` → `EndedState`
- `Process()` — 回合驅動、20 秒行動時限
- `Calculate()` — 完整傷害計算（物理/魔法/治療/復活/防禦/Buff/Debuff/封印/逃跑）
- `GetAtkDamage()` / `GetMatkDamage()` — 物理/魔法傷害公式
- `GetElementCorrection()` — 五行相剋修正（火/水/地/風/無）
- `SucessRate()` — 命中公式：
  - 物理 85% 基礎 + SPD 差距修正
  - 魔法 90% 基礎
  - 狀態技 70% 基礎 + 等級差修正
  - 支援技能（治療/Buff/防禦）100%
- `ApplyCrit()` — 爆擊公式：10% 基礎 + SPD 加成，上限 50%
- `GetDamage()` — 傷害/治療/SP回復統一處理：
  - 防禦減傷：物理 50%、魔法 30%
  - 治療：MATK * 0.8 + 技能威力 * 2
  - SP 回復：MATK * 0.4 + 技能威力
  - 復活：回復 25% 最大 HP
- SP 消耗扣除 — AC50_Battle 驗證 + 不足時自動轉普攻
- `DistributeRewards()` — EXP/金幣分配（含全域 EXP 倍率）
- `MobFighter` — 完整怪物戰鬥者：
  - 多技能支援（從 SkillManager 解析 skillIDs）
  - 智能 AI：40% 機率使用技能、50% 機率攻擊低血量目標
  - EXP/Gold 獎勵公式、掉落物品 ID
- `BattleSkill` — 戰鬥技能封裝：SPCost、攻擊範圍、效果層級
- 速度排序的回合順序 + Combo 攻擊（同側同速同目標合併）
- 戰鬥封包：AC 11,250（列表）/ 11,5（敵方）/ 50,6+50,1（攻擊）/ 51,1（血量更新）

#### 待開發
- 寵物參戰邏輯（Pet AI）
- 毒/持續傷害（DoT）效果
- 技能熟練度提升
- 戰鬥觀戰者封包同步
- 裝備耐久消耗

---

### 4.2 技能系統

**檔案**：`wlo.pserver.core/Game/Objects/Skills.cs`、`wlo.pserver.core/DataFiles/SkillManager.cs`

#### ✅ 已實作
- `Skilllist` 類別（技能列表管理）
- `LoadSkills()` / `SaveSkills()` — 字串序列化
- `AddSkill()` / `GetSkillByID()` — 新增/查詢
- `SkillAttackPattern` — 攻擊範圍模式（單體/橫排/直排）
- 技能資料載入器（等級需求、攻擊範圍）

#### 💬 被註解
- `CheckforUpdate()` — 技能習得邏輯

#### ⚠️ 部分完成
- ~~技能在戰鬥中的執行邏輯~~ ✅ BattleSkill + Calculate() 完整處理各效果層
- ~~技能效果（buff/debuff/治療/復活等）~~ ✅ 治療/復活/防禦/Buff/Debuff/封印 全部實作
- ~~技能消耗（SP）扣除~~ ✅ AC50_Battle 驗證 + 不足時自動轉普攻

#### ❌ 缺失
- 技能等級/熟練度
- 技能冷卻
- 被動技能

---

### 4.3 經驗值與升級系統

**檔案**：
- `wlo.pserver.core/Game/PlayerRelated/Equip.cs` — CalcMaxExp、CurExp、Level、Send8_1
- `wlo.pserver.core/Game/Battle/BattleScene.cs` — DistributeRewards（戰鬥經驗分配）
- `Src/Network/ActionCodes/AC08.cs` — 屬性配點封包處理

#### ✅ 已實作
- `CalcMaxExp()` — 經驗值公式：
  - 非轉生：`(Level + 1)^3.1 + 5`
  - 轉生 Lv1-149：`(Level + 1)^3.3 + 50`
  - 轉生 Lv150+：`(Level + 1)^3.3 + (Level + 1 - 150)^4.9`
- `Level` — 從 `m_totalexp` 動態計算（非儲存欄位）
- `CurExp` setter — 自動升級判定：
  - 經驗累積超過 `CalcMaxExp` 即升級
  - 每級 +5 技能點（SkillPoints）
  - 每級 +1~4 潛力點（CalcPotentialGain：非轉 1 點/級、轉生 2 點/級、每 10 級額外 +3）
  - 升級時自動回滿 HP/SP
  - 觸發 `OnLevelUp()` 虛擬方法
- `OnLevelUp()` — Player 覆寫：地圖廣播升級效果（AC 8,2）+ GameLogger.LogLevelUp
- `Send8_1(true)` — 升級封包：TotalExp、Level、SkillPoints、Potential、全屬性更新
- `SendExp()` — 經驗變動封包（AC 8,1 stat 36）
- `AllocateStat()` — 消耗潛力點分配基礎屬性（Str/Int/Wis/Con/Agi）
- AC08 Sub1 — 屬性配點封包：批次分配多個屬性，驗證潛力點充足
- 全域 EXP 倍率支援 — `Player.GetExpMultiplier` 委派，由 WorldEventSystem 提供
- 戰鬥經驗分配 — `DistributeRewards()` 自動套用 EXP 倍率
- ~~轉生系統邏輯~~ ✅ PerformReborn 完整實作（等級重置、職業選擇、潛力點、AC86）

#### 待開發
- 經驗值獎勵微調（隊伍加成、等級差距加成/懲罰）
- 最大等級上限強制限制（轉生後上限）

---

### 4.4 寵物系統

**檔案**：
- `wlo.pserver.core/Game/PetRelated/Pet.cs` — Pet + PetList 核心邏輯
- `wlo.pserver.core/Game/PetRelated/Equip.cs` — PetEquipManager（API 已遷移至新封包格式）
- `Src/Network/ActionCodes/AC15_Pet.cs` — AC15 封包處理

#### ⚠️ 已實作
- `Pet` 類別（繼承 `PetEquipManager`）
- 基本屬性：名稱、親密度、能力值
- `Fighter` 介面實作（`eFighterType.Pet`）
- `PetList` 完整重寫：
  - `ReceivePet()` — 接收寵物，發送 AC 15,1，自動設為戰鬥寵物
  - `DismissPet()` — 釋放寵物（AC 15,2）
  - `BringIntoBattle()` — 召喚參戰（AC 15,4 地圖廣播）
  - `RestPet()` — 休息（AC 19,2 + AC 19,7 廣播）
  - `RidePetAction()` — 騎乘（AC 15,16 廣播）
  - `UnridePet()` — 下馬（AC 15,17 廣播）
  - `GetPetlistData()` — 完整寵物清單封包（AC 15,8）
  - `SendPetlistStatData()` — 寵物狀態資料
- `PetEquipManager` API 遷移：`.Pack()` → `.Pack8()/Pack16()/Pack32()`、`SetHeader()` 移除
- AC15 封包處理：Sub3 釋放 / Sub5 召喚 / Sub6 休息 / Sub16 騎乘 / Sub17 下馬
- Player 屬性：`Pets`（`PetList` 實例）

#### ✅ 新增功能
- **寵物捕捉機制** — `EffectLayer.Capture`（ID=11）戰鬥捕捉
  - `MobFighter.Catchable` 標記可捕捉怪物
  - 捕捉成功率：基礎30%（滿血）～90%（1HP），等級差懲罰
  - `PetList.ReceivePetFromCapture()` — 從戰鬥捕捉建立寵物
  - `Pet` 新建構子：支援原始數值建立（不依賴 PhoneixNpc）
- **寵物戰鬥經驗** — 參戰寵物獲得玩家經驗的 80%
- **寵物資料庫持久化** — `charpet` 資料表
  - 儲存：NpcID、名稱、五圍、屬性、經驗、HP/SP、親密度
  - 自動建表、存檔（WritePlayer）、讀檔（GetCharacterData）

#### ❌ 尚未實作
- 寵物技能使用
- 寵物訓練
- 寵物進化/轉生

---

### 4.5 NPC 與商店系統

**檔案**：`wlo.pserver.core/Game/Maps/Code/InteractableObjects.cs`、`ShopKeeper.cs`、`QuestNpc.cs`

#### ✅ 已實作
- `ShopKeeper` — 完整商店 NPC：商品清單、買入/賣出、定價公式（等級×類型係數）
- `ShopKeeper.SendShopList()` — AC 27,3 發送商品列表
- `ShopKeeper.ProcessBuy()` — 金錢/背包驗證、扣金、加物品
- `ShopKeeper.ProcessSell()` — 物品移除、加金
- `QuestNpc` — 任務 NPC 框架：對話發送、任務接受/完成/進度檢查
- `GameMap.FindShop()` / `AddShop()` — 商店註冊與查找
- `GameMap.FindQuestNpc()` / `AddQuestNpc()` — 任務 NPC 註冊與查找
- `AC27_Shop` — 買入(Sub1)/賣出(Sub2) 封包處理
- `AC20` Recv1 — NPC 點擊路由（商店/任務NPC自動分派）

#### ❌ 完全缺失
- NPC 行走/巡邏 AI
- 特殊 NPC 功能（倉庫、銀行、傳送員等）
- 從資料檔自動載入商店 NPC 庫存

---

### 4.6 任務系統

**檔案**：`wlo.pserver.core/Game/PlayerRelated/Quest.cs`

#### ⚠️ 框架完成
- `Quest` — 任務實例（QID, progress, total, State）
- `QuestState` — 列舉（NotStarted, InProgress, Completed, TurnedIn）
- `QuestTemplate` — 任務定義模板（需求等級、前置任務、目標、獎勵）
- `QuestManager` — 完整任務管理器：
  - `AcceptQuest()` — 接受任務（含前置條件、等級檢查）
  - `UpdateProgress()` — 更新進度（殺怪/收集）
  - `TurnInQuest()` — 交回任務（扣物品、給經驗/金/物品獎勵）
  - `AbandonQuest()` — 放棄任務
  - `LoadFromDB()` / `GetDBData()` — 資料庫讀寫介面
- DB 表 `charquest` 已建立（charID, quest_started, quest_pos）

#### ✅ 新增實作
- `QuestTemplateManager` — 全域任務模板管理器：
  - `Register()` / `Get()` / `GetAll()` — 註冊/查詢模板
  - `LoadFromFile()` — 從管線分隔文字檔載入（`Data\quests.txt`）
  - 格式：`QuestID|Name|Description|MinLevel|PrereqQuestID|TargetNpcID|TargetItemID|TargetCount|RewardExp|RewardGold|RewardItemID|RewardItemAmount`
- `cGlobal.gQuestTemplates` — 全域任務模板實例
- 戰鬥殺怪自動更新任務進度（`DistributeRewards` 整合）
- GM 指令 `:quest add|complete|list|reload`
- 伺服器啟動時自動載入 `Data\quests.txt`

#### 待開發
- 任務追蹤 UI 封包
- 任務對話/劇情演出

---

### 4.7 組隊系統

**檔案**：`wlo.pserver.core/Game/PlayerRelated/Team.cs`

#### ⚠️ 核心功能完成
- `TeamManager` — 完整重寫的組隊管理器：
  - `RequestJoin()` — 發送加入請求（AC 13,1）
  - `InviteToTeam()` — 邀請加入（AC 13,9）
  - `AcceptMember()` — 接受成員（自動同步所有成員列表）
  - `LeaveTeam()` — 離開隊伍（AC 13,4 廣播）
  - `KickMember()` — 踢出成員
  - `DisbandTeam()` — 解散隊伍（清除所有成員狀態）
  - `TransferLeader()` — 轉讓隊長（AC 13,15）
  - `GetTeamDataPacket()` — 隊伍資料封包（AC 13,6）
- `AC13_Team` — 完整封包處理（Sub 1/2/4/9/10/15/17）
- Player 屬性：`PartyLeader`、`TeamMembers`、`hasParty`、`_13_6Data`

#### ✅ 新增實作
- ~~隊伍經驗分配~~ ✅ `DistributeRewards` 整合：
  - 隊伍加成：每位成員 +10% EXP（最高 +40%）
  - 同地圖非戰鬥隊友獲得 30% 經驗分享
- ~~隊伍聊天頻道~~ ✅ AC 02 Sub 3 已實作

#### 待開發
- 隊伍戰鬥（共同進入戰鬥）

---

### 4.8 交易系統

**檔案**：
- `wlo.pserver.core/Game/PlayerRelated/Trade.cs` — TradeManager 核心邏輯
- `Src/Network/ActionCodes/AC25_Trade.cs` — AC25 封包處理

#### ⚠️ 已實作
- `TradeManager` 完整重寫（原 `Wonderland_Private_Server.Code.Objects` → `Game` 命名空間）
- `RequestTrade()` — 發起交易請求，檢查 TRADABLE 設定
- `AcceptTrade()` — 接受交易，雙方連結，開啟交易視窗（AC 25,1）
- `ConfirmOffer()` — 確認出價（金幣+物品槽位），發送 AC 25,3 給對方
- `FinalizeTrade()` — 雙方確認後執行交換
- `CancelTrade()` — 取消交易通知雙方（AC 25,2 code 3）
- `ExecuteTrade()` — 物品移除→新增、金幣扣除→增加、AC 25,2 code 4 完成通知
- AC25 封包處理：Sub1 請求 / Sub2 接受 / Sub3 確認 / Sub4 完成 / Sub5 取消

#### 待開發
- 交易日誌記錄
- 交易物品鎖定（防止交易中移動物品）

---

### 4.9 公會系統

**檔案**：`wlo.pserver.core/Game/PlayerRelated/Guild.cs`

#### ✅ 已實作（完整遷移至 Game / Network.ActionCodes 命名空間）
- `GuildSystem.CreateNewGuild()` — 創建公會（扣金、分配 ID、初始化）
- `GuildSystem.onPlayerLogin()` — 登入時載入公會資料
- `Guild.AddMember()` / `AddNewMemberGuild()` — 加入成員（通知、廣播）
- `Guild.LeaveGuild()` — 離開公會
- `Guild.Dismiss()` — 開除成員
- `Guild.HoldThePostOfViceOrgleader()` — 任命副會長
- `Guild.RemoveHoldThePostOfViceOrgleader()` — 免除副會長
- `Guild.ChangePermissionMember()` — 修改成員權限
- `Guild.Edit_Rule()` — 修改公會規則
- `Guild.SendInfo()` — 登入送出完整公會資訊
- `Guild.DepositItem()` — 公會倉庫存入（AC 39,40）
- `Guild.WithdrawItem()` — 公會倉庫取出（AC 39,41）
- `Guild.SendWarehouseList()` — 檢視倉庫（AC 39,42）
- `Player.CurGuild` 屬性

#### ❌ 完全缺失
- 公會升級/等級
- 公會技能/科技樹
- 公會戰/GvG
- 公會徽章自訂（圖片上傳未實作）
- 公會捐獻
- 公會訊息板（AC82 框架有但為空）
- 公會倉庫 DB 持久化

---

### 4.10 帳篷系統

**檔案**：
- `wlo.pserver.core/Game/PlayerRelated/Tent/Tent.cs` — 帳篷核心邏輯
- `wlo.pserver.core/Game/PlayerRelated/Tent/TentItemContainer.cs` — TentFloor + PlacedItem
- `Src/Network/ActionCodes/AC62.cs` — 帳篷裝飾 AC（重寫至新系統）
- `Src/Network/ActionCodes/AC64.cs` — 帳篷建造 AC（重寫至新系統）

#### ⚠️ 已實作
- `Open()` / `Close()` — 開啟/關閉帳篷
- 繼承 `GameMap`（帳篷內部是一個地圖實例）
- `TentFloor` 類別 — 多樓層管理（2 層），每層最多 30 個物品
  - `PlaceItem()` — 擺放物品（從背包移除、廣播 AC 62,5）
  - `RemoveItem()` — 拾取物品
  - `MoveItem()` — 移動/旋轉物品
  - `GetItemListPacket()` — 建構 AC 62,4 封包
  - `SaveToDB()` / `LoadFromDB()` — 管線分隔序列化
- `Tent` 裝飾方法：
  - `PlaceItem()` — 擺放背包物品至帳篷（驗證擁有者、廣播）
  - `PickupItem()` — 拾取物品回背包（AC 62,6 廣播）
  - `MoveItem()` — 移動/旋轉物品（AC 62,7 廣播）
  - `SetFloorAppearance()` — 設定地板顏色/壁紙（AC 62,14/15 廣播）
- `SendMapInfo()` — 進入帳篷時發送所有擺放物品、地板/壁紙資料
- AC62 重寫：Sub1 擺放 / Sub3 移動 / Sub4 拾取 / Sub8 外觀設定
- AC64 重寫：Sub1 建造開始 / Sub2 繼續建造 / Sub3 停止建造

#### 待開發
- 帳篷資料 DB 讀寫整合（chartent 表已存在）
- 關閉帳篷時傳送內部玩家
- 建造計時器（目前即時完成）
- 帳篷訪客權限管理

---

### 4.11 好友系統

**檔案**：
- `wlo.pserver.core/Game/PlayerRelated/Friends.cs` — FriendManager 核心邏輯
- `Src/Network/ActionCodes/AC14_Friends.cs` — AC14 封包處理

#### ⚠️ 已實作
- `FriendManager` 完整重寫（原 `Game.Code.PlayerRelated.Friendlist` → `Game.FriendManager`）
- `RequestFriend()` — 發送好友請求（AC 14,1）
- `AcceptFriend()` — 雙向加好友 + 通知（AC 14,9）
- `RemoveFriend()` — 刪除好友 + 通知（AC 14,4）
- `SendFriendList()` — 登入時發送好友清單（AC 14,5）
- `NotifyFriendsOnline()` / `NotifyFriendsOffline()` — 上下線通知（AC 14,7）
- `LoadFromDB()` / `SaveToDB()` — 資料庫序列化
- `GlobalFindPlayer` 靜態委派 — 跨地圖玩家搜尋（由 WorldServer 初始化設定）
- 登入時自動發送好友清單 + 通知好友上線

#### 待開發
- 離線時通知好友（需整合斷線事件）
- 黑名單/封鎖功能
- 好友資料庫讀寫整合（LoadFinalData 載入）

---

### 4.12 郵件系統

**檔案**：
- `wlo.pserver.core/Game/PlayerRelated/Mail.cs` — MailManager + MailEntry
- `Src/Network/ActionCodes/AC14_Friends.cs` — AC14 Sub10-13 郵件封包處理

#### ⚠️ 已實作
- `MailManager` 完整重寫（原 `Game.Code.PlayerRelated.MailManager` → `Game.MailManager`）
- `MailEntry` 資料結構（寄件人、訊息、時間戳、附件物品ID/數量、已讀狀態）
- `SendMail()` — 發送郵件，線上直接送達（AC 14,1），支援物品附件
- `ReceiveMail()` — 接收郵件 + 即時通知
- `SendMailList()` — 郵件清單回應（AC 14,11）
- `DeleteMail()` — 刪除郵件（AC 14,12）
- `TakeAttachment()` — 領取附件物品到背包（AC 14,13）
- `LoadFromDB()` / `SaveToDB()` — 管線分隔格式序列化
- `GlobalFindPlayer` 靜態委派 — 跨地圖玩家搜尋

#### 待開發
- 離線郵件投遞（目前僅線上玩家可收）
- 系統郵件（伺服器端主動發送）
- 郵件過期清理機制

---

### 4.13 聊天系統

**檔案**：`Src/Network/ActionCodes/AC02.cs`、`Src/Server/WorldServer.cs`

#### ⚠️ 已實作
- Sub 1: 私聊/密語 — 跨地圖搜尋目標玩家，發送/回顯封包
- Sub 2: 區域聊天 — 地圖內文字廣播 + GM 指令系統（見下方完整列表）
- Sub 3: 隊伍聊天 — 發送給所有隊伍成員
- Sub 5: 世界聊天 — 透過 WorldServer.BroadcastAll() 全伺服器廣播
- Sub 6: 系統公告 — 伺服器端發起，靜態方法 `SendSystemMessage()` / `BroadcastSystemAnnouncement()`
- WorldServer 新增：`FindPlayerByName()`、`BroadcastAll()`
- GameMap 新增：`FindPlayerByName()`

#### GM 指令列表
| 指令 | 說明 |
|------|------|
| `:item add [id] [數量]` | 給予物品 |
| `:warp [地圖] [x] [y]` | 傳送到指定地圖座標 |
| `:announce [訊息]` | 全伺服器公告 |
| `:kick [玩家名]` | 踢出玩家 |
| `:mute [玩家名] [分鐘]` | 禁言（預設10分鐘） |
| `:unmute [玩家名]` | 解除禁言 |
| `:goto [玩家名]` | 傳送到玩家位置 |
| `:summon [玩家名]` | 將玩家傳送到自己位置 |
| `:gold [數量]` | 給予金幣 |
| `:level [等級]` | 設定等級（1-199） |
| `:heal` | 完全回復 HP/SP |
| `:exp [數量]` | 給予經驗值 |
| `:info [玩家名]` | 查看玩家資訊 |
| `:online` | 查看線上人數 |
| `:event exp [分鐘]` | 啟動雙倍經驗活動 |
| `:event drop [分鐘]` | 啟動雙倍掉寶活動 |
| `:event status` | 查看活動狀態 |

- 所有 GM 指令自動記錄至 GameLogger

#### 待開發
- 公會聊天頻道
- 髒話過濾

---

### 4.14 裝備強化系統

**檔案**：
- `wlo.pserver.core/Game/Item.cs` — Item 強化欄位（SocketID, BombID, SewID, Forge）
- `wlo.pserver.core/Game/Enums/SocketEnum.cs` — ForgeLevel、EnhanceType 列舉
- `Src/Network/ActionCodes/AC29_Enhance.cs` — AC29 封包處理

#### ⚠️ 已實作
- `Item` 類別新增強化欄位：`SocketID`、`BombID`、`SewID`、`Forge`
- `Clear()` / `CopyFrom()` 完整處理強化欄位
- `Equip` 類別鍛造倍率：每級 +5% 基礎屬性加成
- `InventoryDBData` / `EqData` 包含強化欄位（DB 讀寫完整）
- `LoadFinalData()` / `GetCharacterData()` 載入強化資料
- AC29 封包處理：
  - Sub1 鍛造：成功率隨等級遞減（100%→10%），+5 以上失敗歸零，金幣消耗
  - Sub2 嵌寶：消耗背包寶石、設定 SocketID
  - Sub3 拆寶：500 金移除寶石、退回背包
  - Sub4 轟炸：消耗背包炸彈、設定 BombID
  - Sub5 縫紉：消耗背包布料、設定 SewID
- `ForgeLevel` 列舉（None → Plus10）
- `EnhanceType` 列舉（Forge/Socket/Unsocket/Bomb/Sew）
- Player 新增 `GetEquip(byte)` 方法

#### 待開發
- 嵌寶/轟炸/縫紉的具體屬性加成計算（需寶石/炸彈/布料物品資料）
- 強化 NPC 介面整合
- 裝備耐久度消耗與修復

---

### 4.15 副本系統

**檔案**：
- `wlo.pserver.core/Game/Objects/Instance.cs` — InstanceSystem（全域管理）+ DungeonInstance + InstanceData
- `Src/Network/ActionCodes/AC85.cs` — AC85_Instance 封包處理

#### ✅ 已實作（完整遷移至 Game / Network.ActionCodes 命名空間）
- `InstanceSystem.CreateInstance()` — 建立副本隊伍（AC 85,8 / 85,5 / 85,2）
- `InstanceSystem.SendInstanceList()` — 分頁列表（AC 85,1）
- `InstanceSystem.PreJoin()` — 預覽成員（AC 85,4）
- `InstanceSystem.JoinInstance()` — 加入副本（等級/人數檢查、AC 85,7 / 85,5 / 85,3）
- `InstanceSystem.ExitInstance()` — 離開副本（AC 85,12 / 85,11 / 85,5）
- `InstanceSystem.CheckMembers()` — 成員列表分頁（AC 85,6）
- `InstanceSystem.DismissMember()` — 踢除成員（AC 85,12 / 85,5）
- `Player.CurInstance` 屬性
- 3 組預設副本資料（Yoyo Family / Slime Cave / Dark Forest）
- AC85_Instance.Instances 靜態引用（於 WorldServer.Initialize 設定）

#### ❌ 缺失
- 副本地圖生成（獨立地圖實例）
- 副本計時器
- 副本 Boss / 怪物生成
- 副本獎勵分配
- 副本資料從 DB/檔案載入
- SceneLoader 副本欄位整合

---

### 4.16 事件系統

**檔案**：`Src/Server/System/EventSystem.cs`

#### ✅ 已實作
- 事件框架（玩家網路事件）
- 定時重複事件系統
- 事件調度器

#### ❌ 完全缺失
- 具體遊戲事件（世界 Boss、節日活動等）
- 事件觸發條件
- 事件獎勵

---

### 4.17 Bot 系統

**檔案**：`Src/Server/Bots/GmBot.cs`、`Cupid.cs`

#### ❌ 僅有空殼
- `GmBot` — 最小框架
- `Cupid` — 僅繼承 GmBot，無邏輯
- 無 Bot AI、無指令處理、無自動化行為

---

## 5. 資料庫層狀態

| 資料表 | 狀態 | 說明 |
|--------|------|------|
| `user` | ✅ | 帳號驗證，欄位完整 |
| `characters` | ✅ | 角色基本資料，讀寫完整 |
| `charactersExtData` | ⚠️ | Settings 有讀寫；Friends/Guild/Mail 被註解 |
| `stats` | ✅ | 能力值讀寫完整 |
| `inventory` | ✅ | 背包/裝備讀寫完整，含 socket/bomb/sew/forge 預留欄位 |
| `chartent` | ⚠️ | 已自動建立，但無讀寫邏輯 |
| `charquest` | ⚠️ | 已自動建立，但無讀寫邏輯 |
| `charunlocks` | ⚠️ | 已自動建立，但無讀寫邏輯 |

---

## 6. 建議開發順序

### 第一階段：核心戰鬥循環
> 目標：讓玩家能打怪、獲得經驗、升級

1. ~~**戰鬥傷害計算**~~ ✅ Calculate() 完整實作：物理/魔法/治療/防禦/Buff/Debuff/封印/逃跑、爆擊、SP消耗
2. ~~**NPC/怪物 AI**~~ ✅ MobFighter 智能 AI：多技能、低血量優先、SP管理
3. ~~**經驗值與升級系統**~~ ✅ 經驗公式、等級提升、潛力/技能點分配、AC08 屬性配點、EXP倍率
4. ~~**戰鬥獎勵分配**~~ ✅ DistributeRewards 自動分配經驗/金幣給存活玩家

### 第二階段：遊戲互動
> 目標：讓玩家能購物、接任務、使用技能

5. ~~**技能執行系統**~~ ✅ — 技能效果計算、SP 消耗驗證、BattleSkill.SPCost、技能層級分類
6. ~~**NPC 商店**~~ ✅ — ShopKeeper 買賣交易、定價公式、AC27 封包處理
7. ~~**任務系統**~~ ✅ — QuestTemplateManager、quests.txt 模板載入、戰鬥自動更新擊殺進度、GM :quest 指令
8. ~~**組隊系統**~~ ✅ — 組隊經驗分配（+10%/人）、同地圖非戰鬥隊員30%分享

### 第三階段：社交與經濟
> 目標：完善玩家間互動

9. ~~**交易系統**~~ ✅ — Trade 完整實作：請求/接受/確認/完成/取消、物品+金幣原子交換
10. ~~**聊天系統完善**~~ ✅ — 私聊/區域/隊伍/世界/系統公告、GM指令整合
11. ~~**好友系統完善**~~ ✅ — 請求/接受/刪除、上線通知、DB持久化
12. ~~**郵件系統完善**~~ ✅ — 收發/刪除/物品附件、DB持久化

### 第四階段：進階系統
> 目標：增加遊戲深度

13. ~~**寵物系統**~~ ✅ — 捕捉機制、戰鬥經驗、DB持久化、召喚/騎乘/釋放
14. **裝備強化** — 鍛造、嵌寶、轟炸
15. **帳篷裝飾** — 物品擺放、多樓層
16. **公會進階** — 公會倉庫、升級、GvG
17. **副本系統** — 副本地圖、Boss、獎勵
18. **轉生系統** — 轉生流程、職業選擇

### 第五階段：營運工具
> 目標：支援伺服器管理

19. **GM 指令擴充** ✅ — 17 項指令：踢人/禁言/傳送/召喚/金幣/等級/治療/經驗/資訊/線上人數/活動
20. **世界活動/事件** ✅ — WorldEventSystem：定時公告、雙倍經驗/掉寶、週末活動
21. **Bot 系統** ✅ — BotManager + Cupid Bot 框架、GmBot 抽象類
22. **日誌與監控** ✅ — GameLogger：登入/登出/聊天/交易/GM指令/物品/金幣/升級/轉生記錄
