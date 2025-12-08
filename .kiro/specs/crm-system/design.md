# CRM 系统设计文档

## 概述

本文档描述了一个简洁的自用 CRM 系统的技术设计，该系统采用前后端分离架构，使用 C# ASP.NET Core 8 作为后端，React + TypeScript 作为前端，PostgreSQL 作为数据库。系统支持 Docker 化部署，提供客户管理、互动记录跟踪等核心功能。

## 架构

### 整体架构

系统采用三层架构模式：

```
┌─────────────────────────────────────────┐
│         前端层 (React + TS)              │
│  - 客户列表与详情页面                     │
│  - 互动时间线组件                         │
│  - 筛选与搜索功能                         │
└──────────────┬──────────────────────────┘
               │ HTTP/REST API
┌──────────────┴──────────────────────────┐
│         API 层 (ASP.NET Core)           │
│  - Controllers (路由与请求处理)          │
│  - Middleware (认证/日志/异常处理)       │
│  - 统一响应格式                          │
└──────────────┬──────────────────────────┘
               │
┌──────────────┴──────────────────────────┐
│         业务层 (Services)                │
│  - CustomerService                      │
│  - InteractionService                   │
│  - AuthService (可选)                   │
│  - 业务逻辑与验证                        │
└──────────────┬──────────────────────────┘
               │
┌──────────────┴──────────────────────────┐
│      数据访问层 (EF Core + Repositories) │
│  - DbContext                            │
│  - Entity Models                        │
│  - 查询优化                              │
└──────────────┬──────────────────────────┘
               │
┌──────────────┴──────────────────────────┐
│         数据库层 (PostgreSQL)            │
│  - customers 表                         │
│  - interactions 表                      │
│  - users 表 (可选)                      │
└─────────────────────────────────────────┘
```

### 容器化架构

使用 Docker Compose 编排三个服务：

- **db**: PostgreSQL 16 数据库
- **api**: ASP.NET Core Web API
- **web**: React 前端应用（Nginx 托管）

服务间通过 Docker 内部网络通信，数据库使用持久化卷存储。

## 组件与接口

### 后端组件

#### 1. Controllers

薄控制器层，负责路由、参数绑定和响应格式化：

- `CustomersController`: 客户 CRUD 和列表查询
- `InteractionsController`: 互动记录 CRUD
- `AuthController`: 登录认证（可选）
- `HealthController`: 健康检查端点

#### 2. Services

业务逻辑层，包含核心业务规则：

- `CustomerService`: 
  - 客户创建、更新、软删除
  - 列表查询与筛选
  - 唯一性验证
  - LastInteractionAt 维护

- `InteractionService`:
  - 互动记录 CRUD
  - 时间线查询
  - 客户关联验证
  - LastInteractionAt 更新触发

- `AuthService` (可选):
  - JWT 令牌生成与验证
  - 密码哈希与验证
  - 初始管理员创建

#### 3. Repositories

数据访问抽象层：

- `ICustomerRepository` / `CustomerRepository`
- `IInteractionRepository` / `InteractionRepository`
- `IUserRepository` / `UserRepository` (可选)

#### 4. Middleware

- `ExceptionHandlingMiddleware`: 全局异常捕获与统一错误响应
- `RequestLoggingMiddleware`: 请求日志记录与脱敏
- `AuthenticationMiddleware`: JWT 验证（可选）
- `CorsMiddleware`: 跨域配置

### 前端组件

#### 页面组件

- `CustomerListPage`: 客户列表、筛选、搜索、分页
- `CustomerDetailPage`: 客户详情与时间线
- `CustomerFormPage`: 客户创建/编辑表单
- `LoginPage`: 登录页面（可选）

#### 功能组件

- `CustomerTable`: 客户列表表格
- `CustomerFilters`: 筛选器组件
- `InteractionTimeline`: 时间线展示
- `InteractionForm`: 互动记录表单
- `Pagination`: 分页组件

#### 状态管理

使用 React Query 管理服务器状态：

- 查询缓存与自动重新验证
- 乐观更新
- 错误处理与重试
- 分页与无限滚动支持

### API 接口设计

#### 统一响应格式

```typescript
// 成功响应
{
  success: true,
  data: T,
  errors: []
}

// 失败响应
{
  success: false,
  data: null,
  errors: [
    { field?: string, message: string }
  ]
}
```

#### 端点列表

**客户管理**

- `GET /api/customers?page=1&pageSize=20&status=Lead&keyword=xxx&sortBy=LastInteractionAt&sortOrder=desc`
  - 响应: `{ success, data: { items: Customer[], total: number }, errors }`
  
- `GET /api/customers/{id}`
  - 响应: `{ success, data: Customer, errors }`
  
- `POST /api/customers`
  - 请求体: `{ companyName, contactName, phone?, email?, ... }`
  - 响应: `201 Created` + `Location` 头 + `{ success, data: Customer, errors }`
  
- `PUT /api/customers/{id}`
  - 请求头: `If-Match: W/"1702034400123"` (可选，用于并发控制)
  - 请求体: `{ companyName, contactName, ... }`
  - 响应: `{ success, data: Customer, errors }` + `ETag: W/"1702034400456"`
  
- `DELETE /api/customers/{id}`
  - 请求头: `If-Match: W/"1702034400123"` (可选，用于并发控制)
  - 响应: `204 No Content`

**互动记录**

- `GET /api/customers/{customerId}/interactions`
  - 响应: `{ success, data: Interaction[], errors }`
  
- `POST /api/customers/{customerId}/interactions`
  - 请求体: `{ happenedAt, channel, title, summary?, ... }`
  - 响应: `201 Created` + `Location` 头
  
- `PUT /api/interactions/{id}`
  - 请求头: `If-Match: W/"1702034400123"` (可选，用于并发控制)
  - 响应: `{ success, data: Interaction, errors }` + `ETag: W/"1702034400456"`
  
- `DELETE /api/interactions/{id}`
  - 请求头: `If-Match: W/"1702034400123"` (可选，用于并发控制)
  - 响应: `204 No Content`

**认证（可选）**

- `POST /api/auth/login`
  - 请求体: `{ username, password }`
  - 响应: `{ success, data: { token, expiresAt }, errors }`

**健康检查**

- `GET /health`
  - 响应: `{ status: "Healthy", checks: { database: "Healthy" } }`

## 数据模型

### 实体模型

#### Customer 实体

```csharp
public class Customer
{
    public Guid Id { get; set; }
    public string CompanyName { get; set; } // 必填, 最大 200 字符
    public string ContactName { get; set; } // 必填, 最大 200 字符
    public string? Wechat { get; set; } // 最大 100 字符
    public string? Phone { get; set; } // 最大 50 字符
    public string? Email { get; set; } // 最大 255 字符
    public string? Industry { get; set; } // 最大 100 字符
    public CustomerSource? Source { get; set; } // 枚举
    public CustomerStatus Status { get; set; } // 枚举
    public string[]? Tags { get; set; } // PostgreSQL text[]，每个标签最大 50 字符
    public int Score { get; set; } // 0-100
    public DateTimeOffset? LastInteractionAt { get; set; } // UTC
    public DateTimeOffset CreatedAt { get; set; } // UTC
    public DateTimeOffset UpdatedAt { get; set; } // UTC，并发令牌
    public bool IsDeleted { get; set; }
    
    // 导航属性
    public ICollection<Interaction> Interactions { get; set; }
}

public enum CustomerStatus
{
    Lead,
    Contacted,
    NeedsAnalyzed,
    Quoted,
    Negotiating,
    Won,
    Lost
}

public enum CustomerSource
{
    Website,
    Referral,
    SocialMedia,
    Event,
    DirectContact,
    Other
}
```

#### Interaction 实体

```csharp
public class Interaction
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public DateTimeOffset HappenedAt { get; set; } // 必填, UTC
    public InteractionChannel Channel { get; set; } // 必填, 枚举
    public CustomerStatus? Stage { get; set; } // 可选, 记录互动时客户所处阶段
    public string Title { get; set; } // 必填, 最大 200 字符
    public string? Summary { get; set; } // 最大 2000 字符
    public string? RawContent { get; set; } // 最大 10000 字符
    public string? NextAction { get; set; } // 最大 500 字符
    public List<AttachmentInfo>? Attachments { get; set; } // 存储为 JSONB
    public DateTimeOffset CreatedAt { get; set; } // UTC
    public DateTimeOffset UpdatedAt { get; set; } // UTC，并发令牌
    
    // 导航属性
    public Customer Customer { get; set; }
}

public class AttachmentInfo
{
    public string Url { get; set; } // 最大 500 字符
    public string? FileName { get; set; } // 最大 255 字符
    public long? FileSize { get; set; } // 字节数
}

public enum InteractionChannel
{
    Phone,
    Wechat,
    Email,
    Offline,
    Other
}
```

#### User 实体（可选）

```csharp
public class User
{
    public Guid Id { get; set; }
    public string UserName { get; set; } // 唯一, 最大 100 字符
    public string PasswordHash { get; set; } // BCrypt 哈希, 最大 255 字符
    public string Role { get; set; } // 最大 50 字符
    public DateTimeOffset CreatedAt { get; set; } // UTC
    public DateTimeOffset UpdatedAt { get; set; } // UTC
    public DateTimeOffset? LastLoginAt { get; set; } // UTC
}
```

### 数据库架构

#### 表结构

**customers 表**

```sql
CREATE TABLE customers (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_name VARCHAR(200) NOT NULL,
    contact_name VARCHAR(200) NOT NULL,
    wechat VARCHAR(100),
    phone VARCHAR(50),
    email VARCHAR(255),
    industry VARCHAR(100),
    source VARCHAR(50),
    status VARCHAR(50) NOT NULL,
    tags TEXT[],
    score INTEGER DEFAULT 0 CHECK (score >= 0 AND score <= 100),
    last_interaction_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE
);

-- 唯一性约束（仅对未删除记录）
CREATE UNIQUE INDEX uq_customer_company_contact 
ON customers(company_name, contact_name) 
WHERE is_deleted = FALSE;

-- 查询优化索引
CREATE INDEX idx_customers_status ON customers(status) WHERE is_deleted = FALSE;
CREATE INDEX idx_customers_industry ON customers(industry) WHERE is_deleted = FALSE;
CREATE INDEX idx_customers_source ON customers(source) WHERE is_deleted = FALSE;
CREATE INDEX idx_customers_last_interaction ON customers(last_interaction_at DESC NULLS LAST) WHERE is_deleted = FALSE;
```

**interactions 表**

```sql
CREATE TABLE interactions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    customer_id UUID NOT NULL REFERENCES customers(id),
    happened_at TIMESTAMPTZ NOT NULL,
    channel VARCHAR(50) NOT NULL,
    stage VARCHAR(50),
    title VARCHAR(200) NOT NULL,
    summary VARCHAR(2000),
    raw_content VARCHAR(10000),
    next_action VARCHAR(500),
    attachments JSONB,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_interactions_customer_happened ON interactions(customer_id, happened_at DESC);
```

**users 表（可选）**

```sql
CREATE TABLE users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_name VARCHAR(100) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    role VARCHAR(50) NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    last_login_at TIMESTAMPTZ
);
```

### DTO 模型

#### 请求 DTO

```csharp
public class CreateCustomerRequest
{
    [Required, MaxLength(200)]
    public string CompanyName { get; set; }
    
    [Required, MaxLength(200)]
    public string ContactName { get; set; }
    
    [MaxLength(100)]
    public string? Wechat { get; set; }
    
    [MaxLength(50)]
    public string? Phone { get; set; }
    
    [EmailAddress, MaxLength(255)]
    public string? Email { get; set; }
    
    [MaxLength(100)]
    public string? Industry { get; set; }
    
    [EnumDataType(typeof(CustomerSource))]
    public CustomerSource? Source { get; set; }
    
    [EnumDataType(typeof(CustomerStatus))]
    public CustomerStatus Status { get; set; } = CustomerStatus.Lead;
    
    public string[]? Tags { get; set; }
    
    [Range(0, 100)]
    public int Score { get; set; } = 0;
}

public class CreateInteractionRequest
{
    [Required]
    public DateTimeOffset HappenedAt { get; set; }
    
    [Required, EnumDataType(typeof(InteractionChannel))]
    public InteractionChannel Channel { get; set; }
    
    [EnumDataType(typeof(CustomerStatus))]
    public CustomerStatus? Stage { get; set; }
    
    [Required, MaxLength(200)]
    public string Title { get; set; }
    
    [MaxLength(2000)]
    public string? Summary { get; set; }
    
    [MaxLength(10000)]
    public string? RawContent { get; set; }
    
    [MaxLength(500)]
    public string? NextAction { get; set; }
    
    public List<AttachmentInfo>? Attachments { get; set; }
}
```

#### 响应 DTO

```csharp
public class CustomerResponse
{
    public Guid Id { get; set; }
    public string CompanyName { get; set; }
    public string ContactName { get; set; }
    public string? Wechat { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Industry { get; set; }
    public CustomerSource? Source { get; set; }
    public CustomerStatus Status { get; set; }
    public string[]? Tags { get; set; }
    public int Score { get; set; }
    public DateTimeOffset? LastInteractionAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public class InteractionResponse
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public DateTimeOffset HappenedAt { get; set; }
    public InteractionChannel Channel { get; set; }
    public CustomerStatus? Stage { get; set; }
    public string Title { get; set; }
    public string? Summary { get; set; }
    public string? RawContent { get; set; }
    public string? NextAction { get; set; }
    public List<AttachmentInfo>? Attachments { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public class PagedResponse<T>
{
    public List<T> Items { get; set; }
    public int Total { get; set; }
}
```


## 正确性属性

*属性是一个特征或行为，应该在系统的所有有效执行中保持为真。属性作为人类可读规范和机器可验证正确性保证之间的桥梁。*

### 属性 1：客户创建往返一致性

*对于任何*有效的客户数据（包含必填字段 CompanyName 和 ContactName），创建客户后通过返回的 ID 查询应该返回相同的数据（除了系统生成的字段如 Id、CreatedAt、UpdatedAt）

**验证：需求 1.1**

### 属性 2：客户更新反映性

*对于任何*现有客户和任何有效的更新数据，更新后查询该客户应该返回更新后的数据，且 UpdatedAt 时间戳应该晚于更新前的值

**验证：需求 1.3**

### 属性 3：软删除不变性

*对于任何*具有互动记录的客户，软删除该客户后，该客户的互动记录数量应该保持不变（物理上仍然存在）

**验证：需求 1.5**

### 属性 4：软删除过滤性

*对于任何*客户列表查询，返回的结果中不应包含任何 IsDeleted 为 true 的客户

**验证：需求 1.6**

### 属性 5：必填字段验证

*对于任何*缺少必填字段（CompanyName 或 ContactName）的客户创建请求，系统应该返回 400 状态码和包含字段名称的验证错误

**验证：需求 1.7**

### 属性 6：列表排序一致性

*对于任何*客户列表查询（未指定排序参数），返回的客户应该按 LastInteractionAt 降序排列（null 值排在最后）

**验证：需求 2.2**

### 属性 7：自定义排序正确性

*对于任何*指定 sortBy 和 sortOrder 参数的客户列表查询，返回的结果应该按指定字段和方向排序

**验证：需求 2.3**

### 属性 8：状态筛选完整性

*对于任何*指定状态筛选条件的客户列表查询，返回的所有客户的 Status 字段都应该等于指定的状态值

**验证：需求 2.4**

### 属性 9：行业筛选完整性

*对于任何*指定行业筛选条件的客户列表查询，返回的所有客户的 Industry 字段都应该等于指定的行业值

**验证：需求 2.5**

### 属性 10：来源筛选完整性

*对于任何*指定来源筛选条件的客户列表查询，返回的所有客户的 Source 字段都应该等于指定的来源值

**验证：需求 2.6**

### 属性 11：关键词搜索包含性

*对于任何*提供关键词的客户列表查询，返回的所有客户的 CompanyName 或 ContactName 字段都应该包含该关键词（不区分大小写）

**验证：需求 2.7**

### 属性 12：分页响应格式

*对于任何*分页查询请求，响应的 data 对象应该包含 items 数组和 total 字段，且 total 应该表示符合条件的总记录数

**验证：需求 2.9**

### 属性 13：互动创建往返一致性

*对于任何*有效的互动记录数据（包含必填字段 HappenedAt、Channel、Title）和存在的客户 ID，创建互动后通过返回的 ID 查询应该返回相同的数据

**验证：需求 3.1**

### 属性 14：LastInteractionAt 同步性

*对于任何*客户，创建新的互动记录后，该客户的 LastInteractionAt 字段应该等于新互动的 HappenedAt 值

**验证：需求 3.2**

### 属性 15：互动列表排序一致性

*对于任何*客户的互动记录列表查询，返回的互动记录应该按 HappenedAt 降序排列

**验证：需求 3.3**

### 属性 16：互动更新反映性

*对于任何*现有互动记录和任何有效的更新数据，更新后查询该互动应该返回更新后的数据，且 UpdatedAt 时间戳应该晚于更新前的值

**验证：需求 3.4**

### 属性 17：互动删除完整性

*对于任何*互动记录，物理删除后通过其 ID 查询应该返回 404 状态码

**验证：需求 3.5**


### 属性 18：LastInteractionAt 重新计算正确性

*对于任何*客户，删除一条互动记录后，该客户的 LastInteractionAt 应该等于剩余互动记录中最新的 HappenedAt 值，如果没有剩余互动则应该为 null

**验证：需求 3.6**

### 属性 19：互动必填字段验证

*对于任何*缺少必填字段（HappenedAt、Channel 或 Title）的互动创建请求，系统应该返回 400 状态码和包含字段名称的验证错误

**验证：需求 3.7**

### 属性 20：唯一性约束强制性

*对于任何*已存在的未删除客户，尝试创建具有相同 CompanyName 和 ContactName 的新客户应该返回 409 状态码和唯一性约束错误

**验证：需求 4.1**

### 属性 21：时间戳格式一致性

*对于任何*包含时间戳字段的 API 响应，所有时间戳都应该使用 ISO 8601 格式（例如：2024-12-08T10:30:00.123Z）

**验证：需求 4.2, 4.3**

### 属性 22：成功响应格式一致性

*对于任何*返回 2xx 状态码的 API 请求，响应 JSON 应该包含 success 字段为 true、data 字段包含数据、errors 字段为空数组

**验证：需求 6.1**

### 属性 23：失败响应格式一致性

*对于任何*返回 4xx 或 5xx 状态码的 API 请求，响应 JSON 应该包含 success 字段为 false、data 字段为 null、errors 字段为包含错误信息的数组

**验证：需求 6.2**

### 属性 24：乐观并发控制有效性

*对于任何*资源更新请求，如果提供的 If-Match 头中的 UpdatedAt 值与当前资源的 UpdatedAt 不匹配，系统应该返回 409 状态码和当前 UpdatedAt 值

**验证：需求 10.3, 10.4**

### 属性 25：版本控制删除保护

*对于任何*资源删除请求，如果提供的 If-Match 头中的 UpdatedAt 值与当前资源的 UpdatedAt 不匹配，系统应该返回 409 状态码并且资源不应该被删除

**验证：需求 10.5**

## 错误处理

### 错误分类

系统错误分为以下几类：

1. **验证错误 (400 Bad Request)**
   - 缺少必填字段
   - 字段格式不正确（如 Email 格式）
   - 字段长度超限
   - 枚举值无效
   - 数值超出范围（如 Score 不在 0-100）

2. **认证错误 (401 Unauthorized)**
   - 缺少 JWT 令牌
   - JWT 令牌无效或过期
   - 用户名或密码错误

3. **资源不存在 (404 Not Found)**
   - 请求的客户 ID 不存在或已删除
   - 请求的互动记录 ID 不存在

4. **冲突错误 (409 Conflict)**
   - 唯一性约束冲突（重复的公司名+联系人名）
   - 并发更新冲突（版本不匹配）

5. **服务器错误 (500 Internal Server Error)**
   - 数据库连接失败
   - 未预期的异常

### 错误响应格式

所有错误响应遵循统一格式：

```json
{
  "success": false,
  "data": null,
  "errors": [
    {
      "field": "companyName",  // 可选，仅验证错误时提供
      "message": "Company name is required"
    }
  ]
}
```

### 异常处理策略

1. **全局异常处理中间件**
   - 捕获所有未处理的异常
   - 记录详细错误日志（包含堆栈跟踪）
   - 返回通用 500 错误响应（不暴露内部细节）

2. **业务异常**
   - 定义自定义异常类型（ValidationException、NotFoundException、ConflictException）
   - 在 Service 层抛出业务异常
   - 在中间件中转换为相应的 HTTP 状态码

3. **数据库异常**
   - 捕获 EF Core 异常
   - 识别唯一性约束违反、外键约束违反等
   - 转换为友好的错误消息

4. **日志记录**
   - 所有异常都记录到 Serilog
   - 包含请求上下文（路径、方法、用户 ID）
   - 敏感信息脱敏

## 测试策略

### 单元测试

使用 xUnit 和 Moq 进行单元测试：

**测试范围：**
- Service 层业务逻辑
- 验证逻辑
- DTO 映射
- 工具类和辅助方法

**测试示例：**
- 客户创建时的唯一性验证
- LastInteractionAt 更新逻辑
- 软删除过滤逻辑
- 分页计算逻辑

### 属性测试

使用 FsCheck 进行属性测试：

**配置：**
- 每个属性测试运行至少 100 次迭代
- 使用自定义生成器生成有效的测试数据
- 每个属性测试必须标注对应的设计文档属性编号

**测试标注格式：**
```csharp
[Property]
[Trait("Feature", "crm-system")]
[Trait("Property", "1")]
public Property CustomerCreateRoundTrip()
{
    // Feature: crm-system, Property 1: 客户创建往返一致性
    // ...
}
```

**生成器设计：**
- `CustomerGenerator`: 生成有效的客户数据
- `InteractionGenerator`: 生成有效的互动记录数据
- `InvalidDataGenerator`: 生成各种无效数据用于错误测试
- `ConcurrentUpdateGenerator`: 生成并发更新场景

**属性测试覆盖：**
- 属性 1-25：所有正确性属性都应该有对应的属性测试
- 重点测试往返一致性、不变量、筛选正确性、并发控制

### 集成测试

使用 WebApplicationFactory 和 Testcontainers 进行集成测试：

**测试范围：**
- 完整的 API 端点测试
- 数据库交互测试
- 认证流程测试
- 中间件行为测试

**测试环境：**
- 使用 Testcontainers 启动真实的 PostgreSQL 容器
- 每个测试使用独立的数据库实例
- 测试后自动清理

**测试示例：**
- 端到端的客户 CRUD 流程
- 并发更新冲突检测
- 认证令牌验证
- 错误响应格式验证

### 前端测试

使用 Vitest 和 React Testing Library：

**测试范围：**
- 组件渲染测试
- 用户交互测试
- API 集成测试（使用 MSW mock）
- 状态管理测试

**测试示例：**
- 客户列表筛选功能
- 互动时间线展示
- 表单验证
- 乐观更新行为


## 关键实现细节

### 并发控制实现

#### ETag 格式

使用 UpdatedAt 时间戳作为 ETag：
- 格式：`W/"<unix_milliseconds>"`（弱 ETag）
- 示例：`W/"1702034400123"`（Unix 毫秒时间戳）
- 客户端在 If-Match 头中提供此值

#### ETag 生成和解析

```csharp
public static class ETagHelper
{
    public static string GenerateETag(DateTimeOffset updatedAt)
    {
        var milliseconds = updatedAt.ToUnixTimeMilliseconds();
        return $"W/\"{milliseconds}\"";
    }
    
    public static DateTimeOffset? ParseETag(string? etag)
    {
        if (string.IsNullOrWhiteSpace(etag))
            return null;
        
        // 移除 W/" 和 "
        var value = etag.Replace("W/\"", "").Replace("\"", "");
        
        if (long.TryParse(value, out var milliseconds))
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(milliseconds);
        }
        
        return null;
    }
}

// Controller 中使用
[HttpPut("{id}")]
public async Task<IActionResult> UpdateCustomer(Guid id, [FromBody] UpdateCustomerRequest request)
{
    var ifMatch = Request.Headers["If-Match"].FirstOrDefault();
    var originalUpdatedAt = ETagHelper.ParseETag(ifMatch);
    
    if (originalUpdatedAt == null)
    {
        _logger.LogWarning("Update request without If-Match header for customer {CustomerId}", id);
    }
    
    var customer = await _customerService.UpdateCustomerAsync(id, request, originalUpdatedAt);
    
    // 设置新的 ETag
    Response.Headers["ETag"] = ETagHelper.GenerateETag(customer.UpdatedAt);
    
    return Ok(new ApiResponse<CustomerResponse>
    {
        Success = true,
        Data = _mapper.Map<CustomerResponse>(customer),
        Errors = new List<ErrorDetail>()
    });
}
```

#### EF Core 配置

```csharp
modelBuilder.Entity<Customer>(entity =>
{
    entity.Property(e => e.UpdatedAt)
          .IsConcurrencyToken()
          .HasDefaultValueSql("NOW()");
    
    entity.HasIndex(e => new { e.CompanyName, e.ContactName })
          .IsUnique()
          .HasFilter("is_deleted = false");
});

modelBuilder.Entity<Interaction>(entity =>
{
    entity.Property(e => e.UpdatedAt)
          .IsConcurrencyToken()
          .HasDefaultValueSql("NOW()");
});
```

#### 并发冲突处理

```csharp
public async Task<Customer> UpdateCustomerAsync(Guid id, UpdateCustomerRequest request, DateTimeOffset? originalUpdatedAt)
{
    var customer = await _context.Customers.FindAsync(id);
    if (customer == null || customer.IsDeleted)
        throw new NotFoundException("Customer not found");
    
    // 验证版本（如果提供了 If-Match）
    if (originalUpdatedAt.HasValue)
    {
        // 使用毫秒精度比较，避免微秒级差异
        var currentMillis = customer.UpdatedAt.ToUnixTimeMilliseconds();
        var providedMillis = originalUpdatedAt.Value.ToUnixTimeMilliseconds();
        
        if (currentMillis != providedMillis)
        {
            throw new ConcurrencyException("Customer has been modified", customer.UpdatedAt);
        }
    }
    
    // 更新字段
    customer.CompanyName = request.CompanyName;
    // ... 其他字段
    customer.UpdatedAt = DateTimeOffset.UtcNow;
    
    try
    {
        await _context.SaveChangesAsync();
        return customer;
    }
    catch (DbUpdateConcurrencyException)
    {
        // EF Core 检测到并发冲突
        var current = await _context.Customers.AsNoTracking().FirstAsync(c => c.Id == id);
        throw new ConcurrencyException("Customer has been modified", current.UpdatedAt);
    }
}
```

### 时间处理规范

#### 统一使用 DateTimeOffset

所有时间字段使用 `DateTimeOffset` 而非 `DateTime`：
- 明确时区信息
- 避免时区转换错误
- 数据库存储为 TIMESTAMPTZ（PostgreSQL）

#### UTC 归一化

```csharp
// 在保存前统一转换为 UTC
public override int SaveChanges()
{
    var entries = ChangeTracker.Entries()
        .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);
    
    foreach (var entry in entries)
    {
        foreach (var property in entry.Properties)
        {
            if (property.Metadata.ClrType == typeof(DateTimeOffset) ||
                property.Metadata.ClrType == typeof(DateTimeOffset?))
            {
                if (property.CurrentValue is DateTimeOffset dto)
                {
                    property.CurrentValue = dto.ToUniversalTime();
                }
            }
        }
    }
    
    return base.SaveChanges();
}
```

#### JSON 序列化配置

```csharp
// Program.cs
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new DateTimeOffsetConverter());
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

// 自定义转换器确保 ISO 8601 格式
public class DateTimeOffsetConverter : JsonConverter<DateTimeOffset>
{
    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return DateTimeOffset.Parse(reader.GetString()!);
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
    }
}
```

### 唯一性约束实现

#### EF Core 配置

```csharp
modelBuilder.Entity<Customer>(entity =>
{
    // 部分唯一索引（仅对未删除记录）
    entity.HasIndex(e => new { e.CompanyName, e.ContactName })
          .IsUnique()
          .HasFilter("is_deleted = false")
          .HasDatabaseName("uq_customer_company_contact");
});
```

#### 业务层验证

```csharp
public async Task<Customer> CreateCustomerAsync(CreateCustomerRequest request)
{
    // 检查唯一性
    var exists = await _context.Customers
        .Where(c => !c.IsDeleted)
        .AnyAsync(c => c.CompanyName == request.CompanyName && 
                      c.ContactName == request.ContactName);
    
    if (exists)
    {
        throw new ConflictException("A customer with the same company name and contact name already exists");
    }
    
    var customer = new Customer
    {
        CompanyName = request.CompanyName,
        ContactName = request.ContactName,
        // ... 其他字段
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow
    };
    
    _context.Customers.Add(customer);
    
    try
    {
        await _context.SaveChangesAsync();
        return customer;
    }
    catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("uq_customer_company_contact") == true)
    {
        throw new ConflictException("A customer with the same company name and contact name already exists");
    }
}
```

### LastInteractionAt 维护

#### 事务边界

所有互动记录的创建、更新、删除操作必须在同一事务中更新客户的 LastInteractionAt：

```csharp
public async Task<Interaction> CreateInteractionAsync(Guid customerId, CreateInteractionRequest request)
{
    using var transaction = await _context.Database.BeginTransactionAsync();
    
    try
    {
        // 验证客户存在且未删除
        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Id == customerId && !c.IsDeleted);
        
        if (customer == null)
            throw new NotFoundException("Customer not found");
        
        // 创建互动记录
        var interaction = new Interaction
        {
            CustomerId = customerId,
            HappenedAt = request.HappenedAt.ToUniversalTime(),
            // ... 其他字段
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        
        _context.Interactions.Add(interaction);
        
        // 更新客户的 LastInteractionAt
        customer.LastInteractionAt = interaction.HappenedAt;
        customer.UpdatedAt = DateTimeOffset.UtcNow;
        
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
        
        return interaction;
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
}

public async Task DeleteInteractionAsync(Guid id)
{
    using var transaction = await _context.Database.BeginTransactionAsync();
    
    try
    {
        var interaction = await _context.Interactions
            .Include(i => i.Customer)
            .FirstOrDefaultAsync(i => i.Id == id);
        
        if (interaction == null)
            throw new NotFoundException("Interaction not found");
        
        var customerId = interaction.CustomerId;
        
        // 删除互动记录
        _context.Interactions.Remove(interaction);
        
        // 重新计算 LastInteractionAt
        var latestInteraction = await _context.Interactions
            .Where(i => i.CustomerId == customerId && i.Id != id)
            .OrderByDescending(i => i.HappenedAt)
            .FirstOrDefaultAsync();
        
        var customer = interaction.Customer;
        customer.LastInteractionAt = latestInteraction?.HappenedAt;
        customer.UpdatedAt = DateTimeOffset.UtcNow;
        
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
}
```

#### 排序规则

查询客户列表时，LastInteractionAt 为 null 的记录排在最后：

```csharp
var query = _context.Customers
    .Where(c => !c.IsDeleted)
    .OrderByDescending(c => c.LastInteractionAt ?? DateTimeOffset.MinValue);
```

### 认证配置逻辑

#### 环境变量验证

```csharp
public class AuthConfiguration
{
    public bool EnableAuth { get; set; }
    public string? AdminUsername { get; set; }
    public string? AdminPassword { get; set; }
    public string? JwtSecret { get; set; }
    public int JwtExpiryMinutes { get; set; } = 60;
    
    public void Validate()
    {
        if (EnableAuth)
        {
            if (string.IsNullOrWhiteSpace(JwtSecret))
                throw new InvalidOperationException("JWT_SECRET is required when ENABLE_AUTH=true");
            
            if (string.IsNullOrWhiteSpace(AdminUsername) || string.IsNullOrWhiteSpace(AdminPassword))
                throw new InvalidOperationException("ADMIN_USERNAME and ADMIN_PASSWORD are required when ENABLE_AUTH=true");
        }
    }
}
```

#### 启动时初始化

```csharp
// Program.cs
var authConfig = builder.Configuration.GetSection("Auth").Get<AuthConfiguration>();
authConfig.Validate();

if (authConfig.EnableAuth)
{
    // 配置 JWT 认证
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(authConfig.JwtSecret)),
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero
            };
        });
    
    // 创建初始管理员用户
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
    
    if (!await context.Users.AnyAsync())
    {
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(authConfig.AdminPassword);
        var admin = new User
        {
            UserName = authConfig.AdminUsername,
            PasswordHash = passwordHash,
            Role = "Admin",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        context.Users.Add(admin);
        await context.SaveChangesAsync();
    }
}
else
{
    // 禁用认证时不添加认证中间件
    // 所有端点都可以无需令牌访问
}
```

### 数据库迁移锁机制

#### PostgreSQL Advisory Lock

```csharp
public class MigrationService
{
    private const long MigrationLockId = 123456789; // 任意唯一 ID
    private readonly CrmDbContext _context;
    private readonly ILogger<MigrationService> _logger;
    
    public async Task MigrateAsync(CancellationToken cancellationToken = default)
    {
        var connection = _context.Database.GetDbConnection();
        await connection.OpenAsync(cancellationToken);
        
        try
        {
            // 尝试获取 advisory lock（非阻塞）
            using var command = connection.CreateCommand();
            command.CommandText = $"SELECT pg_try_advisory_lock({MigrationLockId})";
            var lockAcquired = (bool)(await command.ExecuteScalarAsync(cancellationToken))!;
            
            if (!lockAcquired)
            {
                _logger.LogInformation("Another instance is running migrations. Waiting...");
                
                // 阻塞等待锁（最多 30 秒）
                command.CommandText = $"SELECT pg_advisory_lock({MigrationLockId})";
                command.CommandTimeout = 30;
                await command.ExecuteNonQueryAsync(cancellationToken);
                
                _logger.LogInformation("Migration lock acquired");
            }
            
            try
            {
                // 执行迁移
                _logger.LogInformation("Applying database migrations...");
                await _context.Database.MigrateAsync(cancellationToken);
                _logger.LogInformation("Database migrations completed successfully");
            }
            finally
            {
                // 释放锁
                using var unlockCommand = connection.CreateCommand();
                unlockCommand.CommandText = $"SELECT pg_advisory_unlock({MigrationLockId})";
                await unlockCommand.ExecuteNonQueryAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database migration failed");
            throw new InvalidOperationException("Database migration failed. Please check the logs and database state.", ex);
        }
    }
}

// Program.cs
if (autoMigrate)
{
    try
    {
        var migrationService = app.Services.GetRequiredService<MigrationService>();
        await migrationService.MigrateAsync();
    }
    catch (Exception ex)
    {
        logger.LogCritical(ex, "Failed to apply database migrations. Application will not start.");
        return; // 阻止应用启动
    }
}
```

### 验证规则细节

#### 字段长度和格式

```csharp
public class CreateCustomerRequestValidator : AbstractValidator<CreateCustomerRequest>
{
    public CreateCustomerRequestValidator()
    {
        RuleFor(x => x.CompanyName)
            .NotEmpty().WithMessage("Company name is required")
            .MaximumLength(200).WithMessage("Company name must not exceed 200 characters");
        
        RuleFor(x => x.ContactName)
            .NotEmpty().WithMessage("Contact name is required")
            .MaximumLength(200).WithMessage("Contact name must not exceed 200 characters");
        
        RuleFor(x => x.Email)
            .EmailAddress().When(x => !string.IsNullOrEmpty(x.Email))
            .WithMessage("Invalid email format")
            .MaximumLength(255).WithMessage("Email must not exceed 255 characters");
        
        RuleFor(x => x.Phone)
            .MaximumLength(50).WithMessage("Phone must not exceed 50 characters");
        
        RuleFor(x => x.Wechat)
            .MaximumLength(100).WithMessage("Wechat must not exceed 100 characters");
        
        RuleFor(x => x.Score)
            .InclusiveBetween(0, 100).WithMessage("Score must be between 0 and 100");
        
        RuleFor(x => x.Tags)
            .Must(tags => tags == null || tags.All(t => t.Length <= 50))
            .WithMessage("Each tag must not exceed 50 characters");
    }
}
```

#### 关键词搜索实现

```csharp
public async Task<PagedResponse<Customer>> SearchCustomersAsync(CustomerSearchRequest request)
{
    var query = _context.Customers.Where(c => !c.IsDeleted);
    
    // 关键词搜索（大小写不敏感，搜索公司名和联系人名）
    // 使用 EF.Functions.ILike 以利用 PostgreSQL 索引
    if (!string.IsNullOrWhiteSpace(request.Keyword))
    {
        var keyword = $"%{request.Keyword}%";
        query = query.Where(c => 
            EF.Functions.ILike(c.CompanyName, keyword) || 
            EF.Functions.ILike(c.ContactName, keyword));
    }
    
    // 状态筛选
    if (request.Status.HasValue)
    {
        query = query.Where(c => c.Status == request.Status.Value);
    }
    
    // 行业筛选
    if (!string.IsNullOrWhiteSpace(request.Industry))
    {
        query = query.Where(c => c.Industry == request.Industry);
    }
    
    // 来源筛选
    if (request.Source.HasValue)
    {
        query = query.Where(c => c.Source == request.Source.Value);
    }
    
    // 排序
    query = request.SortBy switch
    {
        "CreatedAt" => request.SortOrder == "asc" 
            ? query.OrderBy(c => c.CreatedAt) 
            : query.OrderByDescending(c => c.CreatedAt),
        "UpdatedAt" => request.SortOrder == "asc" 
            ? query.OrderBy(c => c.UpdatedAt) 
            : query.OrderByDescending(c => c.UpdatedAt),
        _ => query.OrderByDescending(c => c.LastInteractionAt ?? DateTimeOffset.MinValue)
    };
    
    // 分页
    var total = await query.CountAsync();
    var page = Math.Max(1, request.Page ?? 1);
    var pageSize = Math.Min(100, Math.Max(1, request.PageSize ?? 20));
    
    var items = await query
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .AsNoTracking()
        .ToListAsync();
    
    return new PagedResponse<Customer>
    {
        Items = items,
        Total = total
    };
}
```

### 错误响应格式统一

#### 自定义异常类

```csharp
public class ValidationException : Exception
{
    public Dictionary<string, string> Errors { get; }
    
    public ValidationException(Dictionary<string, string> errors) 
        : base("Validation failed")
    {
        Errors = errors;
    }
}

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}

public class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}

public class ConcurrencyException : Exception
{
    public DateTimeOffset CurrentVersion { get; }
    
    public ConcurrencyException(string message, DateTimeOffset currentVersion) 
        : base(message)
    {
        CurrentVersion = currentVersion;
    }
}
```

#### 全局异常处理

```csharp
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }
    
    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var response = context.Response;
        response.ContentType = "application/json";
        
        var apiResponse = new ApiResponse<object>
        {
            Success = false,
            Data = null,
            Errors = new List<ErrorDetail>()
        };
        
        switch (exception)
        {
            case ValidationException validationEx:
                response.StatusCode = StatusCodes.Status400BadRequest;
                apiResponse.Errors = validationEx.Errors
                    .Select(e => new ErrorDetail { Field = e.Key, Message = e.Value })
                    .ToList();
                break;
            
            case NotFoundException notFoundEx:
                response.StatusCode = StatusCodes.Status404NotFound;
                apiResponse.Errors.Add(new ErrorDetail { Message = notFoundEx.Message });
                break;
            
            case ConflictException conflictEx:
                response.StatusCode = StatusCodes.Status409Conflict;
                apiResponse.Errors.Add(new ErrorDetail { Message = conflictEx.Message });
                break;
            
            case ConcurrencyException concurrencyEx:
                response.StatusCode = StatusCodes.Status409Conflict;
                apiResponse.Errors.Add(new ErrorDetail 
                { 
                    Message = concurrencyEx.Message,
                    Field = "UpdatedAt",
                    CurrentValue = concurrencyEx.CurrentVersion.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                });
                break;
            
            case UnauthorizedAccessException:
                response.StatusCode = StatusCodes.Status401Unauthorized;
                apiResponse.Errors.Add(new ErrorDetail { Message = "Unauthorized" });
                break;
            
            default:
                response.StatusCode = StatusCodes.Status500InternalServerError;
                apiResponse.Errors.Add(new ErrorDetail { Message = "An internal server error occurred" });
                _logger.LogError(exception, "Unhandled exception");
                break;
        }
        
        await response.WriteAsJsonAsync(apiResponse);
    }
}

public class ErrorDetail
{
    public string? Field { get; set; }
    public string Message { get; set; }
    public string? CurrentValue { get; set; }
}
```

## 技术实现细节

### 后端技术栈

- **框架**: ASP.NET Core 8.0
- **ORM**: Entity Framework Core 8.0 + Npgsql
- **验证**: FluentValidation
- **日志**: Serilog (结构化日志)
- **认证**: JWT Bearer Authentication (可选)
- **密码哈希**: BCrypt.Net
- **API 文档**: Swagger/OpenAPI
- **测试**: xUnit + Moq + FsCheck + Testcontainers

### 前端技术栈

- **框架**: React 18 + TypeScript
- **构建工具**: Vite
- **UI 库**: Ant Design (或 Material-UI)
- **状态管理**: React Query (TanStack Query)
- **路由**: React Router v6
- **HTTP 客户端**: Axios
- **表单**: React Hook Form + Zod
- **测试**: Vitest + React Testing Library + MSW

### 数据库

- **数据库**: PostgreSQL 16
- **迁移管理**: EF Core Migrations
- **连接池**: Npgsql 连接池
- **备份策略**: 定期 pg_dump（后续实现）

### 配置管理

#### 环境变量

后端 API 支持以下环境变量：

```bash
# 数据库连接
ConnectionStrings__Default=Host=localhost;Port=5432;Database=crm_db;Username=crm_user;Password=crm_pass

# 认证配置（可选）
ENABLE_AUTH=false
ADMIN_USERNAME=admin
ADMIN_PASSWORD=admin123
JWT_SECRET=your-secret-key-here
JWT_EXPIRY_MINUTES=60

# 迁移配置
AUTO_MIGRATE=true

# 日志配置
SERILOG_MINIMUM_LEVEL=Information

# CORS 配置
CORS_ORIGINS=http://localhost:3000,http://localhost:5173
```

前端应用支持以下环境变量：

```bash
VITE_API_BASE_URL=http://localhost:5000
VITE_ENABLE_AUTH=false
```

### 日志与监控

#### 日志结构

使用 Serilog 记录结构化日志：

```json
{
  "timestamp": "2024-12-08T10:30:00.123Z",
  "level": "Information",
  "messageTemplate": "HTTP {Method} {Path} responded {StatusCode} in {Elapsed}ms",
  "properties": {
    "Method": "GET",
    "Path": "/api/customers",
    "StatusCode": 200,
    "Elapsed": 45,
    "UserId": "user-guid",
    "RequestId": "request-guid"
  }
}
```

#### 脱敏规则

敏感字段自动脱敏：

- **手机号**: `13812345678` → `****5678`
- **邮箱**: `user@example.com` → `us**@example.com`
- **微信号**: `wechat123456` → `we****56`

#### 健康检查

健康检查端点 `/health` 返回：

```json
{
  "status": "Healthy",
  "checks": {
    "database": "Healthy",
    "memory": "Healthy"
  },
  "duration": "00:00:00.0234567"
}
```

### 安全性

#### 认证与授权

- JWT Bearer Token 认证（可选启用）
- 令牌过期时间可配置（默认 60 分钟）
- 密码使用 BCrypt 哈希（work factor = 12）
- 初始管理员通过环境变量配置

#### CORS 策略

- 白名单配置（通过环境变量）
- 支持预检请求
- 允许的方法：GET, POST, PUT, DELETE, OPTIONS
- 允许的头：Content-Type, Authorization, If-Match

#### 输入验证

- 所有输入使用 FluentValidation 验证
- 防止 SQL 注入（使用参数化查询）
- 防止 XSS（前端输出转义）
- 字段长度限制
- 枚举值白名单验证

#### 并发控制

- 使用乐观并发控制（UpdatedAt 字段）
- If-Match 头支持
- 冲突时返回当前版本信息
- 避免丢失更新问题

### 性能优化

#### 数据库优化

- 索引策略：
  - `customers(status, industry, source)` - 筛选查询
  - `customers(last_interaction_at DESC)` - 排序查询
  - `interactions(customer_id, happened_at DESC)` - 时间线查询
  - `customers(company_name, contact_name)` - 唯一性约束

- 查询优化：
  - 使用 `AsNoTracking()` 进行只读查询
  - 分页查询避免加载所有数据
  - 使用投影（Select）减少数据传输
  - 避免 N+1 查询问题（使用 Include）

#### API 优化

- 响应压缩（Gzip）
- 分页默认限制（最大 100 条）
- 缓存策略（ETag 支持）
- 连接池配置

#### 前端优化

- React Query 缓存策略
- 虚拟滚动（大列表）
- 懒加载路由
- 图片懒加载
- 防抖搜索输入

### 部署架构

#### Docker Compose 配置

```yaml
version: '3.9'

services:
  db:
    image: postgres:16
    environment:
      POSTGRES_USER: crm_user
      POSTGRES_PASSWORD: crm_pass
      POSTGRES_DB: crm_db
    volumes:
      - crm_db_data:/var/lib/postgresql/data
    ports:
      - "5432:5432"
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U crm_user"]
      interval: 10s
      timeout: 5s
      retries: 5

  api:
    build:
      context: ./src/Api
      dockerfile: Dockerfile
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      ConnectionStrings__Default: Host=db;Port=5432;Database=crm_db;Username=crm_user;Password=crm_pass
      ENABLE_AUTH: "false"
      AUTO_MIGRATE: "true"
      CORS_ORIGINS: http://localhost:3000
    depends_on:
      db:
        condition: service_healthy
    ports:
      - "5000:8080"
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s
      timeout: 10s
      retries: 3

  web:
    build:
      context: ./frontend
      dockerfile: Dockerfile
    environment:
      VITE_API_BASE_URL: http://localhost:5000
      VITE_ENABLE_AUTH: "false"
    depends_on:
      - api
    ports:
      - "3000:80"

volumes:
  crm_db_data:
```

#### 迁移策略

- 开发环境：`AUTO_MIGRATE=true`，API 启动时自动执行迁移
- 生产环境：`AUTO_MIGRATE=false`，手动执行迁移命令
- 多实例部署：使用数据库锁确保只有一个实例执行迁移
- 迁移失败：记录错误并阻止 API 启动

#### 扩展性考虑

- API 无状态设计，支持水平扩展
- 数据库连接池配置
- 负载均衡器支持（Nginx/HAProxy）
- 后续可添加 Redis 缓存层
- 后续可添加消息队列（RabbitMQ/Kafka）

### 开发工作流

#### 后端开发

1. 创建 EF Core 实体模型
2. 生成迁移：`dotnet ef migrations add MigrationName`
3. 实现 Repository 接口
4. 实现 Service 业务逻辑
5. 实现 Controller 端点
6. 编写单元测试和属性测试
7. 运行集成测试
8. 更新 API 文档

#### 前端开发

1. 定义 TypeScript 类型
2. 实现 API 客户端函数
3. 创建 React Query hooks
4. 实现 UI 组件
5. 编写组件测试
6. 集成到页面
7. 测试用户流程

#### 本地开发环境

```bash
# 启动数据库
docker-compose up db

# 运行后端 API
cd src/Api
dotnet run

# 运行前端
cd frontend
npm run dev

# 运行测试
dotnet test
npm test
```

## 未来扩展

### 短期扩展（MVP 后）

1. **数据导入导出**
   - CSV/Excel 导入客户数据
   - 导出客户列表和互动记录
   - 批量操作支持

2. **附件管理**
   - 上传互动相关附件
   - 文件存储（本地或云存储）
   - 附件预览

3. **高级搜索**
   - 全文搜索
   - 多条件组合搜索
   - 保存搜索条件

4. **仪表板**
   - 客户统计图表
   - 销售漏斗可视化
   - 互动趋势分析

### 长期扩展

1. **AI 功能**
   - 智能客户分类
   - 互动内容摘要生成
   - 下一步行动建议
   - 客户流失预测

2. **多用户协作**
   - 用户角色和权限
   - 客户分配和转移
   - 团队协作功能
   - 活动日志

3. **集成能力**
   - 邮件集成（Gmail/Outlook）
   - 日历集成
   - 第三方 CRM 数据同步
   - Webhook 支持

4. **移动应用**
   - React Native 移动端
   - 离线支持
   - 推送通知

## 总结

本设计文档描述了一个简洁、可扩展的 CRM 系统架构。系统采用现代技术栈，遵循最佳实践，提供清晰的分层架构和完善的测试策略。通过 Docker 化部署，系统可以快速启动和扩展。设计中预留了扩展点，支持未来添加 AI 功能和高级特性。
