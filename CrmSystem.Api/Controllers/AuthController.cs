using Microsoft.AspNetCore.Mvc;
using CrmSystem.Api.DTOs;
using CrmSystem.Api.Services;

namespace CrmSystem.Api.Controllers;

/// <summary>
/// 认证控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthService authService,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// 用户登录
    /// POST /api/auth/login
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Login attempt for user: {Username}", request.Username);

        try
        {
            var (token, expiresAt) = await _authService.LoginAsync(
                request.Username, 
                request.Password, 
                cancellationToken);

            return Ok(new ApiResponse<LoginResponse>
            {
                Success = true,
                Data = new LoginResponse
                {
                    Token = token,
                    ExpiresAt = expiresAt
                },
                Errors = new List<ErrorDetail>()
            });
        }
        catch (UnauthorizedAccessException)
        {
            _logger.LogWarning("Failed login attempt for user: {Username}", request.Username);
            
            return Unauthorized(new ApiResponse<LoginResponse>
            {
                Success = false,
                Data = null,
                Errors = new List<ErrorDetail>
                {
                    new() { Message = "Invalid username or password" }
                }
            });
        }
    }
}

/// <summary>
/// 登录请求 DTO
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// 用户名
    /// </summary>
    public string Username { get; set; } = string.Empty;
    
    /// <summary>
    /// 密码
    /// </summary>
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// 登录响应 DTO
/// </summary>
public class LoginResponse
{
    /// <summary>
    /// JWT 访问令牌
    /// </summary>
    public string Token { get; set; } = string.Empty;
    
    /// <summary>
    /// 令牌过期时间
    /// </summary>
    public DateTimeOffset ExpiresAt { get; set; }
}
