# CRM System - 开发指南

本文档提供 CRM 系统的详细开发指南，包括架构说明、开发规范和最佳实践。

## 目录

- [架构概述](#架构概述)
- [开发环境设置](#开发环境设置)
- [后端开发](#后端开发)
- [前端开发](#前端开发)
- [测试指南](#测试指南)
- [API 规范](#api-规范)
- [数据库设计](#数据库设计)
- [部署指南](#部署指南)

## 架构概述

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
└──────────────┬──────────────────────────┘
               │
┌──────────────┴──────────────────────────┐
│      数据访问层 (EF Core + Repositories) │
│  - DbContext                            │
│  - Entity Models                        │
└──────────────┬──────────────────────────┘
               │
┌──────────────┴──────────────────────────┐
│         数据库层 (PostgreSQL)            │
└─────────────────────────────────────────┘
```

### 目录结构

```
CrmSystem/
├── CrmSystem.Api/              # 后端 API
│   ├── Controllers/            # 控制器（薄层，仅处理 HTTP）
│   ├── Services/               # 业务逻辑
│   ├── Repositories/           # 数据访问
│   ├── Models/                 # 实体模型
│   ├── DTOs/                   # 数据传输对象
│   │   └── Validators/         # 请求验证器
│   ├── Middleware/             # 中间件
│   ├── Data/                   # DbContext
│   ├── Exceptions/             # 自定义异常
│   └── Helpers/                # 工具类
├── CrmSystem.Tests/            # 测试项目
└── crm-frontend/               # 前端项目
```

## 开发环境设置

### 必需工具

1. **.NET 8 SDK**
   ```bash
   # macOS (Homebrew)
   brew install dotnet@8
   
   # 验证安装
   dotnet --version
   ```

2. **PostgreSQL 16**
   ```bash
   # 使用 Docker（推荐）
   docker run -d \
     --name crm-postgres \
     -e POSTGRES_PASSWORD=postgres \
     -e POSTGRES_DB=crm \
     -p 5432:5432 \
     postgres:16-alpine
   ```

3. **Node.js 18+**
   ```bash
   # macOS (Homebrew)
   brew install node@18
   
   # 验证安装
   node --version
   npm --version
   ```

4. **推荐的 IDE**
   - Visual Studio Code + C# Dev Kit
   - JetBrains Rider
   - Visual Studio 2022

### VS Code 推荐扩展

```json
{
  "recommendations": [
    "ms-dotnettools.csharp",
    "ms-dotnettools.csdevkit",
    "dbaeumer.vscode-eslint",
    "esbenp.prettier-vscode",
    "bradlc.vscode-tailwindcss"
  ]
}
```

## 后端开发

### 代码规范

#### 命名约定

| 类型 | 约定 | 示例 |
|------|------|------|
| 类/接口 | PascalCase | `CustomerService`, `ICustomerRepository` |
| 方法 | PascalCase | `GetCustomerByIdAsync` |
| 属性 | PascalCase | `CompanyName` |
| 私有字段 | _camelCase | `_customerRepository` |
| 参数 | camelCase | `customerId` |
| 常量 | PascalCase | `MaxPageSize` |

#### 异步编程

所有 I/O 操作必须使用异步方法：

```csharp
// ✅ 正确
public async Task<Customer> GetCustomerByIdAsync(Guid id, CancellationToken cancellationToken)
{
    return await _context.Customers
        .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
}

// ❌ 错误
public Customer GetCustomerById(Guid id)
{
    return _context.Customers.FirstOrDefault(c => c.Id == id);
}
```

#### 依赖注入

使用构造函数注入：

```csharp
public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly ILogger<CustomerService> _logger;

    public CustomerService(
        ICustomerRepository customerRepository,
        ILogger<CustomerService> logger)
    {
        _customerRepository = customerRepository;
        _logger = logger;
    }
}
```

### 添加新功能

#### 1. 创建实体模型

```csharp
// Models/NewEntity.cs
public class NewEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
```

#### 2. 配置 DbContext

```csharp
// Data/CrmDbContext.cs
public DbSet<NewEntity> NewEntities { get; set; }

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<NewEntity>(entity =>
    {
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
        entity.Property(e => e.UpdatedAt).IsConcurrencyToken();
    });
}
```

#### 3. 创建 DTO

```csharp
// DTOs/CreateNewEntityRequest.cs
public class CreateNewEntityRequest
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;
}

// DTOs/NewEntityResponse.cs
public class NewEntityResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
```

#### 4. 创建 Repository

```csharp
// Repositories/INewEntityRepository.cs
public interface INewEntityRepository
{
    Task<NewEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<NewEntity> CreateAsync(NewEntity entity, CancellationToken cancellationToken);
}

// Repositories/NewEntityRepository.cs
public class NewEntityRepository : INewEntityRepository
{
    private readonly CrmDbContext _context;

    public NewEntityRepository(CrmDbContext context)
    {
        _context = context;
    }

    public async Task<NewEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.NewEntities
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<NewEntity> CreateAsync(NewEntity entity, CancellationToken cancellationToken)
    {
        _context.NewEntities.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return entity;
    }
}
```

#### 5. 创建 Service

```csharp
// Services/INewEntityService.cs
public interface INewEntityService
{
    Task<NewEntity> CreateAsync(CreateNewEntityRequest request, CancellationToken cancellationToken);
}

// Services/NewEntityService.cs
public class NewEntityService : INewEntityService
{
    private readonly INewEntityRepository _repository;

    public NewEntityService(INewEntityRepository repository)
    {
        _repository = repository;
    }

    public async Task<NewEntity> CreateAsync(CreateNewEntityRequest request, CancellationToken cancellationToken)
    {
        var entity = new NewEntity
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        return await _repository.CreateAsync(entity, cancellationToken);
    }
}
```

#### 6. 创建 Controller

```csharp
// Controllers/NewEntitiesController.cs
[ApiController]
[Route("api/[controller]")]
public class NewEntitiesController : ControllerBase
{
    private readonly INewEntityService _service;

    public NewEntitiesController(INewEntityService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<NewEntityResponse>>> Create(
        [FromBody] CreateNewEntityRequest request,
        CancellationToken cancellationToken)
    {
        var entity = await _service.CreateAsync(request, cancellationToken);
        
        return CreatedAtAction(
            nameof(GetById),
            new { id = entity.Id },
            new ApiResponse<NewEntityResponse>
            {
                Success = true,
                Data = MapToResponse(entity),
                Errors = new List<ErrorDetail>()
            });
    }
}
```

#### 7. 注册服务

```csharp
// Program.cs
builder.Services.AddScoped<INewEntityRepository, NewEntityRepository>();
builder.Services.AddScoped<INewEntityService, NewEntityService>();
```

#### 8. 创建迁移

```bash
cd CrmSystem.Api
dotnet ef migrations add AddNewEntity
dotnet ef database update
```

### 错误处理

使用自定义异常类：

```csharp
// 资源不存在
throw new NotFoundException("Customer not found");

// 验证错误
throw new ValidationException("CompanyName", "Company name is required");

// 唯一性冲突
throw new ConflictException("A customer with the same name already exists");

// 并发冲突
throw new ConcurrencyException("Resource has been modified", currentUpdatedAt);
```

异常会被 `ExceptionHandlingMiddleware` 自动转换为适当的 HTTP 响应。

## 前端开发

### 代码规范

#### 组件结构

```tsx
// 函数组件模板
import React from 'react';

interface Props {
  title: string;
  onSubmit: (data: FormData) => void;
}

export const MyComponent: React.FC<Props> = ({ title, onSubmit }) => {
  // Hooks
  const [state, setState] = useState<string>('');
  
  // Event handlers
  const handleSubmit = () => {
    onSubmit({ value: state });
  };
  
  // Render
  return (
    <div>
      <h1>{title}</h1>
      <button onClick={handleSubmit}>Submit</button>
    </div>
  );
};
```

#### API 调用

使用 React Query 管理服务器状态：

```tsx
// hooks/useCustomers.ts
export const useCustomers = (params: CustomerSearchParams) => {
  return useQuery({
    queryKey: ['customers', params],
    queryFn: () => customersApi.getCustomers(params),
  });
};

export const useCreateCustomer = () => {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: customersApi.createCustomer,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['customers'] });
    },
  });
};
```

### 添加新页面

1. 创建页面组件 `src/pages/NewPage.tsx`
2. 添加路由 `src/App.tsx`
3. 创建相关 API 函数 `src/api/newEntity.ts`
4. 创建 React Query hooks `src/hooks/useNewEntity.ts`

## 测试指南

### 单元测试

```csharp
public class CustomerServiceTests
{
    private readonly Mock<ICustomerRepository> _repositoryMock;
    private readonly CustomerService _service;

    public CustomerServiceTests()
    {
        _repositoryMock = new Mock<ICustomerRepository>();
        _service = new CustomerService(_repositoryMock.Object, Mock.Of<ILogger<CustomerService>>());
    }

    [Fact]
    public async Task CreateCustomer_WithValidData_ReturnsCustomer()
    {
        // Arrange
        var request = new CreateCustomerRequest { CompanyName = "Test", ContactName = "John" };
        
        // Act
        var result = await _service.CreateCustomerAsync(request, CancellationToken.None);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test", result.CompanyName);
    }
}
```

### 属性测试

```csharp
[Property]
[Trait("Feature", "crm-system")]
[Trait("Property", "1")]
public Property CustomerCreateRoundTrip()
{
    // Feature: crm-system, Property 1: 客户创建往返一致性
    return Prop.ForAll(
        CustomerGenerator.ValidCustomerRequest(),
        async request =>
        {
            var created = await _service.CreateCustomerAsync(request, CancellationToken.None);
            var retrieved = await _service.GetCustomerByIdAsync(created.Id, CancellationToken.None);
            
            return created.CompanyName == retrieved.CompanyName &&
                   created.ContactName == retrieved.ContactName;
        });
}
```

### 集成测试

```csharp
public class CustomerApiTests : IntegrationTestBase
{
    [Fact]
    public async Task CreateCustomer_ReturnsCreatedCustomer()
    {
        // Arrange
        var request = new CreateCustomerRequest
        {
            CompanyName = "Test Company",
            ContactName = "John Doe"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/customers", request);

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}
```

## API 规范

### 请求/响应格式

所有 API 使用 JSON 格式，时间戳使用 ISO 8601 格式（UTC）。

### HTTP 状态码

| 状态码 | 含义 |
|--------|------|
| 200 | 成功 |
| 201 | 创建成功 |
| 204 | 删除成功 |
| 400 | 请求验证失败 |
| 401 | 未认证 |
| 404 | 资源不存在 |
| 409 | 冲突（唯一性/并发） |
| 500 | 服务器错误 |

### 并发控制

使用 ETag 实现乐观锁：

```http
# 获取资源
GET /api/customers/123
Response Headers:
  ETag: W/"1702034400123"

# 更新资源
PUT /api/customers/123
Request Headers:
  If-Match: W/"1702034400123"
```

## 数据库设计

### 主要表结构

#### customers 表
- `id` - UUID 主键
- `company_name` - 公司名称
- `contact_name` - 联系人姓名
- `status` - 客户状态
- `is_deleted` - 软删除标记
- `last_interaction_at` - 最后互动时间
- `created_at` / `updated_at` - 时间戳

#### interactions 表
- `id` - UUID 主键
- `customer_id` - 关联客户
- `happened_at` - 发生时间
- `channel` - 沟通渠道
- `title` - 标题
- `summary` - 摘要

### 索引策略

- 在筛选字段（status, industry, source）上创建索引
- 在 `last_interaction_at` 上创建索引用于排序
- 在 `(customer_id, happened_at)` 上创建复合索引

## 部署指南

### Docker 部署

```bash
# 构建镜像
docker-compose build

# 启动服务
docker-compose up -d

# 查看日志
docker-compose logs -f

# 停止服务
docker-compose down
```

### 生产环境配置

1. 设置强密码
2. 配置 HTTPS
3. 启用认证 (`ENABLE_AUTH=true`)
4. 配置适当的 CORS 来源
5. 设置日志级别为 `Warning` 或 `Error`

### 健康检查

```bash
curl http://localhost:8080/health
```

响应示例：
```json
{
  "status": "Healthy",
  "checks": {
    "database": "Healthy"
  }
}
```

## 常见问题

### Q: 如何添加新的客户状态？

1. 更新 `CustomerStatus` 枚举
2. 创建数据库迁移
3. 更新前端类型定义

### Q: 如何自定义日志格式？

编辑 `appsettings.json` 中的 Serilog 配置。

### Q: 如何添加新的 API 端点？

参考 [添加新功能](#添加新功能) 章节。

## 参考资料

- [ASP.NET Core 文档](https://docs.microsoft.com/aspnet/core)
- [Entity Framework Core 文档](https://docs.microsoft.com/ef/core)
- [React 文档](https://react.dev)
- [Ant Design 文档](https://ant.design)
