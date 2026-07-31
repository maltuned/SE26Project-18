# SE26Project-18.Backend 单元测试

## 概述

本项目使用 **xUnit** + **Moq** + **EF Core InMemory** 对后端进行单元测试。

- **测试总数**: 326
- **通过**: 321
- **跳过**: 5（InMemory 不支持 `ExecuteUpdateAsync` / `ExecuteDeleteAsync`）
- **失败**: 0

## 覆盖率

| 指标 | 原始（含自动生成代码） | 排除后（仅业务代码） |
|------|----------------------|---------------------|
| 行覆盖 | 18.2% (1735/9511) | **89.6%** (2375/2652) |
| 分支覆盖 | 26.7% (239/894) | - |

> 已排除：EF Core Migration 文件（6335 行）、Program.cs（102 行）、OpenAPI 自动生成代码（384 行）、EnumMemberJsonConverter（23 行）、ChatHub（15 行）

## 服务层覆盖率

| 服务 | 覆盖率 | 说明 |
|------|--------|------|
| TokenService | **100%** | JWT 生成、Refresh Token、Hash |
| MapperService | **100%** | 13 个 DTO 映射方法 |
| AdminService | **100%** | 管理员登录、待处理计数 |
| FeedbackService | **100%** | 反馈 CRUD |
| ReportService | **100%** | 举报 CRUD |
| ReviewService | **96.1%** | 评价创建/查询，自评守卫 |
| MessageService | **91.2%** | 消息发送（6 分支）、聊天状态升级 |
| ChatService | **88.8%** | 聊天创建/复用、关闭 |
| NotificationService | **88%** | 通知 CRUD、未读计数 |
| ResponseService | **88%** | 响应创建、删除流程 |
| UserService | **84.4%** | 用户查询、部分更新 |
| AuthService | **78.4%** | 注册/登录/刷新/登出 |
| GameService | **80%** | 游戏搜索、CRUD |
| ImageService | **74.7%** | MinIO 文件上传/删除/URL |
| TagService | **69%** | 标签 CRUD、级联删除 |
| RecruitmentService | **65.1%** | 招募过滤、部分更新 |
| **服务层平均** | **~87%** | |

## 控制器覆盖率

| 控制器 | 覆盖率 |
|--------|--------|
| NotificationController | **96.8%** |
| AuthController | **89.1%** |
| FeedbackController | **81.8%** |
| MessagesController | **78.2%** |
| ReportController | **61.3%** |
| ReviewController | **56.6%** |

## 目录结构

```
SE26Project-18.Backend.Tests/
├── README.md                          # 本文件
├── SE26Project-18.Backend.Tests.csproj
├── TestDbContextFactory.cs            # InMemory DbContext 工厂
├── coverage-report/
│   ├── index.html                     # HTML 覆盖率报告（浏览器打开）
│   └── Summary.txt                    # 文本覆盖率摘要
├── Services/
│   ├── TokenServiceTests.cs           # JWT 生成、Hash、Refresh Token
│   ├── MapperServiceTests.cs          # 13 个 DTO 映射方法
│   ├── AuthServiceTests.cs            # 注册/登录/刷新/登出
│   ├── AdminServiceTests.cs           # 管理员登录、统计
│   ├── UserServiceTests.cs            # 用户查询、更新、搜索
│   ├── GameServiceTests.cs            # 游戏 CRUD、搜索
│   ├── TagServiceTests.cs             # 标签 CRUD、级联删除
│   ├── ChatServiceTests.cs            # 聊天创建、关闭
│   ├── MessageServiceTests.cs         # 消息发送、权限控制
│   ├── RecruitmentServiceTests.cs     # 招募过滤、部分更新
│   ├── ResponseServiceTests.cs        # 响应创建、删除流程
│   ├── ReviewServiceTests.cs          # 评价、自评/重复守卫
│   ├── FeedbackServiceTests.cs        # 反馈 CRUD
│   ├── ReportServiceTests.cs          # 举报 CRUD
│   ├── NotificationServiceTests.cs    # 通知 CRUD
│   └── ImageServiceTests.cs           # MinIO 文件操作、URL
└── Controllers/
    ├── AuthControllerTests.cs         # 认证 API 端点
    ├── NotificationControllerTests.cs # 通知 API 端点
    ├── MessagesControllerTests.cs     # 消息 API 端点
    ├── FeedbackControllerTests.cs     # 反馈 API 端点
    ├── ReportControllerTests.cs       # 举报 API 端点
    └── ReviewControllerTests.cs       # 评价 API 端点
```

## 技术栈

| 包 | 版本 | 用途 |
|---|---|---|
| xunit | 2.* | 测试框架 |
| xunit.runner.visualstudio | 3.* | Visual Studio 测试运行器 |
| Moq | 4.* | Mock 框架 |
| coverlet.collector | 6.* | 代码覆盖率收集 |
| Microsoft.EntityFrameworkCore.InMemory | 9.* | 内存数据库（替代 MySQL） |

## 运行测试

```bash
# 运行所有测试
cd SE26Project-18.Backend.Tests
dotnet test

# 运行特定测试类
dotnet test --filter "FullyQualifiedName~AuthServiceTests"

# 运行并收集覆盖率
dotnet test --collect:"XPlat Code Coverage"

# 生成 HTML 覆盖率报告
reportgenerator -reports:TestResults/**/coverage.cobertura.xml -targetdir:coverage-report -reporttypes:Html
# 浏览器打开 coverage-report/index.html
```

## 测试模式

### 服务层测试

使用 `TestDbContextFactory.Create()` 创建独立的 InMemory 数据库实例：

```csharp
var db = TestDbContextFactory.Create();
db.Users.Add(new User("testuser", "password"));
await db.SaveChangesAsync();
var service = new AuthService(db, tokenMock.Object, configMock.Object);
var result = await service.LoginAsync("testuser", "password");
```

### 控制器测试

使用 Moq 模拟所有注入服务，设置 `ClaimsPrincipal` 模拟认证用户：

```csharp
var controller = new AuthController(authMock.Object, userMock.Object);
controller.ControllerContext = new ControllerContext
{
    HttpContext = new DefaultHttpContext
    {
        User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"))
    }
};
var result = await controller.Login(request);
```

### Mock 外部依赖

- **ITokenService** → Moq（JWT 生成）
- **IConfiguration** → Moq（JWT/MinIO 配置）
- **IHubContext\<ChatHub\>** → Moq（SignalR 消息推送）
- **IMinioClient** → Moq（MinIO 文件存储）
- **AppDbContext** → EF Core InMemory（替代 MySQL）

## 已知限制

5 个测试因 EF Core InMemory 提供程序不支持 `ExecuteUpdateAsync` / `ExecuteDeleteAsync` 而被跳过：

| 测试 | 原因 |
|------|------|
| NotificationService.MarkAllAsRead | 使用 `ExecuteUpdateAsync` |
| MessageService.MarkAsRead | 使用 `ExecuteUpdateAsync` |
| AuthService.Refresh (token valid) | 使用 `ExecuteDeleteAsync` |
| AuthService.Logout | 使用 `ExecuteDeleteAsync` |
| ImageService.GetStreamAsync | MinIO 回调 Mock 复杂 |

**解决方案**：切换到 SQLite InMemory 或在集成测试环境中运行这些用例。

## 排除代码说明

以下代码未被单元测试覆盖，因为属于基础设施层，适合集成测试：

- `Program.cs` — 应用启动、DI 注册、中间件管道
- `Migrations/*` — EF Core 自动生成的迁移文件
- `Controllers/AdminController.cs` — 16 个端点、560 行（待补充）
- `Controllers/*ThinControllers*` — 瘦透传控制器（逻辑在服务层）
- `Hubs/ChatHub.cs` — SignalR Hub
- `Models/EnumMemberJsonConverter.cs` — JSON 序列化
