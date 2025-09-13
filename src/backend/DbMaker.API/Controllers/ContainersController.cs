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
// TEMP: Allow anonymous during local dev to unblock UI
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

    private string? GetCurrentUserId()
    {
        // Prefer Azure AD B2C OID; fallback to sub/nameidentifier
        return User.FindFirst("oid")?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;
    }

    [HttpGet]
     // TEMPORARY: Remove auth to debug
    public async Task<ActionResult<List<ContainerResponse>>> GetContainers()
    {
    var userId = GetCurrentUserId() ?? "test-user-123";
    // Temporarily disable auth check
    // if (string.IsNullOrEmpty(userId)) return Unauthorized();
        
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
    if (string.IsNullOrEmpty(userId)) return Unauthorized();
        
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
     // TEMPORARY: Remove auth to debug creation
    public async Task<ActionResult<ContainerResponse>> CreateContainer(CreateContainerRequest request)
    {
        var userId = GetCurrentUserId() ?? "test-user-123";

        // Ensure user exists with proper error handling for unique constraints
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? $"user-{userId}@dbmaker.local";
            var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? $"User {userId}";

            // Check if email already exists and generate unique one if needed
            var baseEmail = userEmail;
            var counter = 1;
            
            while (await _context.Users.AnyAsync(u => u.Email == userEmail))
            {
                var emailParts = baseEmail.Split('@');
                if (emailParts.Length == 2)
                {
                    userEmail = $"{emailParts[0]}-{counter}@{emailParts[1]}";
                }
                else
                {
                    userEmail = $"user-{userId}-{counter}@dbmaker.local";
                }
                counter++;
            }

            user = new User
            {
                Id = userId,
                Email = userEmail,
                Name = userName
            };
            
            try
            {
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Created new user {UserId} with email {Email}", userId, userEmail);
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UNIQUE constraint failed") == true)
            {
                // Handle race condition where another request created the user
                _logger.LogWarning("User creation failed due to unique constraint, attempting to find existing user");
                user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    _logger.LogError("Failed to create or find user {UserId}", userId);
                    return StatusCode(500, "Failed to create or retrieve user");
                }
            }
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
    if (string.IsNullOrEmpty(userId)) return Unauthorized();
        
        var container = await _context.DatabaseContainers
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

        if (container == null)
            return NotFound();

        var success = await _orchestrator.StartContainerAsync(container.ContainerId);
        if (success)
        {
            container.Status = ContainerStatus.Running;
            await _context.SaveChangesAsync();
            return Ok();
        }

        return StatusCode(500, "Failed to start container");
    }

    [HttpPost("{id}/stop")]
    public async Task<ActionResult> StopContainer(string id)
    {
    var userId = GetCurrentUserId();
    if (string.IsNullOrEmpty(userId)) return Unauthorized();
        
        var container = await _context.DatabaseContainers
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

        if (container == null)
            return NotFound();

        var success = await _orchestrator.StopContainerAsync(container.ContainerId);
        if (success)
        {
            container.Status = ContainerStatus.Stopped;
            await _context.SaveChangesAsync();
            return Ok();
        }

        return StatusCode(500, "Failed to stop container");
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteContainer(string id)
    {
    var userId = GetCurrentUserId();
    if (string.IsNullOrEmpty(userId)) return Unauthorized();
        
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
    if (string.IsNullOrEmpty(userId)) return Unauthorized();
        
        var container = await _context.DatabaseContainers
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

        if (container == null)
            return NotFound();

        var stats = await _orchestrator.GetContainerStatsAsync(container.ContainerId);
        if (stats == null)
            return NotFound("Container stats not available");

        return Ok(stats);
    }

    [HttpGet("all-debug")]
    public async Task<IActionResult> GetAllContainersDebug()
    {
        try
        {
            // Get all containers from database
            var dbContainers = await _context.DatabaseContainers.ToListAsync();
            
            // Get Docker container stats
            var dockerStats = await _orchestrator.GetAllContainerStatsAsync();
            
            return Ok(new
            {
                message = "Debug container listing",
                timestamp = DateTime.UtcNow,
                totalDbContainers = dbContainers.Count,
                totalDockerContainers = dockerStats.Count,
                databaseContainers = dbContainers.Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.ContainerId,
                    c.Status,
                    c.DatabaseType,
                    c.Port,
                    c.CreatedAt,
                    c.UserId
                }),
                dockerContainerStats = dockerStats.Select(s => new
                {
                    s.ContainerId,
                    s.Status,
                    s.IsHealthy,
                    s.CpuUsage,
                    s.MemoryUsage,
                    s.UserId,
                    s.Timestamp
                })
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
        }
    }

    [HttpPost("create-demo")]
    public async Task<IActionResult> CreateDemoContainer()
    {
        try
        {
            // First ensure demo user exists with proper unique email handling
            var demoUserId = "demo-user";
            var demoUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == demoUserId);
            
            if (demoUser == null)
            {
                // Check if email already exists and generate unique one if needed
                var baseEmail = "demo@devdb.local";
                var email = baseEmail;
                var counter = 1;
                
                while (await _context.Users.AnyAsync(u => u.Email == email))
                {
                    email = $"demo{counter}@devdb.local";
                    counter++;
                }

                demoUser = new User
                {
                    Id = demoUserId,
                    Email = email,
                    Name = "Demo User",
                    CreatedAt = DateTime.UtcNow,
                    LastLoginAt = DateTime.UtcNow,
                    IsActive = true
                };
                
                _context.Users.Add(demoUser);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Created demo user with email {Email}", email);
            }

            // Check if we already have demo containers
            var existingContainers = await _context.DatabaseContainers
                .Where(c => c.UserId == demoUserId)
                .Select(c => c.Name)
                .ToListAsync();
            var existingCount = existingContainers.Count;

            DatabaseContainer demoContainer;
            
            if (existingCount == 0)
            {
                // Create PostgreSQL demo container with realistic port allocation
                var postgresPort = await _orchestrator.GetAvailablePortAsync();
                demoContainer = new DatabaseContainer
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = demoUserId,
                    Name = "demo-postgres",
                    DatabaseType = "postgresql", 
                    ContainerId = "demo-pg-" + Guid.NewGuid().ToString("N")[..6],
                    ContainerName = "demo-postgres-container",
                    Status = ContainerStatus.Running,
                    Port = postgresPort,
                    Subdomain = "demo-postgres",
                    ConnectionString = $"Host=localhost;Port={postgresPort};Database=demoDb;Username=postgres;Password=password",
                    Configuration = new Dictionary<string, string>
                    {
                        {"POSTGRES_DB", "demoDb"},
                        {"POSTGRES_USER", "postgres"},
                        {"POSTGRES_PASSWORD", "password"}
                    },
                    CreatedAt = DateTime.UtcNow,
                    LastAccessedAt = DateTime.UtcNow
                };
            }
            else if (existingCount == 1)
            {
                // Create Redis demo container with realistic port allocation
                var redisPort = await _orchestrator.GetAvailablePortAsync();
                demoContainer = new DatabaseContainer
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = demoUserId,
                    Name = "demo-redis",
                    DatabaseType = "redis", 
                    ContainerId = "demo-rd-" + Guid.NewGuid().ToString("N")[..6],
                    ContainerName = "demo-redis-container",
                    Status = ContainerStatus.Running,
                    Port = redisPort,
                    Subdomain = "demo-redis",
                    ConnectionString = $"redis://localhost:{redisPort}",
                    Configuration = new Dictionary<string, string>
                    {
                        {"REDIS_PASSWORD", "redis123"},
                        {"REDIS_MAXMEMORY", "256mb"}
                    },
                    CreatedAt = DateTime.UtcNow,
                    LastAccessedAt = DateTime.UtcNow
                };
            }
            else
            {
                return BadRequest("Demo containers already exist. Use cleanup-demo to remove them first.");
            }

            _context.DatabaseContainers.Add(demoContainer);
            await _context.SaveChangesAsync();

            return Ok(new { 
                message = "Demo container created successfully", 
                debug = new {
                    existingCount,
                    existingContainers,
                    createdType = demoContainer.DatabaseType,
                    userEmail = demoUser.Email
                },
                container = new {
                    demoContainer.Id,
                    demoContainer.Name,
                    demoContainer.DatabaseType,
                    demoContainer.Status,
                    demoContainer.Port
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create demo container");
            return StatusCode(500, new { 
                error = ex.Message,
                details = ex.InnerException?.Message,
                type = ex.GetType().Name
            });
        }
    }

    [HttpDelete("cleanup-demo")]
    public async Task<IActionResult> CleanupDemoData()
    {
        try
        {
            var demoUserId = "demo-user";
            
            // Find all demo containers
            var demoContainers = await _context.DatabaseContainers
                .Where(c => c.UserId == demoUserId)
                .ToListAsync();

            _logger.LogInformation("Found {Count} demo containers to cleanup", demoContainers.Count);

            // Remove containers from Docker (if they exist)
            foreach (var container in demoContainers)
            {
                try
                {
                    await _orchestrator.RemoveContainerAsync(container.ContainerId);
                    _logger.LogInformation("Removed Docker container {ContainerId}", container.ContainerId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to remove Docker container {ContainerId} - it may not exist", container.ContainerId);
                }
            }

            // Remove containers from database
            _context.DatabaseContainers.RemoveRange(demoContainers);

            // Find and remove demo user
            var demoUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == demoUserId);
            if (demoUser != null)
            {
                _context.Users.Remove(demoUser);
                _logger.LogInformation("Removed demo user {UserId} with email {Email}", demoUser.Id, demoUser.Email);
            }

            // Also cleanup any users with demo emails that might be orphaned
            var orphanedDemoUsers = await _context.Users
                .Where(u => u.Email.StartsWith("demo") && u.Email.Contains("@devdb.local"))
                .ToListAsync();

            if (orphanedDemoUsers.Any())
            {
                _context.Users.RemoveRange(orphanedDemoUsers);
                _logger.LogInformation("Removed {Count} orphaned demo users", orphanedDemoUsers.Count);
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Demo data cleanup completed successfully",
                removedContainers = demoContainers.Count,
                removedUsers = 1 + orphanedDemoUsers.Count,
                details = new
                {
                    containers = demoContainers.Select(c => new { c.Name, c.DatabaseType, c.Port }),
                    demoUserEmail = demoUser?.Email,
                    orphanedUsers = orphanedDemoUsers.Select(u => new { u.Id, u.Email })
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup demo data");
            return StatusCode(500, new { 
                error = ex.Message,
                stackTrace = ex.StackTrace
            });
        }
    }

    [HttpGet("database-debug")]
    public async Task<IActionResult> GetDatabaseDebugInfo()
    {
        try
        {
            // Get all users and their containers
            var users = await _context.Users
                .Include(u => u.Containers)
                .ToListAsync();

            // Check for duplicate emails
            var emailGroups = users.GroupBy(u => u.Email)
                .Where(g => g.Count() > 1)
                .Select(g => new { Email = g.Key, Count = g.Count(), UserIds = g.Select(u => u.Id).ToList() })
                .ToList();

            // Check for orphaned containers
            var orphanedContainers = await _context.DatabaseContainers
                .Where(c => !_context.Users.Any(u => u.Id == c.UserId))
                .ToListAsync();

            return Ok(new
            {
                message = "Database debug information",
                timestamp = DateTime.UtcNow,
                totalUsers = users.Count,
                totalContainers = users.SelectMany(u => u.Containers).Count(),
                duplicateEmails = emailGroups,
                orphanedContainers = orphanedContainers.Select(c => new { c.Id, c.UserId, c.Name, c.DatabaseType }),
                users = users.Select(u => new
                {
                    u.Id,
                    u.Email,
                    u.Name,
                    u.CreatedAt,
                    u.IsActive,
                    ContainerCount = u.Containers.Count,
                    Containers = u.Containers.Select(c => new { c.Name, c.DatabaseType, c.Status, c.Port })
                })
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { 
                error = ex.Message, 
                stackTrace = ex.StackTrace 
            });
        }
    }

    [HttpPost("emergency-reset")]
    public async Task<IActionResult> EmergencyDatabaseReset()
    {
        try
        {
            _logger.LogWarning("EMERGENCY DATABASE RESET INITIATED");

            // Get counts before cleanup
            var userCount = await _context.Users.CountAsync();
            var containerCount = await _context.DatabaseContainers.CountAsync();

            // Stop and remove all containers from Docker first
            var allContainers = await _context.DatabaseContainers.ToListAsync();
            var dockerRemovals = 0;
            var dockerErrors = 0;

            foreach (var container in allContainers)
            {
                try
                {
                    await _orchestrator.RemoveContainerAsync(container.ContainerId);
                    dockerRemovals++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to remove Docker container {ContainerId}", container.ContainerId);
                    dockerErrors++;
                }
            }

            // Clear all database tables
            _context.DatabaseContainers.RemoveRange(allContainers);
            var allUsers = await _context.Users.ToListAsync();
            _context.Users.RemoveRange(allUsers);
            
            // Clear system settings if any
            var allSettings = await _context.SystemSettings.ToListAsync();
            _context.SystemSettings.RemoveRange(allSettings);

            await _context.SaveChangesAsync();

            _logger.LogWarning("Emergency database reset completed. Removed {UserCount} users, {ContainerCount} containers", 
                userCount, containerCount);

            return Ok(new
            {
                message = "EMERGENCY DATABASE RESET COMPLETED",
                warning = "ALL DATA HAS BEEN REMOVED",
                timestamp = DateTime.UtcNow,
                removed = new
                {
                    users = userCount,
                    containers = containerCount,
                    settings = allSettings.Count
                },
                dockerCleanup = new
                {
                    successful = dockerRemovals,
                    errors = dockerErrors
                },
                nextSteps = new[]
                {
                    "Database is now clean",
                    "You can create new demo containers with POST /api/containers/create-demo",
                    "Regular container creation should work normally now"
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Emergency database reset failed");
            return StatusCode(500, new
            {
                error = "Emergency reset failed",
                message = ex.Message,
                stackTrace = ex.StackTrace,
                recommendation = "You may need to manually delete the database file and run migrations"
            });
        }
    }
}
