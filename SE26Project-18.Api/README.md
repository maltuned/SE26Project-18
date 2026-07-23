# SE26Project-18.Api

配对工具（PairingTool）后端 API，基于 ASP.NET Core 9，提供用户认证、用户管理等功能。

---

## 技术栈

| 组件 | 技术 |
|------|------|
| 框架 | ASP.NET Core 9 Web API |
| 数据库 | MySQL 8.x / MariaDB（通过 Pomelo EF Core） |
| ORM | Entity Framework Core 9（Code-First） |
| 认证 | JWT（Access Token + Refresh Token Rotation） |
| 密码哈希 | BCrypt.Net |
| 消息队列 | RabbitMQ（Optional，不可用时自动降级） |

---

## 架构分层

```
┌─────────────────────────────────────┐
│  Controllers（接口层）               │  ← 接收 HTTP 请求，调用 Service
├─────────────────────────────────────┤
│  Services（业务逻辑层）              │  ← 核心逻辑，依赖接口而非实现
│  IAuthService / IUserService / ...  │
├─────────────────────────────────────┤
│  Data（数据访问层）                  │  ← AppDbContext + EF Core
├─────────────────────────────────────┤
│  Models（数据模型层）                │  ← Entities / Enums / Requests / Responses / Mappings
└─────────────────────────────────────┘
```

**依赖注入链**：`Controller → IAuthService/IUserService → AppDbContext / ITokenService`

- `ITokenService` — 单例（无状态）
- `RabbitMQService` — 单例（长连接）
- `IAuthService` / `IUserService` — Scoped（跟随请求生命周期）

---

## 目录结构

```
SE26Project-18.Api/
├── Controllers/
│   ├── AuthController.cs          # 认证：注册/登录/刷新/退出
│   └── UserController.cs          # 用户：获取/更新信息
├── Services/
│   ├── IAuthService.cs            # 认证接口
│   ├── AuthService.cs             # 认证实现（注册/登录/刷新/退出）
│   ├── ITokenService.cs           # Token 接口
│   ├── TokenService.cs            # JWT 生成 / RefreshToken 生成与哈希
│   ├── IUserService.cs            # 用户服务接口
│   ├── UserService.cs             # 用户服务实现（查询/更新）
│   └── RabbitMQService.cs         # 消息队列（发布/订阅）
├── Data/
│   └── AppDbContext.cs            # EF Core 上下文 + Fluent API 关系配置
├── Models/
│   ├── Entities/                  # 数据库实体
│   │   ├── User.cs                # 用户
│   │   ├── RefreshToken.cs        # 刷新令牌（存 SHA256 哈希）
│   │   ├── Game.cs / GameTag.cs   # 游戏/标签
│   │   ├── Recruitment.cs         # 招募
│   │   ├── Chat.cs / Message.cs   # 聊天/消息
│   │   ├── Response.cs            # 应征响应
│   │   └── UserTag.cs             # 用户标签
│   ├── Enums/                     # 枚举
│   │   ├── Gender.cs              # Male / Female / Other
│   │   ├── UserStatus.cs          # Online / Offline / Suspended
│   │   ├── UserRole.cs            # User / Admin
│   │   ├── RecruitmentStatus.cs   # Open / Closed / Deleted
│   │   ├── ResponseType.cs        # Accepted / Rejected
│   │   └── ChatStatus.cs          # Restricted / Free
│   ├── Requests/                  # 请求体 DTO
│   │   ├── RegisterRequest.cs
│   │   ├── LoginRequest.cs
│   │   ├── RefreshTokenRequest.cs
│   │   ├── UpdateUserRequest.cs
│   │   └── SearchGamesRequest.cs
│   ├── Responses/                 # 响应体 DTO
│   │   ├── TokenResponse.cs       # 认证返回（accessToken + refreshToken + 过期时间）
│   │   ├── UserResponse.cs        # 用户信息
│   │   ├── GameResponse.cs
│   │   ├── GameTagResponse.cs
│   │   └── UserTagResponse.cs
│   └── Mappings/                  # Entity → Response 映射扩展方法
│       ├── UserMappings.cs
│       ├── GameMappings.cs
│       ├── GameTagMappings.cs
│       └── UserTagMappings.cs
├── Migrations/                    # EF Core 迁移（自动生成）
├── Program.cs                     # 启动入口：DI 注册 + 中间件配置
├── appsettings.json               # 配置模板（可提交 Git）
└── appsettings.Development.json   # 本地开发配置（Git 忽略，含真实密码）
```

---

## API 接口文档

**Base URL**：`http://localhost:5000/api/v1`

### 认证接口 — `/Auth`

#### 1. 注册 `POST /api/v1/Auth/register`

```
Content-Type: application/json
```

**Request：**
```json
{
    "username": "testuser",
    "password": "123456"
}
```

**Response `200`：**
```json
{
    "accessToken": "eyJhbGciOiJIUzI1NiIs...",
    "refreshToken": "dGhpcyBpcyBhIHJlZnJl...",
    "accessTokenExpiresAt": "2026-07-23T04:15:00Z",
    "refreshTokenExpiresAt": "2026-07-30T03:45:00Z"
}
```

| 状态码 | 说明 |
|--------|------|
| 200 | 注册成功，返回 Token 对 |
| 400 | 用户名已存在 |

**注册成功后会自动发布 `user.registered` 事件到 RabbitMQ。**

---

#### 2. 登录 `POST /api/v1/Auth/login`

```
Content-Type: application/json
```

**Request：**
```json
{
    "username": "testuser",
    "password": "123456"
}
```

**Response `200`：** 同上 `TokenResponse` 格式

| 状态码 | 说明 |
|--------|------|
| 200 | 登录成功 |
| 401 | 用户名或密码错误 |

---

#### 3. 刷新 Token `POST /api/v1/Auth/refresh`

```
Content-Type: application/json
```

**Request：**
```json
{
    "refreshToken": "dGhpcyBpcyBhIHJlZnJl..."
}
```

**Response `200`：** 同上 `TokenResponse` 格式（返回全新的 AccessToken + RefreshToken）

| 状态码 | 说明 |
|--------|------|
| 200 | 刷新成功，旧 RefreshToken 作废 |
| 401 | RefreshToken 无效或已过期 |

**RefreshToken Rotation 机制**：每次刷新后旧的 RefreshToken 被撤销，同时清理该用户所有过期/已撤销的 Token。

---

#### 4. 退出登录 `POST /api/v1/Auth/logout`

```
Authorization: Bearer <accessToken>
Content-Type: application/json
```

**Request：**
```json
{
    "refreshToken": "dGhpcyBpcyBhIHJlZnJl..."
}
```

**Response `204 No Content`**

---

### 用户接口 — `/User`

> 所有用户接口需要 JWT 认证。

#### 5. 获取当前用户 `GET /api/v1/User/me`

```
Authorization: Bearer <accessToken>
```

**Response `200`：**
```json
{
    "id": 1,
    "username": "testuser",
    "nickname": "",
    "signature": "",
    "gender": "Other",
    "status": "Online",
    "tags": []
}
```

| 状态码 | 说明 |
|--------|------|
| 200 | 返回当前登录用户信息 |
| 401 | Token 无效或已过期 |

---

#### 6. 按 ID 获取用户 `GET /api/v1/User/{id}`

```
Authorization: Bearer <accessToken>
```

**Response `200`：** 同 UserResponse 格式

| 状态码 | 说明 |
|--------|------|
| 200 | 成功 |
| 404 | 用户不存在 |

---

#### 7. 更新用户信息 `PUT /api/v1/User/{id}`

```
Authorization: Bearer <accessToken>
Content-Type: application/json
```

**Request：**
```json
{
    "nickname": "新昵称",
    "signature": "新签名",
    "gender": "Male",
    "tagIds": [1, 2]
}
```

**Response `200`：** 更新后的 `UserResponse`

> 所有字段均为可选，传了才会更新。

---

## 认证流程

```
┌─────────┐                    ┌──────────────┐
│  前端    │                    │   后端 API    │
└────┬────┘                    └──────┬───────┘
     │   POST /Auth/register/login    │
     │ ─────────────────────────────> │ BCrypt 验密
     │                                │ 生成 AccessToken (JWT, 30min)
     │                                │ 生成 RefreshToken (随机串, 7天)
     │                                │ RefreshToken SHA256 哈希后存库
     │  { accessToken, refreshToken } │
     │ <───────────────────────────── │
     │                                │
     │   GET /User/me                 │
     │   Authorization: Bearer <at>   │
     │ ─────────────────────────────> │ JWT 验证 → 提取 sub → 查库
     │  UserResponse                  │
     │ <───────────────────────────── │
     │                                │
     │   (AccessToken 过期)           │
     │   POST /Auth/refresh           │
     │   { refreshToken }             │
     │ ─────────────────────────────> │ 查 RefreshToken 哈希
     │                                │ 旧 Token 撤销
     │  { 新 accessToken,             │ 旧 Token 物理删除
     │    新 refreshToken }           │ 发新 Token 对
     │ <───────────────────────────── │
```

**安全要点**：
- RefreshToken 在数据库中只存 **SHA256 哈希值**，即使数据库泄露也无法伪造
- 每次刷新触发 **Rotation**：旧 Token 撤销 + 数据库物理清理
- JWT 包含 `sub`（用户 ID）、`username`、`role` 三个 Claims
- AccessToken 过期时间 30 分钟，RefreshToken 7 天

---

## 消息队列

### RabbitMQ（可选）

项目启动时自动连接 RabbitMQ。如果 RabbitMQ 不可用，**静默降级**，不影响 API 正常功能。

| 事件 | 路由键 | 发布时机 |
|------|--------|----------|
| `UserRegistered` | `user.registered` | 用户注册成功后 |

**Exchange**：`pairing_tool`（Topic 模式，Durable）

**消费者示例**：项目启动时自动注册 `user_registered_queue` 队列监听 `user.registered`，当前仅输出到控制台：

```
[RabbitMQ] 收到消息: {"UserId":1,"Username":"testuser","RegisteredAt":"..."}
```

后续可扩展为：发送欢迎通知、初始化用户数据、同步到其他系统等。

---

## 本地开发

### 环境要求

- .NET 9 SDK
- MySQL 8.x 或 MariaDB
- （可选）RabbitMQ 3.x

### 1. 配置数据库

```sql
CREATE DATABASE pairing_tool CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
```

### 2. 配置连接字符串

复制 `appsettings.json` 中的 `ConnectionStrings:DefaultConnection`，把密码填入 `appsettings.Development.json`：

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=pairing_tool;User=root;Password=你的密码;"
  },
  "Jwt": {
    "Secret": "至少32字符的随机密钥"
  }
}
```

### 3. 运行数据库迁移

```bash
cd SE26Project-18.Api
dotnet ef database update
```

### 4. 启动项目

```bash
dotnet run
```

服务默认监听 `http://localhost:5000`。Swagger 文档可通过 `http://localhost:5000/openapi/v1.json` 访问。

### 5. 测试

用 Postman 或 curl 调用 `http://localhost:5000/api/v1/Auth/register`。

---

## 配置参考

### appsettings.json

| 路径 | 说明 | 默认值 |
|------|------|--------|
| `ConnectionStrings:DefaultConnection` | MySQL 连接字符串 | — |
| `Jwt:Secret` | JWT 签名密钥（至少 32 字符） | — |
| `Jwt:Issuer` | Token 签发者 | `SE26Project-18.Api` |
| `Jwt:Audience` | Token 接收方 | `SE26Project-18.MobileApp` |
| `Jwt:AccessTokenExpiryMinutes` | AccessToken 过期时间（分钟） | `30` |
| `Jwt:RefreshTokenExpiryDays` | RefreshToken 过期时间（天） | `7` |
| `RabbitMQ:Host` | RabbitMQ 地址 | `localhost` |
| `RabbitMQ:Port` | RabbitMQ 端口 | `5672` |
| `RabbitMQ:Username` | RabbitMQ 用户名 | `guest` |
| `RabbitMQ:Password` | RabbitMQ 密码 | `guest` |

---

## 数据库表结构

| 表名 | 说明 |
|------|------|
| `users` | 用户表（Username 唯一索引） |
| `refresh_tokens` | 刷新令牌（TokenHashed 唯一索引，存 SHA256 哈希） |
| `games` | 游戏 |
| `game_tags` | 游戏标签 |
| `GameGameTag` | 游戏-标签 多对多中间表 |
| `recruitments` | 招募信息 |
| `chats` | 聊天会话 |
| `messages` | 聊天消息 |
| `responses` | 招募响应 |
| `user_tags` | 用户标签 |
| `UserUserTag` | 用户-标签 多对多中间表 |

---

## 变更记录

详见仓库根目录下的 [CHANGES.md](../CHANGES.md)，记录了在团队 `feat-add-user-feature` 基础上新增和修复的内容。
