# 实现计划

- [x] 1. 初始化后端项目结构
  - 创建 ASP.NET Core 8 Web API 项目
  - 配置项目依赖（EF Core, Npgsql, FluentValidation, Serilog, BCrypt.Net, FsCheck, xUnit）
  - 设置项目结构（Controllers, Services, Repositories, Models, DTOs, Middleware）
  - _需求：所有需求_

- [x] 2. 配置数据库和 EF Core
  - 配置 PostgreSQL 连接字符串
  - 创建 CrmDbContext
  - 配置 DateTimeOffset 自动 UTC 转换
  - 配置 JSON 序列化选项（ISO 8601 格式）
  - _需求：4.2, 4.3, 10.2_

- [x] 3. 实现核心实体模型
- [x] 3.1 创建 Customer 实体
  - 实现 Customer 类（包含所有字段和导航属性）
  - 配置 EF Core 映射（字段长度、索引、唯一约束、并发令牌）
  - 配置部分唯一索引（is_deleted = false）
  - _需求：1.1, 4.1, 4.8, 9.1, 9.2, 10.1_

- [x] 3.2 创建 Interaction 实体
  - 实现 Interaction 类和 AttachmentInfo 类
  - 配置 EF Core 映射（字段长度、外键、索引、并发令牌）
  - 配置 JSONB 存储 Attachments
  - _需求：3.1, 9.3, 10.1_

- [x] 3.3 创建 User 实体（可选）
  - 实现 User 类
  - 配置 EF Core 映射（唯一约束）
  - _需求：5.1, 5.2_

- [x] 3.4 创建枚举类型
  - 实现 CustomerStatus 枚举
  - 实现 CustomerSource 枚举
  - 实现 InteractionChannel 枚举
  - _需求：4.6, 4.7_

- [x] 3.5 生成初始数据库迁移
  - 运行 `dotnet ef migrations add InitialCreate`
  - 验证生成的迁移脚本
  - _需求：8.2_

- [x] 4. 实现数据库迁移锁机制
  - 创建 MigrationService 类
  - 实现 PostgreSQL advisory lock 逻辑
  - 配置超时和错误处理
  - 在 Program.cs 中集成迁移服务
  - _需求：8.2, 8.3, 8.4, 8.5_

- [x] 5. 实现 DTO 和验证
- [x] 5.1 创建请求 DTO
  - 实现 CreateCustomerRequest 和 UpdateCustomerRequest
  - 实现 CreateInteractionRequest 和 UpdateInteractionRequest
  - 实现 CustomerSearchRequest
  - _需求：1.1, 1.3, 2.1, 3.1, 3.4_

- [x] 5.2 创建响应 DTO
  - 实现 CustomerResponse
  - 实现 InteractionResponse
  - 实现 PagedResponse<T>
  - 实现 ApiResponse<T> 和 ErrorDetail
  - _需求：6.1, 6.2, 6.5_

- [x] 5.3 实现 FluentValidation 验证器
  - 创建 CreateCustomerRequestValidator
  - 创建 CreateInteractionRequestValidator
  - 验证字段长度、格式、范围、枚举值
  - _需求：1.7, 3.7, 4.5, 4.6, 4.7, 4.8, 4.9_

- [x] 6. 实现 Repository 层
- [x] 6.1 创建 ICustomerRepository 和 CustomerRepository
  - 实现基本 CRUD 操作
  - 实现搜索和筛选方法（使用 EF.Functions.ILike）
  - 实现分页查询
  - _需求：1.1, 1.2, 1.3, 1.4, 2.1-2.9_

- [x] 6.2 创建 IInteractionRepository 和 InteractionRepository
  - 实现基本 CRUD 操作
  - 实现按客户查询互动记录
  - _需求：3.1, 3.3, 3.4, 3.5_

- [x] 6.3 创建 IUserRepository 和 UserRepository（可选）
  - 实现用户查询和创建
  - _需求：5.1, 5.2_

- [x] 7. 实现 Service 层
- [x] 7.1 实现 CustomerService
  - 实现创建客户（含唯一性验证）
  - 实现查询客户详情
  - 实现更新客户（含并发控制）
  - 实现软删除客户
  - 实现搜索和筛选客户
  - _需求：1.1-1.8, 2.1-2.9, 4.1, 10.3, 10.4_

- [ ]* 7.2 编写属性测试：客户创建往返一致性
  - **属性 1：客户创建往返一致性**
  - **验证：需求 1.1**

- [ ]* 7.3 编写属性测试：客户更新反映性
  - **属性 2：客户更新反映性**
  - **验证：需求 1.3**

- [ ]* 7.4 编写属性测试：软删除过滤性
  - **属性 4：软删除过滤性**
  - **验证：需求 1.6**

- [ ]* 7.5 编写属性测试：唯一性约束强制性
  - **属性 20：唯一性约束强制性**
  - **验证：需求 4.1**

- [x] 7.6 实现 InteractionService
  - 实现创建互动记录（含事务更新 LastInteractionAt）
  - 实现查询互动记录列表
  - 实现更新互动记录（含并发控制）
  - 实现删除互动记录（含事务重新计算 LastInteractionAt）
  - _需求：3.1-3.8, 10.3, 10.4, 10.5_

- [ ]* 7.7 编写属性测试：LastInteractionAt 同步性
  - **属性 14：LastInteractionAt 同步性**
  - **验证：需求 3.2**

- [ ]* 7.8 编写属性测试：LastInteractionAt 重新计算正确性
  - **属性 18：LastInteractionAt 重新计算正确性**
  - **验证：需求 3.6**

- [ ]* 7.9 编写属性测试：软删除不变性
  - **属性 3：软删除不变性**
  - **验证：需求 1.5**

- [x] 7.10 实现 AuthService（可选）
  - 实现登录验证
  - 实现 JWT 令牌生成
  - 实现密码哈希验证
  - 实现初始管理员创建
  - _需求：5.1-5.8_

- [x] 8. 实现工具类和辅助方法
- [x] 8.1 实现 ETagHelper
  - 实现 GenerateETag 方法
  - 实现 ParseETag 方法
  - _需求：10.3, 10.4, 10.5_

- [x] 8.2 实现自定义异常类
  - 创建 ValidationException
  - 创建 NotFoundException
  - 创建 ConflictException
  - 创建 ConcurrencyException
  - _需求：6.3, 6.4, 6.5, 6.6, 6.7, 6.8_

- [x] 9. 实现 Middleware
- [x] 9.1 实现 ExceptionHandlingMiddleware
  - 捕获所有异常
  - 转换为统一的 ApiResponse 格式
  - 记录错误日志
  - _需求：6.2, 6.6, 6.9_

- [x] 9.2 实现 RequestLoggingMiddleware
  - 记录请求路径、方法、状态码、处理时间
  - 实现敏感字段脱敏（手机号、邮箱、微信号）
  - _需求：7.1, 7.2, 7.3, 7.4, 7.5_

- [x] 9.3 配置 CORS Middleware
  - 配置白名单
  - 配置允许的方法和头
  - _需求：安全性_

- [x] 9.4 配置 Authentication Middleware（可选）
  - 配置 JWT Bearer 认证
  - 配置令牌验证参数
  - _需求：5.2, 5.3, 5.6_

- [x] 10. 实现 Controllers
- [x] 10.1 实现 CustomersController
  - GET /api/customers（列表查询）
  - GET /api/customers/{id}（详情查询）
  - POST /api/customers（创建）
  - PUT /api/customers/{id}（更新，含 If-Match 支持）
  - DELETE /api/customers/{id}（软删除，含 If-Match 支持）
  - 设置 ETag 响应头
  - _需求：1.1-1.8, 2.1-2.9, 10.3, 10.4, 10.5_

- [ ]* 10.2 编写属性测试：列表排序一致性
  - **属性 6：列表排序一致性**
  - **验证：需求 2.2**

- [ ]* 10.3 编写属性测试：状态筛选完整性
  - **属性 8：状态筛选完整性**
  - **验证：需求 2.4**

- [ ]* 10.4 编写属性测试：关键词搜索包含性
  - **属性 11：关键词搜索包含性**
  - **验证：需求 2.7**

- [ ]* 10.5 编写属性测试：分页响应格式
  - **属性 12：分页响应格式**
  - **验证：需求 2.9**

- [x] 10.6 实现 InteractionsController
  - GET /api/customers/{customerId}/interactions（时间线查询）
  - POST /api/customers/{customerId}/interactions（创建）
  - PUT /api/interactions/{id}（更新，含 If-Match 支持）
  - DELETE /api/interactions/{id}（删除，含 If-Match 支持）
  - 设置 ETag 响应头
  - _需求：3.1-3.8, 10.3, 10.4, 10.5_

- [ ]* 10.7 编写属性测试：互动列表排序一致性
  - **属性 15：互动列表排序一致性**
  - **验证：需求 3.3**

- [ ]* 10.8 编写属性测试：乐观并发控制有效性
  - **属性 24：乐观并发控制有效性**
  - **验证：需求 10.3, 10.4**

- [x] 10.9 实现 AuthController（可选）
  - POST /api/auth/login
  - _需求：5.1, 5.4_

- [x] 10.10 实现 HealthController
  - GET /health
  - 检查数据库连接状态
  - 不暴露敏感配置
  - _需求：7.3, 7.4_

- [x] 11. 配置应用程序启动
- [x] 11.1 配置 Program.cs
  - 配置服务注册（DbContext, Repositories, Services）
  - 配置中间件管道
  - 配置认证（如果启用）
  - 配置 Serilog
  - 配置环境变量验证
  - 执行数据库迁移（如果 AUTO_MIGRATE=true）
  - 创建初始管理员用户（如果启用认证）
  - _需求：5.1-5.8, 7.8, 8.1-8.7_

- [ ]* 11.2 编写属性测试：成功响应格式一致性
  - **属性 22：成功响应格式一致性**
  - **验证：需求 6.1**

- [ ]* 11.3 编写属性测试：失败响应格式一致性
  - **属性 23：失败响应格式一致性**
  - **验证：需求 6.2**

- [x] 12. 创建 Docker 配置
- [x] 12.1 创建后端 Dockerfile
  - 多阶段构建
  - 优化镜像大小
  - _需求：8.1_

- [x] 12.2 创建 docker-compose.yml
  - 配置 db 服务（PostgreSQL 16）
  - 配置 api 服务（依赖 db）
  - 配置持久化卷
  - 配置健康检查
  - _需求：8.1, 8.6, 8.7_

- [ ] 13. 初始化前端项目
  - 创建 Vite + React + TypeScript 项目
  - 安装依赖（Ant Design, React Query, React Router, Axios, React Hook Form, Zod）
  - 配置项目结构（pages, components, api, types, hooks）
  - _需求：前端需求_

- [ ] 14. 实现前端 API 客户端
- [ ] 14.1 创建 TypeScript 类型定义
  - 定义 Customer, Interaction, User 类型
  - 定义枚举类型
  - 定义 API 响应类型
  - _需求：前端需求_

- [ ] 14.2 实现 API 客户端函数
  - 实现 Axios 实例配置
  - 实现客户相关 API 函数
  - 实现互动记录相关 API 函数
  - 实现认证相关 API 函数（可选）
  - 处理 ETag 和 If-Match 头
  - _需求：前端需求_

- [ ] 14.3 创建 React Query hooks
  - 创建 useCustomers, useCustomer, useCreateCustomer, useUpdateCustomer, useDeleteCustomer
  - 创建 useInteractions, useCreateInteraction, useUpdateInteraction, useDeleteInteraction
  - 配置缓存策略和乐观更新
  - _需求：前端需求_

- [ ] 15. 实现前端页面和组件
- [ ] 15.1 实现 CustomerListPage
  - 实现客户列表表格
  - 实现筛选器（状态、行业、来源）
  - 实现搜索框
  - 实现分页
  - 实现排序
  - _需求：前端需求_

- [ ] 15.2 实现 CustomerDetailPage
  - 实现客户信息卡片
  - 实现互动时间线
  - 实现编辑按钮
  - _需求：前端需求_

- [ ] 15.3 实现 CustomerFormPage
  - 实现客户创建/编辑表单
  - 实现表单验证
  - 实现提交处理
  - _需求：前端需求_

- [ ] 15.4 实现 InteractionForm 组件
  - 实现互动记录表单（弹窗）
  - 实现表单验证
  - 实现附件上传（占位）
  - _需求：前端需求_

- [ ] 15.5 实现 LoginPage（可选）
  - 实现登录表单
  - 实现令牌存储
  - _需求：5.1_

- [ ] 16. 实现前端路由
  - 配置 React Router
  - 实现路由保护（如果启用认证）
  - 实现 404 页面
  - _需求：前端需求_

- [ ] 17. 创建前端 Dockerfile
  - 多阶段构建（构建 + Nginx）
  - 配置 Nginx
  - _需求：8.1_

- [ ] 18. 集成测试
- [ ] 18.1 编写后端集成测试
  - 使用 Testcontainers 启动 PostgreSQL
  - 测试完整的 API 端点流程
  - 测试并发冲突场景
  - 测试认证流程（如果启用）
  - _需求：所有需求_

- [ ]* 18.2 编写前端集成测试
  - 使用 MSW mock API
  - 测试用户交互流程
  - 测试表单验证
  - _需求：前端需求_

- [ ] 19. 最终验证
  - 使用 docker-compose 启动完整系统
  - 验证所有功能正常工作
  - 验证健康检查端点
  - 验证日志记录
  - 验证数据持久化
  - _需求：所有需求_

- [ ] 20. 文档完善
  - 更新 README.md（包含启动说明、环境变量说明）
  - 添加 API 文档（Swagger）
  - 添加开发指南
  - _需求：文档需求_
