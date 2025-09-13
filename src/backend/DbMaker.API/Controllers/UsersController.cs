using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using DbMaker.Shared.Data;
using DbMaker.Shared.Models;
using System.Security.Claims;

namespace DbMaker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly DbMakerDbContext _context;
    private readonly ILogger<UsersController> _logger;

    public UsersController(DbMakerDbContext context, ILogger<UsersController> logger)
    {
        _context = context;
        _logger = logger;
    }

    private string? GetCurrentUserId()
    {
        return User.FindFirst("oid")?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;
    }

    [HttpGet("me")]
     // TEMPORARY: allow for dev UI to work
    public async Task<ActionResult<User>> GetCurrentUser()
    {
        var userId = GetCurrentUserId() ?? "test-user-123";
        var email = User.FindFirst(ClaimTypes.Email)?.Value ?? "unknown@domain.com";
        var name = User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown User";

        var user = await _context.Users
            .Include(u => u.Containers)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            // Create user if doesn't exist
            user = new User
            {
                Id = userId,
                Email = email,
                Name = name
            };
            
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }
        else
        {
            // Update last login time
            user.LastLoginAt = DateTime.UtcNow;
            user.Email = email; // Update email in case it changed
            user.Name = name;   // Update name in case it changed
            await _context.SaveChangesAsync();
        }

        return Ok(user);
    }

    [HttpGet("me/stats")]
     // TEMPORARY: allow for dev UI to work
    public async Task<ActionResult<object>> GetUserStats()
    {
        var userId = GetCurrentUserId() ?? "test-user-123";

        var stats = await _context.DatabaseContainers
            .Where(c => c.UserId == userId)
            .GroupBy(c => c.DatabaseType)
            .Select(g => new { DatabaseType = g.Key, Count = g.Count() })
            .ToListAsync();

        var totalContainers = await _context.DatabaseContainers
            .CountAsync(c => c.UserId == userId);

        var runningContainers = await _context.DatabaseContainers
            .CountAsync(c => c.UserId == userId && c.Status == ContainerStatus.Running);

        return Ok(new
        {
            TotalContainers = totalContainers,
            RunningContainers = runningContainers,
            ContainersByType = stats
        });
    }
}
