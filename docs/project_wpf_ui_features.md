---
name: WPF UI 可開發功能清單
description: 盤點後端所有系統能力後整理的 WPF 管理面板可開發功能清單，按優先級排序
type: project
---

## WPF 管理面板功能開發清單

**Why:** 使用者需要規劃 WPF 管理面板的功能開發順序
**How to apply:** 開發新 UI 頁面時參考此清單，確認後端系統路徑和 API

### 已完成頁面（7 個）

| 頁面 | ActivePageIndex | 功能 |
|------|----------------|------|
| 系統總覽 (SystemView) | 0 | 儀表板統計 |
| 即時日誌 (LogsView) | 1 | 主控台輸出 |
| 線上玩家 (OnlineUsersView) | 2 | 玩家清單、搜尋、踢除 |
| 管理面板 (AdminView) | 3 | 資料庫連線設定 |
| 伺服器設定 (ConfigurationView) | 4 | 設定檔 + DB 欄位映射 |
| 更新管理 (UpdateView) | 5 | 更新系統 |
| 商城管理 (ItemMallView) | 6 | Transfer 穿梭框上架/下架 |

導航結構：Nav[0-3] = System/Logs/Online/Admin，Settings[0-2]+4 = Config(4)/Update(5)/Mall(6)

### Tier 1 — 後端已完整，可直接串接

1. **世界事件管理** — `Src/Server/System/WorldEventSystem.cs`
   - ExpMultiplier, DropMultiplier, StartDoubleExp(), StartDoubleDrop(), GetStatus()
2. **遊戲日誌檢視** — `Src/Server/System/GameLogger.cs`
   - LogLogin/Logout/Chat/Trade/GMCommand/ItemGain/Gold/LevelUp/Reborn
   - 寫入 logs/ 目錄
3. **全服廣播** — `WorldServer.BroadcastAll(SendPacket)`
4. **公會管理** — `wlo.pserver.core/Game/PlayerRelated/Guild.cs` + `cGlobal.gGuildSystem`
   - CreateNewGuild(), GetGuild(), RemoveGuild()
5. **任務編輯器** — `wlo.pserver.core/Game/PlayerRelated/Quest.cs` + `cGlobal.gQuestTemplates`
   - Register(), Get(), Unregister(), LoadFromFile("Data/quests.txt")
6. **排程任務管理** — `Src/Server/System/TaskManager.cs`
   - BindingList<taskItem>, CreateTask(), EndTask(), ChangeInterval()

### Tier 2 — 需少量後端補充

- 玩家詳細資訊（背包/裝備/寵物/好友/郵件）
- 角色資料庫瀏覽 (CharacterDataBase)
- 帳號管理 (UserDataBase)
- 地圖監控 (WorldServer.MapList - private, 需暴露)

### Tier 3 — 需較多開發

- 副本監控 (InstanceSystem)
- 寵物管理 (PetList)
- 交易監控 (TradeManager)
- NPC/怪物/物品/技能瀏覽器 (PhxNpcDat, PhxItemDat, SkillManager)
- 封鎖/IP 管理 (LoginServer)
