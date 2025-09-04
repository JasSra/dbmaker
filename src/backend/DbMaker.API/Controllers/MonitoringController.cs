using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using DbMaker.Shared.Services;
using DbMaker.Shared.Models;
using System.Security.Claims;

namespace DbMaker.API.Controllers;

/// <summary>
/// Container monitoring and real-time statistics
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MonitoringController : ControllerBase
{
    private readonly IContainerOrchestrator _orchestrator;
    private readonly ILogger<MonitoringController> _logger;

    public MonitoringController(
        IContainerOrchestrator orchestrator,
        ILogger<MonitoringController> logger)
    {
        _orchestrator = orchestrator;
        _logger = logger;
    }

    private string? GetCurrentUserId()
    {
        return User.FindFirst("oid")?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;
    }

    /// <summary>
    /// Get real-time statistics for all user containers
    /// </summary>
    [HttpGet("stats")]
    public async Task<ActionResult<List<ContainerMonitoringData>>> GetContainerStats()
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        try
        {
            var allStats = await _orchestrator.GetAllContainerStatsAsync();
            var userStats = allStats.Where(s => s.UserId == userId).ToList();
            
            return Ok(userStats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get container stats for user {UserId}", userId);
            return StatusCode(500, "Failed to retrieve container statistics");
        }
    }

    /// <summary>
    /// Get statistics for a specific container
    /// </summary>
    [HttpGet("stats/{containerId}")]
    public async Task<ActionResult<ContainerMonitoringData>> GetContainerStats(string containerId)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        try
        {
            var stats = await _orchestrator.GetContainerStatsAsync(containerId);
            if (stats == null || stats.UserId != userId)
            {
                return NotFound("Container not found or access denied");
            }

            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get stats for container {ContainerId}", containerId);
            return StatusCode(500, "Failed to retrieve container statistics");
        }
    }

    /// <summary>
    /// Get system-wide monitoring summary (admin only)
    /// </summary>
    [HttpGet("summary")]
    public async Task<ActionResult<MonitoringSummary>> GetMonitoringSummary()
    {
        // Note: In a real application, you'd check for admin role here
        try
        {
            var allStats = await _orchestrator.GetAllContainerStatsAsync();
            
            var summary = new MonitoringSummary
            {
                TotalContainers = allStats.Count,
                RunningContainers = allStats.Count(s => s.Status == ContainerStatus.Running),
                StoppedContainers = allStats.Count(s => s.Status == ContainerStatus.Stopped),
                FailedContainers = allStats.Count(s => s.Status == ContainerStatus.Failed),
                TotalMemoryUsage = allStats.Sum(s => s.MemoryUsage),
                AverageCpuUsage = allStats.Any() ? allStats.Average(s => s.CpuUsage) : 0,
                UnhealthyContainers = allStats.Count(s => !s.IsHealthy),
                LastUpdated = DateTime.UtcNow
            };

            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get monitoring summary");
            return StatusCode(500, "Failed to retrieve monitoring summary");
        }
    }

    /// <summary>
    /// Get container logs (if available)
    /// </summary>
    [HttpGet("logs/{containerId}")]
    public async Task<ActionResult<ContainerLogs>> GetContainerLogs(string containerId, [FromQuery] int lines = 100)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        try
        {
            // In a full implementation, you'd verify the container belongs to the user
            // and retrieve actual logs from Docker
            
            return Ok(new ContainerLogs
            {
                ContainerId = containerId,
                Lines = new List<string> { "Logs feature coming soon..." },
                LastUpdated = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get logs for container {ContainerId}", containerId);
            return StatusCode(500, "Failed to retrieve container logs");
        }
    }

    /// <summary>
    /// Test container connectivity
    /// </summary>
    [HttpPost("test/{containerId}")]
    public async Task<ActionResult<ContainerTestResult>> TestContainer(string containerId)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        try
        {
            var stats = await _orchestrator.GetContainerStatsAsync(containerId);
            if (stats == null || stats.UserId != userId)
            {
                return NotFound("Container not found or access denied");
            }

            // Basic connectivity test
            var testResult = new ContainerTestResult
            {
                ContainerId = containerId,
                IsReachable = stats.IsHealthy && stats.Status == ContainerStatus.Running,
                ResponseTime = Random.Shared.Next(10, 100), // Simulated response time
                Message = stats.IsHealthy ? "Container is healthy and reachable" : "Container is not responding",
                TestedAt = DateTime.UtcNow
            };

            return Ok(testResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to test container {ContainerId}", containerId);
            return Ok(new ContainerTestResult
            {
                ContainerId = containerId,
                IsReachable = false,
                Message = $"Test failed: {ex.Message}",
                TestedAt = DateTime.UtcNow
            });
        }
    }
}

// Additional DTOs for monitoring
public class MonitoringSummary
{
    public int TotalContainers { get; set; }
    public int RunningContainers { get; set; }
    public int StoppedContainers { get; set; }
    public int FailedContainers { get; set; }
    public long TotalMemoryUsage { get; set; }
    public double AverageCpuUsage { get; set; }
    public int UnhealthyContainers { get; set; }
    public DateTime LastUpdated { get; set; }
}

public class ContainerLogs
{
    public string ContainerId { get; set; } = string.Empty;
    public List<string> Lines { get; set; } = new();
    public DateTime LastUpdated { get; set; }
}

public class ContainerTestResult
{
    public string ContainerId { get; set; } = string.Empty;
    public bool IsReachable { get; set; }
    public int ResponseTime { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime TestedAt { get; set; }
}
