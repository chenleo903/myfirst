# CRM System

一个简洁的自用 CRM（客户关系管理）系统，采用前后端分离架构，支持 Docker 化一键启动。

## 功能特性

- ✅ **客户信息管理** - 创建、查看、更新、软删除客户
- ✅ **客户列表查询** - 支持筛选、搜索、分页、排序
- ✅ **互动记录管理** - 时间线形式跟踪客户沟通历史
- ✅ **乐观并发控制** - 使用 ETag 防止数据覆盖
- ✅ **统一 API 响应格式** - 标准化的成功/错误响应
- ✅ **全局异常处理** - 友好的错误信息
- ✅ **结构化日志** - 敏感信息自动脱敏
- ✅ **健康检查端点** - 监控系统状态
- ✅ **JWT 认证** - 可选的用户认证功能
- ✅ **Docker 化部署** - 一键启动完整系统

## 技术栈

### 后端
| 技术 | 版本 | 用途 |
|------|------|------|
| ASP.NET Core | 8.0 | Web API 框架 |
| PostgreSQL | 16 | 关系型数据库 |
| Entity Framework Core | 8.0 | ORM |
| FluentValidation | 11.3 | 请求验证 |
| Serilog | 8.0 | 结构化日志 |
| BCrypt.Net | 4.0 | 密码哈希 |
| Swashbuckle | 6.5 | Swagger API 文档 |

### 前端
| 技术 | 版本 | 用途 |
|------|------|------|
| React | 18 | UI 框架 |
| TypeScript | 5.x | 类型安全 |
| Vite | 5.x | 构建工具 |
| Ant Design | 5.x | UI 组件库 |
| React Query | 5.x | 服务器状态管理 |
| React Router | 6.x | 路由 |
| Axios | 1.x | HTTP 客户端 |

### 测试
| 技术 | 用途 |
|------|------|
| xUnit | 单元测试框架 |
| FsCheck | 属性测试 |
| Testcontainers | 集成测试 |

## 快速开始

### 方式一：Docker Compose（推荐）

最简单的方式是使用 Docker Compose 一键启动完整系统：

```bash
# 1. 克隆仓库
git clone <repository-url>
cd CrmSystem

# 2. 复制环境变量配置文件
cp .env.example .env

# 3. 启动所有服务
docker-compose up -d

# 4. 查看服务状态
docker-compose ps

# 5. 查看 API 日志
docker-compose logs -f api
```

服务启动后访问：
- **前端应用**: http://localhost:3000
- **后端 API**: http://localhost:8080
- **API 文档**: http://localhost:8080/swagger
- **健康检查**: http://localhost:8080/health

### 方式二：本地开发

#### 前置要求
- .NET 8 SDK
- PostgreSQL 16
- Node.js 18+

#### 后端启动

```bash
# 1. 进入后端目录
cd CrmSystem.Api

# 2. 配置数据库连接（编辑 appsettings.Development.json）

# 3. 运行数据库迁移
dotnet ef database update

# 4. 启动 API
dotnet run
```

API 将在 `https://localhost:5001` 运行

#### 前端启动

```bash
# 1. 进入前端目录
cd crm-frontend

# 2. 安装依赖
npm install

# 3. 启动开发服务器
npm run dev
```

前端将在 `http://localhost:5173` 运行

## 环境变量配置

### Docker Compose 环境变量

在 `.env` 文件中配置以下变量：

#### 数据库配置
| 变量名 | 说明 | 默认值 |
|--------|------|--------|
| `POSTGRES_USER` | PostgreSQL 用户名 | `postgres` |
| `POSTGRES_PASSWORD` | PostgreSQL 密码 | `postgres` |
| `POSTGRES_DB` | 数据库名 | `crm` |
| `DB_PORT` | 数据库外部端口 | `5432` |

#### API 配置
| 变量名 | 说明 | 默认值 |
|--------|------|--------|
| `API_PORT` | API 服务外部端口 | `8080` |
| `WEB_PORT` | 前端服务外部端口 | `3000` |
| `ASPNETCORE_ENVIRONMENT` | 运行环境 | `Production` |
| `AUTO_MIGRATE` | 启动时自动迁移数据库 | `true` |

#### 认证配置（可选）
| 变量名 | 说明 | 默认值 |
|--------|------|--------|
| `ENABLE_AUTH` | 是否启用 JWT 认证 | `false` |
| `ADMIN_USERNAME` | 初始管理员用户名 | - |
| `ADMIN_PASSWORD` | 初始管理员密码 | - |
| `JWT_SECRET` | JWT 签名密钥（至少32字符） | - |
| `JWT_EXPIRY_MINUTES` | JWT 令牌过期时间（分钟） | `60` |

#### CORS 配置
| 变量名 | 说明 | 默认值 |
|--------|------|--------|
| `CORS_ORIGIN_1` | 允许的 CORS 来源 1 | `http://localhost:3000` |
| `CORS_ORIGIN_2` | 允许的 CORS 来源 2 | `http://localhost:5173` |

### 启用认证示例

```bash
# .env 文件
ENABLE_AUTH=true
ADMIN_USERNAME=admin
ADMIN_PASSWORD=YourSecurePassword123!
JWT_SECRET=your-super-secret-jwt-key-at-least-32-characters-long
JWT_EXPIRY_MINUTES=60
```

## API 文档

### Swagger UI

启动应用后访问 Swagger UI 查看完整的 API 文档：
- **本地开发**: https://localhost:5001/swagger
- **Docker 环境**: http://localhost:8080/swagger

### API 端点概览

#### 客户管理
| 方法 | 端点 | 说明 |
|------|------|------|
| GET | `/api/customers` | 获取客户列表（支持筛选、搜索、分页） |
| GET | `/api/customers/{id}` | 获取客户详情 |
| POST | `/api/customers` | 创建客户 |
| PUT | `/api/customers/{id}` | 更新客户 |
| DELETE | `/api/customers/{id}` | 软删除客户 |

#### 互动记录
| 方法 | 端点 | 说明 |
|------|------|------|
| GET | `/api/customers/{customerId}/interactions` | 获取客户互动记录列表 |
| POST | `/api/customers/{customerId}/interactions` | 创建互动记录 |
| GET | `/api/interactions/{id}` | 获取互动记录详情 |
| PUT | `/api/interactions/{id}` | 更新互动记录 |
| DELETE | `/api/interactions/{id}` | 删除互动记录 |

#### 认证（可选）
| 方法 | 端点 | 说明 |
|------|------|------|
| POST | `/api/auth/login` | 用户登录 |

#### 系统
| 方法 | 端点 | 说明 |
|------|------|------|
| GET | `/health` | 健康检查 |

### 统一响应格式

#### 成功响应
```json
{
  "success": true,
  "data": { ... },
  "errors": []
}
```

#### 失败响应
```json
{
  "success": false,
  "data": null,
  "errors": [
    { "field": "companyName", "message": "Company name is required" }
  ]
}
```

### 并发控制

系统使用 ETag 实现乐观并发控制：

1. **获取资源时**：响应头包含 `ETag: W/"1702034400123"`
2. **更新/删除时**：请求头添加 `If-Match: W/"1702034400123"`
3. **冲突时**：返回 409 状态码和当前版本信息

## 项目结构

```
CrmSystem/
├── CrmSystem.Api/              # 后端 API 项目
│   ├── Controllers/            # API 控制器
│   ├── Services/               # 业务逻辑层
│   ├── Repositories/           # 数据访问层
│   ├── Models/                 # 实体模型
│   ├── DTOs/                   # 数据传输对象
│   │   └── Validators/         # FluentValidation 验证器
│   ├── Middleware/             # 中间件（异常处理、日志、认证）
│   ├── Data/                   # DbContext
│   ├── Exceptions/             # 自定义异常
│   ├── Helpers/                # 工具类
│   ├── Migrations/             # EF Core 迁移
│   └── Program.cs              # 应用入口
├── CrmSystem.Tests/            # 测试项目
│   ├── UnitTests/              # 单元测试
│   ├── PropertyTests/          # 属性测试 (FsCheck)
│   ├── IntegrationTests/       # 集成测试
│   └── Generators/             # FsCheck 生成器
├── crm-frontend/               # 前端项目
│   ├── src/
│   │   ├── api/                # API 客户端
│   │   ├── components/         # React 组件
│   │   ├── hooks/              # 自定义 Hooks
│   │   ├── pages/              # 页面组件
│   │   └── types/              # TypeScript 类型定义
│   └── ...
├── .kiro/specs/crm-system/     # 规范文档
│   ├── requirements.md         # 需求文档
│   ├── design.md               # 设计文档
│   └── tasks.md                # 任务列表
├── docker-compose.yml          # Docker Compose 配置
├── .env.example                # 环境变量示例
└── README.md                   # 项目说明
```

## 测试

### 运行所有测试

```bash
cd CrmSystem.Tests
dotnet test
```

### 运行特定类型测试

```bash
# 单元测试
dotnet test --filter "FullyQualifiedName~UnitTests"

# 属性测试
dotnet test --filter "FullyQualifiedName~PropertyTests"

# 集成测试（需要 Docker）
dotnet test --filter "FullyQualifiedName~IntegrationTests"
```

### 测试覆盖率

```bash
dotnet test --collect:"XPlat Code Coverage"
```

## 常用命令

### Docker Compose

```bash
# 启动所有服务
docker-compose up -d

# 停止所有服务
docker-compose down

# 停止并删除数据卷
docker-compose down -v

# 查看日志
docker-compose logs -f api

# 重新构建镜像
docker-compose build --no-cache

# 仅启动数据库
docker-compose up -d db
```

### 数据库迁移

```bash
cd CrmSystem.Api

# 创建新迁移
dotnet ef migrations add <MigrationName>

# 应用迁移
dotnet ef database update

# 回滚到指定迁移
dotnet ef database update <MigrationName>

# 生成 SQL 脚本
dotnet ef migrations script
```

## 开发指南

详细的开发指南请参考：
- [需求文档](.kiro/specs/crm-system/requirements.md) - 系统需求和验收标准
- [设计文档](.kiro/specs/crm-system/design.md) - 架构设计和技术细节
- [任务列表](.kiro/specs/crm-system/tasks.md) - 实现任务清单

## 故障排除

### 数据库连接失败

1. 确认 PostgreSQL 服务正在运行
2. 检查连接字符串配置
3. 确认数据库用户权限

### Docker 容器启动失败

```bash
# 查看容器日志
docker-compose logs api

# 检查容器状态
docker-compose ps

# 重新构建并启动
docker-compose down
docker-compose build --no-cache
docker-compose up -d
```

### 迁移失败

```bash
# 检查数据库连接
dotnet ef database update --verbose

# 如果迁移冲突，可以重置
dotnet ef database drop
dotnet ef database update
```

## 许可证

MIT License

