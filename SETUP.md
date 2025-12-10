# CRM System - 安装指南

本文档提供 CRM 系统的详细安装和配置说明。

## 前置要求

### 必需软件

| 软件 | 版本 | 用途 |
|------|------|------|
| .NET SDK | 8.0+ | 后端开发和运行 |
| PostgreSQL | 16+ | 数据库 |
| Node.js | 18+ | 前端开发 |
| Docker | 最新版 | 容器化部署（可选） |

### 安装 .NET SDK

**macOS (Homebrew)**
```bash
brew install dotnet@8
```

**Windows**
从 [.NET 下载页面](https://dotnet.microsoft.com/download/dotnet/8.0) 下载安装程序。

**验证安装**
```bash
dotnet --version
```

### 安装 PostgreSQL

**使用 Docker（推荐）**
```bash
docker run -d \
  --name crm-postgres \
  -e POSTGRES_PASSWORD=postgres \
  -e POSTGRES_DB=crm \
  -p 5432:5432 \
  postgres:16-alpine
```

**macOS (Homebrew)**
```bash
brew install postgresql@16
brew services start postgresql@16
```

### 安装 Node.js

**macOS (Homebrew)**
```bash
brew install node@18
```

**验证安装**
```bash
node --version
npm --version
```

## 快速开始

### 方式一：Docker Compose（推荐）

最简单的方式是使用 Docker Compose 一键启动：

```bash
# 1. 克隆仓库
git clone <repository-url>
cd CrmSystem

# 2. 复制环境变量配置
cp .env.example .env

# 3. 启动所有服务
docker-compose up -d

# 4. 查看服务状态
docker-compose ps
```

服务启动后：
- 前端: http://localhost:3000
- API: http://localhost:8080
- API 文档: http://localhost:8080/swagger
- 健康检查: http://localhost:8080/health

### 方式二：本地开发

#### 1. 克隆仓库

```bash
git clone <repository-url>
cd CrmSystem
```

#### 2. 配置数据库

确保 PostgreSQL 正在运行，然后创建数据库：

```bash
# 使用 psql 连接
psql -U postgres

# 创建数据库
CREATE DATABASE crm;
\q
```

#### 3. 配置后端

编辑 `CrmSystem.Api/appsettings.Development.json`：

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=crm;Username=postgres;Password=your_password"
  }
}
```

#### 4. 运行数据库迁移

```bash
cd CrmSystem.Api
dotnet ef database update
```

#### 5. 启动后端

```bash
dotnet run
```

API 将在 `https://localhost:5001` 运行。

#### 6. 启动前端

```bash
cd crm-frontend
npm install
npm run dev
```

前端将在 `http://localhost:5173` 运行。

## 环境变量配置

### Docker Compose 环境变量

在 `.env` 文件中配置：

```bash
# 数据库配置
POSTGRES_USER=postgres
POSTGRES_PASSWORD=postgres
POSTGRES_DB=crm
DB_PORT=5432

# API 配置
API_PORT=8080
WEB_PORT=3000
ASPNETCORE_ENVIRONMENT=Production

# 迁移设置
AUTO_MIGRATE=true

# 认证设置（可选）
ENABLE_AUTH=false
ADMIN_USERNAME=admin
ADMIN_PASSWORD=changeme
JWT_SECRET=your-super-secret-jwt-key-at-least-32-characters-long
JWT_EXPIRY_MINUTES=60

# CORS 设置
CORS_ORIGIN_1=http://localhost:3000
CORS_ORIGIN_2=http://localhost:5173
```

### 启用认证

如果需要启用 JWT 认证：

```bash
ENABLE_AUTH=true
ADMIN_USERNAME=admin
ADMIN_PASSWORD=YourSecurePassword123!
JWT_SECRET=your-super-secret-jwt-key-at-least-32-characters-long
```

## 数据库迁移

### 自动迁移

设置 `AUTO_MIGRATE=true` 后，API 启动时会自动执行迁移。

### 手动迁移

```bash
cd CrmSystem.Api

# 应用迁移
dotnet ef database update

# 创建新迁移
dotnet ef migrations add <MigrationName>

# 回滚迁移
dotnet ef database update <PreviousMigrationName>

# 生成 SQL 脚本
dotnet ef migrations script
```

## 验证安装

### 检查 API 健康状态

```bash
curl http://localhost:8080/health
```

预期响应：
```json
{
  "status": "Healthy",
  "checks": {
    "database": "Healthy"
  }
}
```

### 访问 API 文档

打开浏览器访问：
- 本地开发: https://localhost:5001/swagger
- Docker: http://localhost:8080/swagger

## 常见问题

### 数据库连接失败

1. 确认 PostgreSQL 服务正在运行
2. 检查连接字符串中的用户名和密码
3. 确认数据库已创建

### Docker 容器启动失败

```bash
# 查看日志
docker-compose logs api

# 重新构建
docker-compose down
docker-compose build --no-cache
docker-compose up -d
```

### 迁移失败

```bash
# 查看详细错误
dotnet ef database update --verbose

# 如果需要重置数据库
dotnet ef database drop
dotnet ef database update
```

### 端口冲突

修改 `.env` 文件中的端口配置：

```bash
API_PORT=8081
WEB_PORT=3001
DB_PORT=5433
```

## 下一步

- 阅读 [README.md](README.md) 了解项目概述
- 阅读 [DEVELOPMENT.md](DEVELOPMENT.md) 了解开发指南
- 查看 [API 文档](http://localhost:8080/swagger) 了解 API 接口
