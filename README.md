# CRM System

一个简洁的自用 CRM（客户关系管理）系统，采用前后端分离架构。

## 技术栈

### 后端
- **框架**: ASP.NET Core 8 Web API
- **数据库**: PostgreSQL 16
- **ORM**: Entity Framework Core 8
- **验证**: FluentValidation
- **日志**: Serilog
- **认证**: JWT Bearer (可选)
- **密码哈希**: BCrypt.Net
- **测试**: xUnit, FsCheck (属性测试), Testcontainers

### 前端
- **框架**: React 18 + TypeScript
- **构建工具**: Vite
- **UI 库**: Ant Design
- **状态管理**: React Query
- **路由**: React Router
- **HTTP 客户端**: Axios
- **表单**: React Hook Form + Zod

## 项目结构

```
CrmSystem/
├── CrmSystem.Api/              # 后端 API 项目
│   ├── Controllers/            # API 控制器
│   ├── Services/               # 业务逻辑层
│   ├── Repositories/           # 数据访问层
│   ├── Models/                 # 实体模型
│   ├── DTOs/                   # 数据传输对象
│   ├── Middleware/             # 中间件
│   ├── Data/                   # DbContext 和数据库配置
│   ├── Exceptions/             # 自定义异常
│   ├── Helpers/                # 工具类
│   └── Program.cs              # 应用入口
├── CrmSystem.Tests/            # 测试项目
│   ├── UnitTests/              # 单元测试
│   ├── PropertyTests/          # 属性测试 (FsCheck)
│   ├── IntegrationTests/       # 集成测试
│   └── Generators/             # FsCheck 生成器
├── .kiro/specs/crm-system/     # 规范文档
│   ├── requirements.md         # 需求文档
│   ├── design.md               # 设计文档
│   └── tasks.md                # 任务列表
└── README.md                   # 项目说明
```

## 前置要求

- .NET 8 SDK
- PostgreSQL 16
- Docker & Docker Compose (用于容器化部署)
- Node.js 18+ (用于前端开发)

## 快速开始

### 本地开发

1. **克隆仓库**
   ```bash
   git clone <repository-url>
   cd CrmSystem
   ```

2. **配置数据库连接**
   
   编辑 `CrmSystem.Api/appsettings.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=localhost;Port=5432;Database=crm;Username=postgres;Password=your_password"
     }
   }
   ```

3. **运行数据库迁移**
   ```bash
   cd CrmSystem.Api
   dotnet ef database update
   ```

4. **启动后端 API**
   ```bash
   dotnet run
   ```
   
   API 将在 `https://localhost:5001` 运行

5. **运行测试**
   ```bash
   cd ../CrmSystem.Tests
   dotnet test
   ```

### Docker 部署

使用 Docker Compose 一键启动完整系统：

1. **复制环境变量配置文件**
   ```bash
   cp .env.example .env
   ```

2. **根据需要修改 `.env` 文件中的配置**

3. **启动服务**
   ```bash
   docker-compose up -d
   ```

4. **查看日志**
   ```bash
   docker-compose logs -f api
   ```

5. **停止服务**
   ```bash
   docker-compose down
   ```

6. **停止服务并删除数据卷**
   ```bash
   docker-compose down -v
   ```

服务将在以下端口运行：
- 后端 API: `http://localhost:8080`
- PostgreSQL: `localhost:5432`

健康检查端点：`http://localhost:8080/health`

## 环境变量

### Docker Compose 环境变量

| 变量名 | 说明 | 默认值 | 必填 |
|--------|------|--------|------|
| `POSTGRES_USER` | PostgreSQL 用户名 | `postgres` | 否 |
| `POSTGRES_PASSWORD` | PostgreSQL 密码 | `postgres` | 否 |
| `POSTGRES_DB` | PostgreSQL 数据库名 | `crm` | 否 |
| `DB_PORT` | PostgreSQL 外部端口 | `5432` | 否 |
| `API_PORT` | API 服务外部端口 | `8080` | 否 |
| `AUTO_MIGRATE` | 启动时自动执行数据库迁移 | `true` | 否 |
| `ENABLE_AUTH` | 是否启用 JWT 认证 | `false` | 否 |
| `ADMIN_USERNAME` | 初始管理员用户名 | - | 启用认证时必填 |
| `ADMIN_PASSWORD` | 初始管理员密码 | - | 启用认证时必填 |
| `JWT_SECRET` | JWT 签名密钥（至少32字符） | - | 启用认证时必填 |
| `JWT_EXPIRY_MINUTES` | JWT 令牌过期时间（分钟） | `60` | 否 |
| `CORS_ORIGIN_1` | 允许的 CORS 来源 1 | `http://localhost:3000` | 否 |
| `CORS_ORIGIN_2` | 允许的 CORS 来源 2 | `http://localhost:5173` | 否 |
| `ASPNETCORE_ENVIRONMENT` | ASP.NET Core 环境 | `Production` | 否 |

## 核心功能

- ✅ 客户信息管理（CRUD）
- ✅ 客户列表查询、筛选、搜索
- ✅ 互动记录管理（时间线）
- ✅ 软删除支持
- ✅ 乐观并发控制（ETag）
- ✅ 统一 API 响应格式
- ✅ 全局异常处理
- ✅ 结构化日志（敏感信息脱敏）
- ✅ 健康检查端点
- ✅ JWT 认证（可选）
- ✅ Docker 化部署

## API 文档

启动应用后访问 Swagger UI：
- 开发环境: `https://localhost:5001/swagger`
- Docker 环境: `http://localhost:5000/swagger`

## 测试

项目包含三种类型的测试：

1. **单元测试**: 测试独立的业务逻辑
2. **属性测试**: 使用 FsCheck 验证正确性属性
3. **集成测试**: 使用 Testcontainers 测试完整流程

运行所有测试：
```bash
dotnet test
```

运行特定类型的测试：
```bash
# 仅运行单元测试
dotnet test --filter Category=Unit

# 仅运行属性测试
dotnet test --filter Category=Property

# 仅运行集成测试
dotnet test --filter Category=Integration
```

## 开发指南

详细的开发指南请参考：
- [需求文档](.kiro/specs/crm-system/requirements.md)
- [设计文档](.kiro/specs/crm-system/design.md)
- [任务列表](.kiro/specs/crm-system/tasks.md)

## 许可证

MIT
