using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using DbMaker.Shared.Data;
using DbMaker.Shared.Models;
using DbMaker.Shared.Services;
using System.Security.Claims;

namespace DbMaker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ContainersController : ControllerBase
{
    private readonly DbMakerDbContext _context;
    private readonly IContainerOrchestrator _orchestrator;
    private readonly ILogger<ContainersController> _logger;

    public ContainersController(
        DbMakerDbContext context,
        IContainerOrchestrator orchestrator,
        ILogger<ContainersController> logger)
    {
        _context = context;
        _orchestrator = orchestrator;
        _logger = logger;
    }

    private string GetCurrentUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? 
               User.FindFirst("sub")?.Value ?? 
               User.FindFirst("oid")?.Value ?? 
               throw new UnauthorizedAccessException("User ID not found");
    }

    [HttpGet]
    public async Task<ActionResult<List<ContainerResponse>>> GetContainers()
    {
        var userId = GetCurrentUserId();
        
        var containers = await _context.DatabaseContainers
            .Where(c => c.UserId == userId)
            .Select(c => new ContainerResponse
            {
                Id = c.Id,
                Name = c.Name,
                DatabaseType = c.DatabaseType,
                ConnectionString = c.ConnectionString,
                Status = c.Status,
                Subdomain = c.Subdomain,
                Port = c.Port,
                CreatedAt = c.CreatedAt,
                Configuration = c.Configuration
            })
            .ToListAsync();

        return Ok(containers);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ContainerResponse>> GetContainer(string id)
    {
        var userId = GetCurrentUserId();
        
        var container = await _context.DatabaseContainers
            .Where(c => c.Id == id && c.UserId == userId)
            .Select(c => new ContainerResponse
            {
                Id = c.Id,
                Name = c.Name,
                DatabaseType = c.DatabaseType,
                ConnectionString = c.ConnectionString,
                Status = c.Status,
                Subdomain = c.Subdomain,
                Port = c.Port,
                CreatedAt = c.CreatedAt,
                Configuration = c.Configuration
            })
            .FirstOrDefaultAsync();

        if (container == null)
            return NotFound();

        return Ok(container);
    }

    [HttpPost]
    public async Task<ActionResult<ContainerResponse>> CreateContainer(CreateContainerRequest request)
    {
        var userId = GetCurrentUserId();

        // Ensure user exists
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            user = new User
            {
                Id = userId,
                Email = User.FindFirst(ClaimTypes.Email)?.Value ?? "unknown@domain.com",
                Name = User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown User"
            };
            _context.Users.Add(user);
        }

        // Check if container name already exists for this user
        var existingContainer = await _context.DatabaseContainers
            .AnyAsync(c => c.UserId == userId && c.Name == request.Name);

        if (existingContainer)
            return BadRequest("Container with this name already exists");

        try
        {
            // Create container using orchestrator
            var container = await _orchestrator.CreateContainerAsync(userId, request);

            // Save to database
            _context.DatabaseContainers.Add(container);
            await _context.SaveChangesAsync();

            var response = new ContainerResponse
            {
                Id = container.Id,
                Name = container.Name,
                DatabaseType = container.DatabaseType,
                ConnectionString = container.ConnectionString,
                Status = container.Status,
                Subdomain = container.Subdomain,
                Port = container.Port,
                CreatedAt = container.CreatedAt,
                Configuration = container.Configuration
            };

            return CreatedAtAction(nameof(GetContainer), new { id = container.Id }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create container for user {UserId}", userId);
            return StatusCode(500, "Failed to create container");
        }
    }

    [HttpPost("{id}/start")]
    public async Task<ActionResult> StartContainer(string id)
    {
        var userId = GetCurrentUserId();
        
        var container = await _context.DatabaseContainers
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

        if (container == null)
            return NotFound();

        var success = await _orchestrator.StartContainerAsync(container.ContainerId);
        if (success)
        {
            container.Status = AppContainerStatus.Running;
            await _context.SaveChangesAsync();
            return Ok();
        }

        return StatusCode(500, "Failed to start container");
    }

    [HttpPost("{id}/stop")]
    public async Task<ActionResult> StopContainer(string id)
    {
        var userId = GetCurrentUserId();
        
        var container = await _context.DatabaseContainers
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

        if (container == null)
            return NotFound();

        var success = await _orchestrator.StopContainerAsync(container.ContainerId);
        if (success)
        {
            container.Status = AppContainerStatus.Stopped;
            await _context.SaveChangesAsync();
            return Ok();
        }

        return StatusCode(500, "Failed to stop container");
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteContainer(string id)
    {
        var userId = GetCurrentUserId();
        
        var container = await _context.DatabaseContainers
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

        if (container == null)
            return NotFound();

        try
        {
            // Remove from Docker
            await _orchestrator.RemoveContainerAsync(container.ContainerId);
            
            // Remove from database
            _context.DatabaseContainers.Remove(container);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete container {ContainerId}", id);
            return StatusCode(500, "Failed to delete container");
        }
    }

    [HttpGet("{id}/stats")]
    public async Task<ActionResult<ContainerMonitoringData>> GetContainerStats(string id)
    {
        var userId = GetCurrentUserId();
        
        var container = await _context.DatabaseContainers
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

        if (container == null)
            return NotFound();

        var stats = await _orchestrator.GetContainerStatsAsync(container.ContainerId);
        if (stats == null)
            return NotFound("Container stats not available");

        return Ok(stats);
    }
}
