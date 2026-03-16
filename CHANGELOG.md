# Wonderland Private Server - 開發紀錄

## 2026-03-17 開發紀錄

### 一、NPC 資料解碼系統

#### 新增檔案
- **`Src/DataFiles/NpcDecoder.cs`** — NPC 資料 XOR 解碼器
  - 解碼 PhoenixData.dll 中 `PhxNpcDat` 的 NPC 資料（Npc.dat 共 4376 筆）
  - XOR 解碼公式：
    - `byte` 欄位：`val ^ 0xC8`（不減 9，避免小數值 underflow）
    - `ushort` 欄位：`val ^ 0x5209`（不減 9，同上原因）
    - `NpcID` 特殊處理：`(val ^ 0x5209) - 9`（需減 9 以對應 Eve 資料 ID）
    - `uint` 欄位：`(val ^ 0xBAEB716) - 9`（HP/SP/顏色值解碼後皆 > 9）

- **`Src/DataFiles/EveNpcMapper.cs`** — Eve.emg NPC 對應表解析器
  - 從 Eve.emg 二進位檔解析地圖 NPC 的 clickID → npcID 對應關係
  - 用於地圖上點擊 NPC 時，將地圖 clickID 轉換為實際的 NpcID

#### 修改檔案
- **`Src/DataFiles/NpcManager.cs`** — 新增 `GetNpcbyID(ushort)` 方法，根據 NpcID 查找 NPC 資料
- **`Src/cGlobal.cs`** — 新增 `gNpcManager` 全域靜態參考

### 二、戰鬥系統修復

#### 問題與修復
1. **PK_NPC 判斷值錯誤**
   - 解碼後 PK_NPC=1 代表怪物（可戰鬥），PK_NPC=2 代表非戰鬥 NPC
   - 修正 `AC20.cs` 和 `WorldServer.cs` 中的判斷從 `== 0` 改為 `== 1`

2. **戰鬥中看不到怪物（客戶端閃退）**
   - 原因：`MobFighter.ID` 使用自動遞增值（100000+），客戶端無法根據此 ID 查找怪物圖片
   - 修正：`MobFighter.ID` 改為回傳 `NpcID`（如 17000），客戶端可正確查找 Npc.dat 中的戰鬥圖片

#### 修改檔案
- **`wlo.pserver.core/Game/Battle/MobFighter.cs`** — `ID` 屬性改回傳 `m_npcID`
- **`wlo.pserver.core/Game/Maps/Map.cs`** — 新增戰鬥開始 debug log，新增 `FindMonsterNpc()`、`PopulateMonsterNpcs()` 方法
- **`Src/Network/ActionCodes/AC20.cs`** — 完整重寫 NPC 點擊處理邏輯（商店 → 任務 NPC → 怪物）
- **`Src/Server/WorldServer.cs`** — `PopulateMonsterNpcs()` 使用 Eve 對應表為地圖註冊怪物 NPC

### 三、伺服器基礎設施改善

1. **地圖載入修復**
   - `MainForm1.cs`：設定 `Environment.CurrentDirectory = AppDomain.CurrentDomain.BaseDirectory`
   - 解決 VS 啟動時 CWD 指向專案根目錄，導致 `Maps/` 資料夾找不到的問題

2. **Log 系統增強**
   - `MainForm1.cs`：`WriteToLogFile` 加入時間戳記格式 `[HH:mm:ss.fff]`

3. **Plugin 系統改善**
   - `Plugin.cs`：silent `catch {}` 改為輸出錯誤訊息到 DebugSystem

4. **Login Server**
   - `LoginServer.cs`：新增 `SO_REUSEADDR` 選項，避免重啟時 port 被占用

### 四、地圖系統

#### 新增檔案
- **`wlo.pserver.maps/Maps/Map60000_NorthIsland.cs`** — 北島地圖（含怪物 NPC 配置）
- **`wlo.pserver.maps/Maps/Map10019_Lobby.cs`** — 大廳地圖
- **`wlo.pserver.maps/Maps/Map11016_SouthBay.cs`** — 南灣地圖

### 五、先前 commit 包含的大型更新（cd8da84）

此 commit 包含大量功能新增與重構：

- **戰鬥系統**：`Battle.cs`、`BattleScene.cs`、`BattleSkill.cs`、`MobFighter.cs` 完整重寫
- **寵物系統**：`Pet.cs`、寵物裝備、寵物捕捉機制
- **社交系統**：組隊（AC13）、好友（AC14）、交易（AC25）、公會系統
- **任務系統**：`Quest.cs`、`QuestNpc.cs` 重構
- **商店系統**：AC27 商店購買/販賣
- **強化系統**：AC29 裝備強化
- **轉生系統**：AC86 轉生功能
- **副本系統**：`Instance.cs` 副本管理
- **遊戲記錄**：`GameLogger.cs` 遊戲事件 log
- **世界事件**：`WorldEventSystem.cs` 世界事件系統
- **Docker 支援**：`docker-compose.yml`、`docker/init.sql` MySQL 容器化
- **gitignore 清理**：移除大量不應追蹤的 bin/obj/packages 檔案

---

## 已知待修項目

- [ ] 戰鬥中怪物顯示 — 已修改 MobFighter.ID，待測試驗證
- [ ] 怪物地圖上移動（NPC 巡邏/走動）— 尚未實作
- [ ] 多隻怪物同場戰鬥時 ID 唯一性問題（目前單怪戰鬥正常）
- [ ] 伺服器早期連線時偶發崩潰（已加 SO_REUSEADDR，可能需更多處理）

---

## 技術備註

### NPC 資料流程
```
Npc.dat (XOR encoded) → PhoenixData.dll (PhxNpcDat) → NpcDecoder.DecodeAllNpcs() → 解碼後的 NPC 資料
Eve.emg → EveNpcMapper → clickID ↔ npcID 對應表
玩家點擊 NPC → AC20 Recv1 → FindMonsterNpc(clickID) → GetNpcbyID(npcID) → MobFighter → Battle
```

### 戰鬥封包格式
```
AC 11,250 — 戰鬥初始化（列出己方所有戰鬥者）
AC 11,5   — 對方戰鬥者資料
每位戰鬥者: BattlePosition(1) + TypeofFighter(1) + ID(4) + ClickID(2) + OwnerID(4)
           + GridX(1) + GridY(1) + MaxHP(4) + MaxSP(2) + CurHP(4) + CurSP(2)
           + Level(1) + Element(1) + Reborn(1) + Job(1)
```

### 建置指令
```bash
MSBuild "Wonderland Private Server.sln" /p:Configuration=Debug /verbosity:minimal
```
