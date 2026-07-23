# 对 feat-add-user-feature 分支的补充修改

> 修改时间：2026-07-23
> 修改范围：`SE26Project-18.Api/` 下的所有变更

---

## 一、新增文件

### 1. `Services/RabbitMQService.cs`

新增 RabbitMQ 消息队列服务，功能：
- **PublishAsync** — 发布消息到指定路由键（如 `user.registered`），RabbitMQ 不可用时静默降级
- **SubscribeAsync** — 声明队列 + 绑定路由 + 注册异步消费回调
- **构造函数** — 从 `IConfiguration` 读取配置，自动恢复连接

### 2. `.gitignore`

项目缺失 `.gitignore`，新建并包含：
- `bin/`、`obj/` 等 .NET 编译产物
- `appsettings.*.json`（排除所有环境配置，`appsettings.Development.json` 含真实密码不会被提交）
- `.vscode/`、`.idea/` 等 IDE 目录
- `.env` 环境变量文件

### 3. `appsettings.Development.json`

重写本地开发配置，存放真实敏感信息（此文件被 .gitignore 排除）：
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=pairing_tool;User=root;Password=<your-password>;"
  },
  "Jwt": {
    "Secret": "dev-secret-key-at-least-32-characters-long"
  }
}
```

### 4. `README.md`

新增完整后端文档，包含：技术栈、架构分层图、目录结构、7 个 API 接口文档、JWT 认证流程图、RabbitMQ 事件表、本地开发指南、配置项参考、数据库表一览。

---

## 二、修改文件

### 5. `Services/AuthService.cs`

**改动：注入 RabbitMQService，注册成功后发布事件**
```csharp
// 新增字段
private readonly RabbitMQService? _rabbitMQ;

// 构造函数新增可选参数（RabbitMQ 不可用时不影响业务）
public AuthService(AppDbContext db, ITokenService tokenService,
    IConfiguration configuration, RabbitMQService? rabbitMQ = null)

// RegisterAsync 中 SaveChangesAsync 之后新增
if (_rabbitMQ is not null)
{
    await _rabbitMQ.PublishAsync("user.registered", new
    {
        UserId = user.Id,
        user.Username,
        RegisteredAt = DateTime.UtcNow,
    });
}
```

### 6. `Controllers/UserController.cs`

**改动：新增 GET /api/v1/User/me 端点**
```csharp
// 新增 using
using System.Security.Claims;

// 新增端点 — 从 JWT 的 sub claim 提取当前用户 ID 并查询
[HttpGet("me")]
public async Task<IActionResult> GetCurrentUser(CancellationToken ct)
```

### 7. `Program.cs`

| 改动 | 说明 |
|------|------|
| `AddSingleton<RabbitMQService>()` | 注册 RabbitMQ 服务（单例长连接） |
| 注释 `AddScoped<IGameService, GameService>()` | GameService 在 `feat-add-game-feature` 分支，合并后放开 |
| 新增消费者启动代码 | 监听 `user.registered` 路由，当前输出到控制台 |

### 8. `appsettings.json`

| 改动 | 说明 |
|------|------|
| 新增 `ConnectionStrings:DefaultConnection` | 数据库连接模板（密码为占位符 `your_password`） |
| 新增 `Jwt:Secret` | JWT 签名密钥（团队版本缺失，值为空字符串占位） |
| 新增 `RabbitMQ` section | RabbitMQ 连接配置（guest/guest，可提交） |

> 敏感信息已迁移到 `appsettings.Development.json`，`appsettings.json` 作为模板可安全提交 Git。

### 9. `SE26Project-18.Api.csproj`

| 改动 | 说明 |
|------|------|
| `net10.0` → `net9.0` | 降级以匹配本地 .NET 9 SDK |
| JwtBearer `10.0.9` → `9.0.18` | 版本兼容 |
| OpenApi `10.0.9` → `9.0.9` | 版本兼容 |
| EFCore.Design `10.0.9` → `9.0.18` | 版本兼容 |
| 新增 `RabbitMQ.Client` `7.2.1` | 消息队列客户端 |

### 10. `Models/Entities/Chat.cs`

新增 EF Core 私有无参构造函数（原仅有带参构造函数，导致 Migration 失败）：
```csharp
/// <summary>EF Core 无参构造函数</summary>
private Chat() { }
```

### 11. `Models/Entities/Response.cs`

同上，新增 EF Core 私有无参构造函数：
```csharp
/// <summary>EF Core 无参构造函数</summary>
private Response() { }
```

### 12. `Models/Entities/Message.cs`

同上，新增 EF Core 私有无参构造函数：
```csharp
/// <summary>EF Core 无参构造函数</summary>
private Message() { }
```

### 13. `Models/Entities/Recruitment.cs`

同上，新增 EF Core 私有无参构造函数：
```csharp
/// <summary>EF Core 无参构造函数</summary>
private Recruitment() { }
```

### 14. `Data/AppDbContext.cs`

**改动：追加 Fluent API 关系配置（团队版本仅有 RefreshToken、User、Game 配置）**

| 新增配置 | 说明 |
|----------|------|
| `Recruitment → Game` | 多对一，Shadow FK `GameId` |
| `Chat → Recruitment` | 多对一，Shadow FK `RecruitmentId` |
| `Chat → User（Recruiter）` | 多对一，Shadow FK `RecruiterId`，Restrict 级联删除 |
| `Chat → User（Responser）` | 多对一，Shadow FK `ResponserId`，Restrict 级联删除 |
| `Chat → Message` | 一对多，Shadow FK `ChatId` |
| `Message → User` | 多对一，Shadow FK `SenderId` |
| `Response → Recruitment` | 多对一，Shadow FK `RecruitmentId` |
| `Response → User` | 多对一，Shadow FK `ResponserId`，Restrict 级联删除 |

---

## 三、Bug 修复（团队原有代码）

### 15. `Models/Entities/GameTag.cs`

构造函数名写成了 `Game`，应为 `GameTag`：
```csharp
// 修复前：public Game(string name)
// 修复后：public GameTag(string name)
```

---

## 四、数据库

**数据库**：MySQL 8.4 `pairing_tool`

**EF Core Migration**：`20260723064020_InitialCreate` 已创建并应用，表结构：

| 表名 | 说明 |
|------|------|
| `users` | 用户表（Username 唯一索引，含 Role 字段） |
| `refresh_tokens` | 刷新令牌（TokenHashed 唯一索引，存 SHA256 哈希） |
| `games` / `game_tags` / `GameGameTag` | 游戏 + 标签（多对多） |
| `recruitments` | 招募信息（GameId 外键） |
| `chats` | 聊天会话（RecruitmentId/RecruiterId/ResponserId 外键） |
| `messages` | 聊天消息（SenderId/ChatId 外键） |
| `responses` | 招募响应（RecruitmentId/ResponserId 外键） |
| `user_tags` / `UserUserTag` | 用户标签（多对多） |

---

## 五、API 接口总览

| 方法 | 路由 | 认证 | 说明 |
|------|------|------|------|
| POST | `/api/v1/Auth/register` | 无 | 注册 → BCrypt → 发 Token → 发布 RabbitMQ 事件 |
| POST | `/api/v1/Auth/login` | 无 | 登录 → 验密 → 发 Token |
| POST | `/api/v1/Auth/refresh` | 无 | Token 刷新（Rotation + 旧撤销 + 过期清理） |
| POST | `/api/v1/Auth/logout` | JWT | 退出登录（撤销 RefreshToken） |
| GET | `/api/v1/User/me` | JWT | **新增** — 获取当前登录用户信息 |
| GET | `/api/v1/User/{id}` | JWT | 按 ID 获取用户 |
| PUT | `/api/v1/User/{id}` | JWT | 更新个人信息（可选字段） |
