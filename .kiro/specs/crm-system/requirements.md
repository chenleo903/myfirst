# 需求文档

## 简介

本系统是一个简洁的自用 CRM（客户关系管理）系统，旨在快速录入客户信息、跟踪沟通时间线，支持 Docker 化一键启动，并为后续 AI 功能和数据导入导出预留扩展空间。MVP 版本聚焦于核心的客户管理和互动记录功能。

## 术语表

- **CRM系统**：客户关系管理系统，本文档中指代整个应用程序
- **客户实体**：系统中存储的客户信息记录
- **互动记录**：与客户的沟通交互历史记录
- **用户实体**：系统使用者的账户信息
- **软删除**：标记记录为已删除但不从数据库中物理删除
- **时间线**：按时间顺序排列的互动记录列表
- **乐观并发控制**：使用版本字段检测并发更新冲突的机制
- **版本标识**：用于并发控制的 UpdatedAt 时间戳字段
- **认证模式**：系统可配置为启用 JWT 认证或跳过认证的运行模式

## 需求

### 需求 1：客户信息管理

**用户故事：** 作为 CRM 用户，我希望能够创建、查看、更新和删除客户信息，以便管理我的客户数据库。

#### 验收标准

1. WHEN 用户提交包含公司名称和联系人姓名的客户创建请求 THEN CRM系统 SHALL 创建新的客户记录并返回 201 状态码和包含唯一标识符的客户信息
2. WHEN 用户请求查看特定客户详情 THEN CRM系统 SHALL 返回 200 状态码和该客户的完整信息包括基本字段和最后互动时间
3. WHEN 用户提交客户信息更新请求 THEN CRM系统 SHALL 更新指定客户的字段并返回 200 状态码和更新后的 UpdatedAt 时间戳
4. WHEN 用户请求删除客户 THEN CRM系统 SHALL 将该客户的 IsDeleted 标志设置为 true 并返回 204 状态码
5. WHEN 用户请求删除客户 THEN CRM系统 SHALL 保留该客户的所有互动记录
6. WHEN 查询客户列表或互动列表 THEN CRM系统 SHALL 自动过滤 IsDeleted 为 true 的客户及其关联数据
7. WHEN 用户尝试创建缺少必填字段的客户 THEN CRM系统 SHALL 返回 400 状态码和字段级验证错误信息
8. WHEN 用户请求不存在或已删除的客户 THEN CRM系统 SHALL 返回 404 状态码

### 需求 2：客户列表查询与筛选

**用户故事：** 作为 CRM 用户，我希望能够查看客户列表并按不同条件筛选，以便快速找到目标客户。

#### 验收标准

1. WHEN 用户请求客户列表且未指定分页参数 THEN CRM系统 SHALL 使用默认值 page 为 1 和 pageSize 为 20 返回未删除的客户记录
2. WHEN 用户请求客户列表且未指定排序参数 THEN CRM系统 SHALL 返回未删除的客户记录按 LastInteractionAt 降序排列
3. WHEN 用户指定 sortBy 参数为 CreatedAt 或 UpdatedAt THEN CRM系统 SHALL 按指定字段排序并支持 asc 或 desc 方向
4. WHEN 用户指定状态筛选条件 THEN CRM系统 SHALL 验证状态值在允许的枚举范围内并仅返回匹配该状态的客户记录
5. WHEN 用户指定行业筛选条件 THEN CRM系统 SHALL 仅返回匹配该行业的客户记录
6. WHEN 用户指定来源筛选条件 THEN CRM系统 SHALL 仅返回匹配该来源的客户记录
7. WHEN 用户提供关键词搜索 THEN CRM系统 SHALL 执行大小写不敏感的搜索并返回公司名称或联系人姓名任一包含该关键词的客户记录
8. WHEN 用户指定 pageSize 超过 100 THEN CRM系统 SHALL 返回 400 状态码和错误信息
9. WHEN 用户指定分页参数 THEN CRM系统 SHALL 返回 data 对象包含 items 数组和 total 字段

### 需求 3：互动记录管理

**用户故事：** 作为 CRM 用户，我希望能够为客户添加、查看、更新和删除互动记录，以便跟踪沟通历史。

#### 验收标准

1. WHEN 用户为特定客户提交包含发生时间、渠道和标题的互动记录 THEN CRM系统 SHALL 创建新的互动记录并返回 201 状态码
2. WHEN 用户创建互动记录 THEN CRM系统 SHALL 更新关联客户的 LastInteractionAt 字段为该互动的 HappenedAt 值
3. WHEN 用户请求特定客户的互动记录列表 THEN CRM系统 SHALL 仅返回未删除客户的互动记录按 HappenedAt 降序排列
4. WHEN 用户提交互动记录更新请求 THEN CRM系统 SHALL 更新指定互动记录的字段并返回 200 状态码和更新后的 UpdatedAt 时间戳
5. WHEN 用户请求删除互动记录 THEN CRM系统 SHALL 从数据库中物理删除该记录并返回 204 状态码
6. WHEN 用户删除互动记录 THEN CRM系统 SHALL 重新计算关联客户的 LastInteractionAt 为剩余最新互动的 HappenedAt 或 null
7. WHEN 用户尝试创建缺少必填字段的互动记录 THEN CRM系统 SHALL 返回 400 状态码和字段级验证错误信息
8. WHEN 用户尝试为已删除客户创建互动记录 THEN CRM系统 SHALL 返回 404 状态码

### 需求 4：数据完整性与约束

**用户故事：** 作为系统管理员，我希望系统强制执行数据完整性规则，以便保持数据质量和一致性。

#### 验收标准

1. WHEN 用户尝试创建与现有未删除客户相同公司名称和联系人姓名的客户 THEN CRM系统 SHALL 返回 409 状态码和唯一性约束错误
2. WHEN 系统存储任何时间戳 THEN CRM系统 SHALL 使用 UTC 时区和 ISO 8601 格式存储
3. WHEN API 返回任何时间戳 THEN CRM系统 SHALL 使用 ISO 8601 格式输出
4. WHEN 查询客户列表 THEN CRM系统 SHALL 自动排除 IsDeleted 为 true 的记录
5. WHEN 创建或更新客户评分 THEN CRM系统 SHALL 验证评分值在 0 到 100 之间否则返回 400 状态码
6. WHEN 创建或更新客户状态 THEN CRM系统 SHALL 验证状态值为 Lead、Contacted、NeedsAnalyzed、Quoted、Negotiating、Won 或 Lost 之一
7. WHEN 创建或更新互动渠道 THEN CRM系统 SHALL 验证渠道值为 Phone、Wechat、Email、Offline 或 Other 之一
8. WHEN 创建或更新客户字段 THEN CRM系统 SHALL 验证 CompanyName 和 ContactName 长度不超过 200 字符
9. WHEN 创建或更新客户 Email THEN CRM系统 SHALL 验证 Email 格式符合标准电子邮件格式或为空
10. WHEN 创建互动记录 THEN CRM系统 SHALL 验证 CustomerId 引用存在的未删除客户否则返回 404 状态码

### 需求 5：用户认证（可选功能）

**用户故事：** 作为系统管理员，我希望能够控制系统访问权限，以便保护客户数据安全。

#### 验收标准

1. WHEN 系统启动 THEN CRM系统 SHALL 从环境变量 ENABLE_AUTH 读取认证模式配置默认值为 false
2. WHERE 启用认证功能 WHEN 系统首次启动且数据库无用户记录 THEN CRM系统 SHALL 从环境变量 ADMIN_USERNAME 和 ADMIN_PASSWORD 创建初始管理员用户
3. WHERE 启用认证功能且未配置初始管理员环境变量 WHEN 系统首次启动 THEN CRM系统 SHALL 记录错误日志并阻止启动
4. WHERE 启用认证功能 WHEN 用户提交有效的用户名和密码 THEN CRM系统 SHALL 返回 200 状态码和包含 JWT 访问令牌的响应
5. WHERE 启用认证功能 WHEN 用户访问客户或互动相关的 API 端点且未提供有效令牌 THEN CRM系统 SHALL 返回 401 状态码
6. WHERE 启用认证功能 WHEN 用户提供过期的 JWT 令牌 THEN CRM系统 SHALL 返回 401 状态码
7. WHERE 启用认证功能 WHEN 创建新用户 THEN CRM系统 SHALL 使用 BCrypt 算法存储密码哈希
8. WHERE 禁用认证功能 WHEN 用户访问任何 API 端点 THEN CRM系统 SHALL 完全跳过认证和授权中间件

### 需求 6：API 响应格式

**用户故事：** 作为前端开发者，我希望所有 API 响应遵循统一格式并使用正确的 HTTP 状态码，以便简化客户端处理逻辑。

#### 验收标准

1. WHEN API 请求成功且返回 2xx 状态码 THEN CRM系统 SHALL 返回 JSON 格式为 { success: true, data: object, errors: [] }
2. WHEN API 请求失败且返回 4xx 或 5xx 状态码 THEN CRM系统 SHALL 返回 JSON 格式为 { success: false, data: null, errors: [{field?, message}] }
3. WHEN API 创建资源成功 THEN CRM系统 SHALL 返回 201 状态码和 Location 头指向新资源的 URI
4. WHEN API 删除资源成功 THEN CRM系统 SHALL 返回 204 状态码和空响应体
5. WHEN API 返回分页列表 THEN CRM系统 SHALL 在 data 中包含 items 数组和 total 字段表示总记录数
6. WHEN API 发生验证错误 THEN CRM系统 SHALL 返回 400 状态码和 errors 数组包含 field 和 message 字段
7. WHEN API 发生唯一性约束冲突 THEN CRM系统 SHALL 返回 409 状态码和 errors 数组包含冲突详情
8. WHEN API 发生并发更新冲突 THEN CRM系统 SHALL 返回 409 状态码和 errors 数组包含当前版本信息
9. WHEN API 发生服务器错误 THEN CRM系统 SHALL 返回 500 状态码和通用错误消息而不在 errors 中包含堆栈跟踪或内部异常细节

### 需求 7：系统可观测性

**用户故事：** 作为运维人员，我希望系统提供日志和健康检查功能，以便监控系统状态和排查问题。

#### 验收标准

1. WHEN 系统处理任何 API 请求 THEN CRM系统 SHALL 记录结构化日志包含请求路径、方法、状态码和处理时间
2. WHEN 系统记录包含手机号的日志 THEN CRM系统 SHALL 仅保留后 4 位数字其余替换为星号
3. WHEN 系统记录包含邮箱的日志 THEN CRM系统 SHALL 仅保留 @ 符号前 2 个字符和域名其余替换为星号
4. WHEN 系统记录包含微信号的日志 THEN CRM系统 SHALL 仅保留前 2 个和后 2 个字符其余替换为星号
5. WHEN 系统记录请求体日志 THEN CRM系统 SHALL 对 Phone、Email 和 Wechat 字段应用脱敏规则
6. WHEN 系统发生异常 THEN CRM系统 SHALL 记录包含异常类型、消息和堆栈跟踪的错误日志
7. WHEN 外部服务请求健康检查端点 THEN CRM系统 SHALL 返回系统健康状态包括数据库连接状态但不包含连接字符串或密码
8. WHEN 系统启动 THEN CRM系统 SHALL 记录启动时间和非敏感配置信息到日志

### 需求 8：容器化部署

**用户故事：** 作为运维人员，我希望能够通过 Docker Compose 一键启动整个系统，以便简化部署流程。

#### 验收标准

1. WHEN 运维人员执行 docker-compose up 命令 THEN CRM系统 SHALL 启动数据库、后端 API 和前端 Web 服务
2. WHEN API 服务启动且环境变量 AUTO_MIGRATE 为 true THEN CRM系统 SHALL 在单个实例中执行数据库迁移
3. WHEN 数据库迁移失败且 AUTO_MIGRATE 为 true THEN CRM系统 SHALL 记录错误日志并阻止 API 服务启动
4. WHERE 环境变量 AUTO_MIGRATE 为 false WHEN API 服务启动 THEN CRM系统 SHALL 跳过自动迁移并记录日志提示手动执行迁移命令
5. WHEN 多个 API 实例同时启动且 AUTO_MIGRATE 为 true THEN CRM系统 SHALL 使用数据库锁机制确保仅一个实例执行迁移
6. WHEN 服务之间需要通信 THEN CRM系统 SHALL 使用 Docker 网络内部主机名进行连接
7. WHEN 数据库服务重启 THEN CRM系统 SHALL 保留数据通过持久化卷存储

### 需求 9：数据库性能优化

**用户故事：** 作为系统架构师，我希望数据库查询性能良好，以便支持快速的用户操作响应。

#### 验收标准

1. WHEN 系统创建数据库表 THEN CRM系统 SHALL 在 customers 表的 status、industry 和 source 字段上创建索引
2. WHEN 系统创建数据库表 THEN CRM系统 SHALL 在 customers 表的 last_interaction_at 字段上创建索引
3. WHEN 系统创建数据库表 THEN CRM系统 SHALL 在 interactions 表的 customer_id 和 happened_at 字段上创建复合索引
4. WHEN 查询客户列表使用筛选条件 THEN CRM系统 SHALL 利用索引提高查询性能

### 需求 10：并发控制

**用户故事：** 作为 CRM 用户，我希望系统能够处理并发更新，以便避免数据覆盖问题。

#### 验收标准

1. WHEN 多个用户同时更新同一互动记录或客户记录 THEN CRM系统 SHALL 使用 UpdatedAt 字段作为版本标识进行乐观并发控制
2. WHEN 系统存储或返回 UpdatedAt 字段 THEN CRM系统 SHALL 使用 UTC 时区和 ISO 8601 格式精确到毫秒
3. WHEN 客户端执行 PUT 或 PATCH 请求更新资源时提供 If-Match 头包含 ISO 8601 格式的 UpdatedAt 值 THEN CRM系统 SHALL 验证版本匹配后再执行更新
4. WHEN 检测到并发更新冲突 THEN CRM系统 SHALL 返回 409 状态码和 errors 数组包含当前 UpdatedAt 值
5. WHEN 客户端执行 DELETE 请求时提供 If-Match 头 THEN CRM系统 SHALL 验证版本匹配后再执行删除
6. WHEN 客户端更新或删除资源时未提供 If-Match 头 THEN CRM系统 SHALL 执行操作但记录警告日志
