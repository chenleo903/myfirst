CRM v1 可落地设计

目标：自用简洁 CRM；快速录入客户、跟踪沟通时间线；可 Docker 化一键启动；后续易扩展 AI/导入导出。

范围（MVP）：客户 CRUD + 筛选搜索；客户详情 + 时间线 CRUD；单用户登录或跳过；基础健康检查与日志。

技术栈：

后端：C# / ASP.NET Core 8 Web API，EF Core + Npgsql，FluentValidation，Serilog。
前端：React + TS + Vite，AntD（或 MUI），React Query，React Router。
DB：PostgreSQL；容器化：Docker + docker-compose（api/web/db）。
领域模型（受控枚举放配置/常量）：

Customer：Id (UUID)，CompanyName*，ContactName*，Wechat，Phone，Email，Industry，Source，Status（Lead/Contacted/NeedsAnalyzed/Quoted/Negotiating/Won/Lost），Tags (text[])，Score (int, 0-100)，LastInteractionAt，CreatedAt，UpdatedAt，IsDeleted。
Interaction：Id，CustomerId，HappenedAt*，Channel（Phone/Wechat/Email/Offline/Other），Stage（同 Status 或细分），Title*，Summary，RawContent，NextAction，Attachments (json/urls，可选)，CreatedAt，UpdatedAt。
User（可选）：Id，UserName，PasswordHash，Role，CreatedAt，UpdatedAt，LastLoginAt。
API 设计（REST，返回统一 {success,data,errors}）：

GET /api/customers?page&pageSize&status&industry&source&keyword → 列表（按 LastInteractionAt desc 默认）。
GET /api/customers/{id} → 详情（含基本信息+LastInteractionAt）。
POST /api/customers → 创建（校验必填）。
PUT /api/customers/{id} → 更新。
DELETE /api/customers/{id} → 软删。
GET /api/customers/{id}/interactions → 时间线，按 HappenedAt desc。
POST /api/customers/{id}/interactions → 新增记录。
PUT /api/interactions/{id} → 更新（乐观并发，用 If-Match/rowversion 或 UpdatedAt 比对）。
DELETE /api/interactions/{id} → 删除。
可选鉴权：POST /api/auth/login 返回 JWT；其他接口需 Bearer。
数据库要点（EF Migration 管理）：

唯一：CompanyName + ContactName（或单 CompanyName）UNIQUE，用户名 UNIQUE。
索引：customers(status, industry, source)，customers(last_interaction_at)，interactions(customer_id, happened_at)。
软删：IsDeleted + 查询过滤。
时间统一 UTC，前端展示转换。
架构/分层（后端）：

Controllers（薄） → Services（业务） → Repositories/DbContext。
DTO 与 Entity 分离；AutoMapper 可选。
中间件：异常处理、请求日志、CORS、认证授权。
配置：环境变量注入连接串/JWT 密钥；不要写死。
前端信息架构：

路由：/login（可选），/customers，/customers/new，/customers/:id，/customers/:id/edit。
列表：表格 + 筛选（状态/行业/来源）+ 搜索；行点击进详情；空态/加载/错误态。
详情：左侧信息卡（编辑按钮），右侧时间线；时间线节点显示时间、Stage/Channel、Title、Summary；支持新增/编辑/删除弹窗表单。
状态管理：React Query（缓存/失效策略），乐观更新或提交后失效；URL query 保持筛选状态。
非功能：

安全：PBKDF2/BCrypt/Argon2，JWT 过期 + 刷新（可后续）；CORS 白名单；简单限流（AspNetCoreRateLimit）。
观察性：Serilog 结构化日志，健康检查 /health。
运维：docker-compose 一键拉起；api 启动前自动执行 dotnet ef database update。
备份：Postgres volume + 定期 dump（后续脚本化）。
docker-compose 草案（示意）：

version: '3.9'
services:
  db:
    image: postgres:16
    environment:
      POSTGRES_USER: crm_user
      POSTGRES_PASSWORD: crm_pass
      POSTGRES_DB: crm_db
    volumes: [crm_db_data:/var/lib/postgresql/data]
    ports: ["5432:5432"]
  api:
    build: ./src/Api
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      ConnectionStrings__Default: Host=db;Port=5432;Database=crm_db;Username=crm_user;Password=crm_pass
    depends_on: [db]
    ports: ["5000:8080"]
  web:
    build: ./frontend
    depends_on: [api]
    ports: ["3000:80"]
volumes:
  crm_db_data:
开发流程建议：
初始化后端解决方案 + EF 模型 + 首次迁移；跑通 docker-compose（api+db）。
起前端 Vite + AntD，完成列表/详情/时间线骨架；接通 API。
加鉴权（如需要）；完善日志/健康检查；打通自动迁移。
补导入导出/附件/AI 扩展（后续）。




