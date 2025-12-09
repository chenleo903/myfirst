using Microsoft.AspNetCore.Mvc;
using CrmSystem.Api.Data;

namespace CrmSystem.Api.Controllers;

/// <summary>
/// 健康检查控制器
/// </summary>
[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase
{
    private readonly CrmDbContext _context;
    private readonly ILogger<HealthController> _logger;

    public HealthController(
        CrmDbContext context,
        ILogger<HealthController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 健康检查端点
    /// GET /health
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetHealth(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Health check requested");

        var healthStatus = new HealthStatus
        {
            Status = "Healthy",
            Checks = new Dictionary<string, string>()
        };

        try
        {
            // Check database connection
            var canConnect = await _context.Database.CanConnectAsync(cancellationToken);
            healthStatus.Checks["database"] = canConnect ? "Healthy" : "Unhealthy";

            if (!canConnect)
            {
                healthStatus.Status = "Unhealthy";
                _logger.LogWarning("Health check: Database connection failed");
                return StatusCode(503, healthStatus);
            }

            _logger.LogDebug("Health check: All checks passed");
            return Ok(healthStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed with exception");
            
            healthStatus.Status = "Unhealthy";
            healthStatus.Checks["database"] = "Unhealthy";
            
            return StatusCode(503, healthStatus);
        }
    }
}

/// <summary>
/// 健康状态响应
/// </summary>
public class HealthStatus
{
    /// <summary>
    /// 整体健康状态
    /// </summary>
    public string Status { get; set; } = "Healthy";
    
    /// <summary>
    /// 各组件检查结果
    /// </summary>
    public Dictionary<string, string> Checks { get; set; } = new();
}
