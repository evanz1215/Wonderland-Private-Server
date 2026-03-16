# Wonderland Private Server — 重構架構設計

> 此文檔記錄新版私服的完整技術棧與前後端專案結構。
> 原有技術棧（.NET Framework 4.5.2、EF6、WinForms）過於老舊，決定全部重做。

---

## 技術棧決策

| 層級 | 技術 |
|---|---|
| 管理前端 | Vue3 + TypeScript + Pinia + Vue Router |
| UI 元件庫 | Element Plus + Tailwind CSS |
| 後端框架 | C# .NET 9 + ASP.NET Core WebAPI |
| 即時通訊 | SignalR（@microsoft/signalr） |
| 架構模式 | DDD（領域驅動設計）+ CQRS（MediatR） |
| 資料庫 ORM | EF Core 9 + Pomelo.EntityFrameworkCore.MySql |
| 快取 | Redis（StackExchange.Redis） |
| 日誌 | Serilog |
| 認證 | JWT（Microsoft.AspNetCore.Authentication.JwtBearer） |
| 驗證 | FluentValidation |
| 測試 | xunit + Moq + FluentAssertions |

---

## DDD 限界上下文（Bounded Contexts）

| 上下文 | 職責 |
|---|---|
| **Identity** | 用戶註冊、登入、封禁 |
| **Characters** | 角色建立、屬性、等級、位置 |
| **Battle** | 戰鬥流程、傷害計算、結算 |
| **Inventory** | 背包、物品、裝備 |
| **World** | 地圖、NPC、傳送點 |
| **Pets** | 寵物捕捉、屬性、釋放 |

---

## 後端 Solution 結構

```
WonderlandServer/
├── src/
│   │
│   ├── WonderlandServer.Domain/              # 領域層（純業務邏輯，零外部依賴）
│   │   ├── Common/
│   │   │   ├── BaseEntity.cs                # 所有 Entity 基底類別
│   │   │   ├── AggregateRoot.cs             # 聚合根基底（含 DomainEvents 集合）
│   │   │   ├── ValueObject.cs               # 值物件基底（結構相等）
│   │   │   ├── IDomainEvent.cs              # 領域事件介面
│   │   │   └── IRepository.cs              # 泛型 Repository 介面
│   │   │
│   │   ├── Identity/
│   │   │   ├── User.cs                      # 聚合根
│   │   │   ├── UserId.cs                    # Value Object
│   │   │   ├── Email.cs                     # Value Object（含格式驗證）
│   │   │   ├── HashedPassword.cs            # Value Object
│   │   │   ├── Events/
│   │   │   │   ├── UserRegisteredEvent.cs
│   │   │   │   └── UserBannedEvent.cs
│   │   │   └── Repositories/
│   │   │       └── IUserRepository.cs
│   │   │
│   │   ├── Characters/
│   │   │   ├── Character.cs                 # 聚合根
│   │   │   ├── CharacterId.cs
│   │   │   ├── CharacterStats.cs            # Value Object（HP/MP/ATK/DEF）
│   │   │   ├── CharacterClass.cs            # Enum（戰士/法師/弓手）
│   │   │   ├── Position.cs                  # Value Object（X, Y, MapId）
│   │   │   ├── Events/
│   │   │   │   ├── CharacterCreatedEvent.cs
│   │   │   │   ├── CharacterLevelUpEvent.cs
│   │   │   │   └── CharacterMovedEvent.cs
│   │   │   ├── Services/
│   │   │   │   └── IExperienceCalculator.cs # 領域服務介面
│   │   │   └── Repositories/
│   │   │       └── ICharacterRepository.cs
│   │   │
│   │   ├── Battle/
│   │   │   ├── BattleSession.cs             # 聚合根
│   │   │   ├── BattleSessionId.cs
│   │   │   ├── Fighter.cs                   # Entity（戰鬥中的角色快照）
│   │   │   ├── BattleResult.cs              # Value Object
│   │   │   ├── DamageResult.cs              # Value Object
│   │   │   ├── Services/
│   │   │   │   └── IDamageCalculator.cs     # 傷害計算領域服務
│   │   │   ├── Events/
│   │   │   │   ├── BattleStartedEvent.cs
│   │   │   │   ├── AttackExecutedEvent.cs
│   │   │   │   └── BattleEndedEvent.cs
│   │   │   └── Repositories/
│   │   │       └── IBattleRepository.cs
│   │   │
│   │   ├── Inventory/
│   │   │   ├── Inventory.cs                 # 聚合根
│   │   │   ├── Item.cs                      # Entity
│   │   │   ├── ItemId.cs
│   │   │   ├── ItemType.cs                  # Enum
│   │   │   ├── Events/
│   │   │   │   ├── ItemAddedEvent.cs
│   │   │   │   ├── ItemRemovedEvent.cs
│   │   │   │   └── ItemEquippedEvent.cs
│   │   │   └── Repositories/
│   │   │       └── IInventoryRepository.cs
│   │   │
│   │   ├── World/
│   │   │   ├── GameMap.cs                   # 聚合根
│   │   │   ├── MapId.cs
│   │   │   ├── Npc.cs                       # Entity
│   │   │   ├── SpawnPoint.cs                # Value Object
│   │   │   ├── Events/
│   │   │   │   └── PlayerEnteredMapEvent.cs
│   │   │   └── Repositories/
│   │   │       └── IMapRepository.cs
│   │   │
│   │   └── Pets/
│   │       ├── Pet.cs                       # 聚合根
│   │       ├── PetId.cs
│   │       ├── PetStats.cs                  # Value Object
│   │       ├── Events/
│   │       │   └── PetCapturedEvent.cs
│   │       └── Repositories/
│   │           └── IPetRepository.cs
│   │
│   ├── WonderlandServer.Application/        # 應用層（用例編排，CQRS）
│   │   ├── Common/
│   │   │   ├── Behaviors/                   # MediatR Pipeline
│   │   │   │   ├── ValidationBehavior.cs    # 自動驗證（FluentValidation）
│   │   │   │   ├── LoggingBehavior.cs       # 自動記錄 Command/Query
│   │   │   │   └── TransactionBehavior.cs   # Command 自動包 DB Transaction
│   │   │   ├── Interfaces/
│   │   │   │   ├── ICurrentUser.cs
│   │   │   │   ├── IGameNotificationService.cs  # SignalR 推送抽象介面
│   │   │   │   └── ICacheService.cs
│   │   │   └── Exceptions/
│   │   │       ├── NotFoundException.cs
│   │   │       ├── ForbiddenException.cs
│   │   │       └── BusinessRuleException.cs
│   │   │
│   │   ├── Identity/
│   │   │   ├── Commands/
│   │   │   │   ├── Register/
│   │   │   │   │   ├── RegisterCommand.cs
│   │   │   │   │   ├── RegisterHandler.cs
│   │   │   │   │   └── RegisterValidator.cs
│   │   │   │   └── Login/
│   │   │   │       ├── LoginCommand.cs
│   │   │   │       ├── LoginHandler.cs
│   │   │   │       └── LoginResult.cs       # DTO（含 JWT Token）
│   │   │   └── Queries/
│   │   │       └── GetCurrentUser/
│   │   │           ├── GetCurrentUserQuery.cs
│   │   │           ├── GetCurrentUserHandler.cs
│   │   │           └── UserDto.cs
│   │   │
│   │   ├── Characters/
│   │   │   ├── Commands/
│   │   │   │   ├── CreateCharacter/
│   │   │   │   │   ├── CreateCharacterCommand.cs
│   │   │   │   │   ├── CreateCharacterHandler.cs
│   │   │   │   │   └── CreateCharacterValidator.cs
│   │   │   │   ├── UpdateCharacterStats/
│   │   │   │   │   ├── UpdateCharacterStatsCommand.cs
│   │   │   │   │   └── UpdateCharacterStatsHandler.cs
│   │   │   │   └── BanCharacter/
│   │   │   │       ├── BanCharacterCommand.cs
│   │   │   │       └── BanCharacterHandler.cs
│   │   │   ├── Queries/
│   │   │   │   ├── GetCharacter/
│   │   │   │   │   ├── GetCharacterQuery.cs
│   │   │   │   │   ├── GetCharacterHandler.cs
│   │   │   │   │   └── CharacterDetailDto.cs
│   │   │   │   └── GetCharacterList/
│   │   │   │       ├── GetCharacterListQuery.cs
│   │   │   │       ├── GetCharacterListHandler.cs
│   │   │   │       └── CharacterListDto.cs
│   │   │   └── EventHandlers/
│   │   │       └── CharacterLevelUpEventHandler.cs
│   │   │
│   │   ├── Battle/
│   │   │   ├── Commands/
│   │   │   │   ├── StartBattle/
│   │   │   │   │   ├── StartBattleCommand.cs
│   │   │   │   │   └── StartBattleHandler.cs
│   │   │   │   ├── SubmitAttack/
│   │   │   │   │   ├── SubmitAttackCommand.cs
│   │   │   │   │   └── SubmitAttackHandler.cs
│   │   │   │   └── EndBattle/
│   │   │   │       ├── EndBattleCommand.cs
│   │   │   │       └── EndBattleHandler.cs
│   │   │   ├── Queries/
│   │   │   │   └── GetBattleStatus/
│   │   │   │       ├── GetBattleStatusQuery.cs
│   │   │   │       ├── GetBattleStatusHandler.cs
│   │   │   │       └── BattleStatusDto.cs
│   │   │   └── EventHandlers/
│   │   │       └── BattleEndedEventHandler.cs  # 結算經驗值、物品掉落
│   │   │
│   │   ├── Inventory/
│   │   │   ├── Commands/
│   │   │   │   ├── AddItem/
│   │   │   │   ├── RemoveItem/
│   │   │   │   └── EquipItem/
│   │   │   └── Queries/
│   │   │       └── GetInventory/
│   │   │
│   │   ├── World/
│   │   │   ├── Commands/
│   │   │   │   └── MoveCharacter/
│   │   │   └── Queries/
│   │   │       ├── GetMapInfo/
│   │   │       └── GetOnlinePlayers/
│   │   │
│   │   └── Pets/
│   │       ├── Commands/
│   │       │   ├── CapturePet/
│   │       │   └── ReleasePet/
│   │       └── Queries/
│   │           └── GetPetList/
│   │
│   ├── WonderlandServer.Infrastructure/     # 基礎設施層（實作外部依賴）
│   │   ├── Persistence/
│   │   │   ├── AppDbContext.cs              # EF Core DbContext
│   │   │   ├── Configurations/              # Fluent API Entity 設定
│   │   │   │   ├── UserConfiguration.cs
│   │   │   │   ├── CharacterConfiguration.cs
│   │   │   │   ├── ItemConfiguration.cs
│   │   │   │   └── PetConfiguration.cs
│   │   │   ├── Repositories/               # IRepository 實作
│   │   │   │   ├── UserRepository.cs
│   │   │   │   ├── CharacterRepository.cs
│   │   │   │   ├── InventoryRepository.cs
│   │   │   │   ├── BattleRepository.cs
│   │   │   │   └── PetRepository.cs
│   │   │   └── Migrations/
│   │   │
│   │   ├── GameServer/                      # TCP 遊戲伺服器（對應原 ActionCodes）
│   │   │   ├── TcpGameServer.cs             # 主要 TCP 監聽器
│   │   │   ├── GameClient.cs                # 單一連線管理
│   │   │   ├── PacketRouter.cs              # ActionCode → Handler 路由
│   │   │   ├── PacketSerializer.cs          # 封包序列化/反序列化
│   │   │   └── Handlers/
│   │   │       ├── LoginPacketHandler.cs
│   │   │       ├── MovePacketHandler.cs
│   │   │       ├── BattlePacketHandler.cs
│   │   │       └── ChatPacketHandler.cs
│   │   │
│   │   ├── Notifications/
│   │   │   └── SignalRNotificationService.cs  # IGameNotificationService 實作
│   │   │
│   │   ├── Cache/
│   │   │   └── RedisCacheService.cs
│   │   │
│   │   ├── Security/
│   │   │   └── JwtTokenService.cs
│   │   │
│   │   └── Services/
│   │       ├── DateTimeService.cs
│   │       ├── ExperienceCalculator.cs
│   │       └── DamageCalculator.cs
│   │
│   └── WonderlandServer.API/                # 表現層（HTTP + SignalR）
│       ├── Controllers/
│       │   ├── AuthController.cs
│       │   ├── CharactersController.cs
│       │   ├── InventoryController.cs
│       │   ├── WorldController.cs
│       │   ├── BattleController.cs
│       │   └── AdminController.cs           # GM 管理指令
│       ├── Hubs/
│       │   ├── ServerStatusHub.cs           # 伺服器狀態推送（管理後台用）
│       │   └── GameEventHub.cs              # 遊戲事件推送
│       ├── Middleware/
│       │   ├── ExceptionHandlingMiddleware.cs
│       │   └── RequestLoggingMiddleware.cs
│       ├── Extensions/
│       │   ├── ServiceCollectionExtensions.cs  # DI 註冊
│       │   └── WebApplicationExtensions.cs
│       └── Program.cs
│
└── tests/
    ├── WonderlandServer.Domain.Tests/       # 純領域邏輯單元測試
    ├── WonderlandServer.Application.Tests/  # Command/Query Handler 測試
    └── WonderlandServer.Integration.Tests/  # API + DB 整合測試
```

---

## 前端結構（Vue3 + TypeScript）

```
wonderland-admin/
├── public/
│   └── favicon.ico
├── src/
│   ├── assets/
│   │   └── styles/
│   │       ├── main.css
│   │       └── variables.css
│   │
│   ├── components/                          # 可重用元件
│   │   ├── layout/
│   │   │   ├── AppLayout.vue
│   │   │   ├── Sidebar.vue
│   │   │   ├── TopBar.vue
│   │   │   └── BreadCrumb.vue
│   │   ├── common/
│   │   │   ├── DataTable.vue
│   │   │   ├── StatCard.vue
│   │   │   ├── StatusBadge.vue
│   │   │   ├── ConfirmDialog.vue
│   │   │   └── LoadingSpinner.vue
│   │   ├── dashboard/
│   │   │   ├── ServerStatsCard.vue          # CPU/記憶體/連線數
│   │   │   ├── OnlinePlayersWidget.vue
│   │   │   └── RecentEventsLog.vue
│   │   ├── players/
│   │   │   ├── PlayerTable.vue
│   │   │   ├── PlayerDetailPanel.vue
│   │   │   └── BanPlayerModal.vue
│   │   ├── characters/
│   │   │   ├── CharacterTable.vue
│   │   │   ├── CharacterStatsEditor.vue
│   │   │   └── InventoryViewer.vue
│   │   ├── world/
│   │   │   ├── MapViewer.vue
│   │   │   └── OnlinePlayersMap.vue
│   │   └── battle/
│   │       ├── BattleLogTable.vue
│   │       └── BattleDetailModal.vue
│   │
│   ├── views/                               # 頁面
│   │   ├── auth/
│   │   │   └── LoginView.vue
│   │   ├── DashboardView.vue
│   │   ├── PlayersView.vue
│   │   ├── CharactersView.vue
│   │   ├── WorldView.vue
│   │   ├── BattleLogsView.vue
│   │   ├── ItemsView.vue
│   │   └── SettingsView.vue
│   │
│   ├── stores/                              # Pinia 狀態管理
│   │   ├── auth.store.ts
│   │   ├── server.store.ts                  # 伺服器狀態（SignalR 驅動）
│   │   ├── players.store.ts
│   │   ├── characters.store.ts
│   │   └── notifications.store.ts
│   │
│   ├── composables/                         # 可重用邏輯
│   │   ├── useSignalR.ts                    # SignalR 連線管理
│   │   ├── useApi.ts                        # Axios 封裝
│   │   ├── usePagination.ts
│   │   └── useConfirm.ts
│   │
│   ├── services/                            # API 呼叫層
│   │   ├── api.client.ts                    # Axios instance + interceptors
│   │   ├── auth.service.ts
│   │   ├── character.service.ts
│   │   ├── player.service.ts
│   │   ├── world.service.ts
│   │   └── signalr.service.ts
│   │
│   ├── types/                               # TypeScript 型別定義
│   │   ├── auth.types.ts
│   │   ├── character.types.ts
│   │   ├── player.types.ts
│   │   ├── battle.types.ts
│   │   ├── item.types.ts
│   │   └── signalr.types.ts
│   │
│   ├── router/
│   │   ├── index.ts
│   │   └── guards.ts                        # 路由守衛（JWT 驗證）
│   │
│   ├── utils/
│   │   ├── formatters.ts
│   │   └── validators.ts
│   │
│   └── main.ts
│
├── .env.development
├── .env.production
├── vite.config.ts
├── tsconfig.json
└── package.json
```

---

## 套件清單

### 後端 NuGet
```
MediatR
FluentValidation.AspNetCore
Microsoft.EntityFrameworkCore
Pomelo.EntityFrameworkCore.MySql
Serilog.AspNetCore
Microsoft.AspNetCore.Authentication.JwtBearer
StackExchange.Redis
xunit
Moq
FluentAssertions
```

### 前端 npm
```json
{
  "dependencies": {
    "vue": "^3",
    "vue-router": "^4",
    "pinia": "^2",
    "axios": "^1",
    "@microsoft/signalr": "^8",
    "element-plus": "^2"
  },
  "devDependencies": {
    "typescript": "^5",
    "vite": "^5",
    "@vitejs/plugin-vue": "latest",
    "tailwindcss": "^3"
  }
}
```

---

## 資料流示意

```
Vue3                    C# API              Application Layer       Domain
 │                         │                      │                    │
 ├─ POST /battles ────────▶│                      │                    │
 │                         ├─ StartBattleCommand ▶│                    │
 │                         │                      ├─ BattleSession.Start()▶│
 │                         │                      │◀─ BattleStartedEvent ──│
 │                         │                      ├─ BattleStartedEventHandler
 │                         │                      │  └─ SignalR.Push() ─────▶│
 │◀─ SignalR 即時推送 ──────────────────────────────────────────────────│
```
