using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DbMaker.Shared.Data;
using DbMaker.Shared.Services;

namespace DbMaker.API.Controllers;

[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase
{
    private readonly DbMakerDbContext _context;
    private readonly IContainerOrchestrator _orchestrator;
    private readonly ILogger<HealthController> _logger;

    public HealthController(DbMakerDbContext context, IContainerOrchestrator orchestrator, ILogger<HealthController> logger)
    {
        _context = context;
        _orchestrator = orchestrator;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult> GetHealth()
    {
        var healthStatus = new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            version = "1.0.0",
            database = "unknown",
            docker = "unknown",
            containers = new { running = 0, total = 0 }
        };

        try
        {
            // Test database connection
            await _context.Database.CanConnectAsync();
            healthStatus = healthStatus with { database = "connected" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed");
            healthStatus = healthStatus with { database = $"failed: {ex.Message}" };
        }

        try
        {
            // Test Docker connectivity
            var stats = await _orchestrator.GetAllContainerStatsAsync();
            var runningCount = stats.Count(s => s.Status == DbMaker.Shared.Models.ContainerStatus.Running);
            
            healthStatus = healthStatus with 
            { 
                docker = "connected",
                containers = new { running = runningCount, total = stats.Count }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Docker health check failed");
            healthStatus = healthStatus with { docker = $"failed: {ex.Message}" };
        }

        var isHealthy = healthStatus.database == "connected" && healthStatus.docker == "connected";
        var statusCode = isHealthy ? 200 : 503;
        
        return StatusCode(statusCode, healthStatus with { status = isHealthy ? "healthy" : "unhealthy" });
    }
}
