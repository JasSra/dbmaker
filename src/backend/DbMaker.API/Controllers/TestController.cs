using Microsoft.AspNetCore.Mvc;
using DbMaker.Shared.Services;
using DbMaker.Shared.Models;

namespace DbMaker.API.Controllers;

[ApiController]
[Route("api/test")]
public class TestController : ControllerBase
{
    private readonly IContainerOrchestrator _orchestrator;
    private readonly ILogger<TestController> _logger;

    public TestController(
        IContainerOrchestrator orchestrator,
        ILogger<TestController> logger)
    {
        _orchestrator = orchestrator;
        _logger = logger;
    }

    [HttpPost("create-container")]
    public async Task<ActionResult> TestCreateContainer([FromBody] TestCreateContainerRequest request)
    {
        try
        {
            _logger.LogInformation("Testing container creation for type: {DatabaseType}, name: {Name}", request.DatabaseType, request.Name);

            var createRequest = new CreateContainerRequest
            {
                Name = request.Name,
                DatabaseType = request.DatabaseType,
                Configuration = request.Configuration ?? new Dictionary<string, string>()
            };

            var container = await _orchestrator.CreateContainerAsync("test-user-123", createRequest);

            return Ok(new
            {
                Success = true,
                Container = new
                {
                    container.Id,
                    container.Name,
                    container.DatabaseType,
                    container.ContainerId,
                    container.Port,
                    container.Subdomain,
                    container.ConnectionString,
                    container.Status
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create test container");
            return StatusCode(500, new { Error = ex.Message, Details = ex.ToString() });
        }
    }

    [HttpGet("port")]
    public ActionResult GetAvailablePort()
    {
        try
        {
            var port = _orchestrator.GetAvailablePort();
            return Ok(new { Port = port });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get available port");
            return StatusCode(500, new { Error = ex.Message });
        }
    }

    [HttpGet("subdomain/{userId}/{containerName}/{databaseType}")]
    public ActionResult GenerateSubdomain(string userId, string containerName, string databaseType)
    {
        try
        {
            var subdomain = _orchestrator.GenerateSubdomain(userId, containerName, databaseType);
            return Ok(new { Subdomain = subdomain });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate subdomain");
            return StatusCode(500, new { Error = ex.Message });
        }
    }

    [HttpGet("docker-status")]
    public async Task<ActionResult> GetDockerStatus()
    {
        try
        {
            // Try to get container stats to test Docker connectivity
            var stats = await _orchestrator.GetAllContainerStatsAsync();
            return Ok(new { 
                DockerConnected = true,
                ContainerCount = stats.Count,
                Message = "Docker is accessible"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Docker connectivity test failed");
            return Ok(new { 
                DockerConnected = false,
                Error = ex.Message,
                Message = "Docker is not accessible"
            });
        }
    }
}

public class TestCreateContainerRequest
{
    public string Name { get; set; } = string.Empty;
    public string DatabaseType { get; set; } = string.Empty;
    public Dictionary<string, string>? Configuration { get; set; }
}
