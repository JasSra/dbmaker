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
public class SettingsController : ControllerBase
{
    private readonly DbMakerDbContext _context;

    public SettingsController(DbMakerDbContext context)
    {
        _context = context;
    }

    private string GetCurrentUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
    }

    [HttpGet]
    public async Task<ActionResult<SettingsResponse>> GetSettings()
    {
        try
        {
            var userId = GetCurrentUserId();
            
            // Get user-specific settings first, fall back to global settings
            var userSettings = await _context.SystemSettings
                .FirstOrDefaultAsync(s => s.UserId == userId);

            var globalSettings = await _context.SystemSettings
                .FirstOrDefaultAsync(s => s.UserId == string.Empty);

            // Merge settings (user settings override global)
            var settings = globalSettings ?? new SystemSettings();
            if (userSettings != null)
            {
                // Merge user preferences over global settings
                settings.UI = userSettings.UI;
                if (userSettings.Docker != null) settings.Docker = userSettings.Docker;
                if (userSettings.Containers != null) settings.Containers = userSettings.Containers;
            }

            return Ok(new SettingsResponse 
            { 
                Settings = settings, 
                Success = true 
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new SettingsResponse 
            { 
                Success = false, 
                Message = ex.Message 
            });
        }
    }

    [HttpGet("global")]
    public async Task<ActionResult<SettingsResponse>> GetGlobalSettings()
    {
        try
        {
            var globalSettings = await _context.SystemSettings
                .FirstOrDefaultAsync(s => s.UserId == string.Empty);

            if (globalSettings == null)
            {
                // Create default global settings
                globalSettings = new SystemSettings
                {
                    Id = "global-settings",
                    UserId = string.Empty
                };
                _context.SystemSettings.Add(globalSettings);
                await _context.SaveChangesAsync();
            }

            return Ok(new SettingsResponse 
            { 
                Settings = globalSettings, 
                Success = true 
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new SettingsResponse 
            { 
                Success = false, 
                Message = ex.Message 
            });
        }
    }

    [HttpPut]
    public async Task<ActionResult<SettingsResponse>> UpdateSettings([FromBody] UpdateSettingsRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            
            var settings = await _context.SystemSettings
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (settings == null)
            {
                settings = new SystemSettings
                {
                    Id = $"user-settings-{userId}",
                    UserId = userId
                };
                _context.SystemSettings.Add(settings);
            }

            // Update only the provided settings sections
            if (request.UI != null)
                settings.UI = request.UI;
            
            if (request.Docker != null)
                settings.Docker = request.Docker;
                
            if (request.Nginx != null)
                settings.Nginx = request.Nginx;
                
            if (request.Containers != null)
                settings.Containers = request.Containers;

            settings.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new SettingsResponse 
            { 
                Settings = settings, 
                Success = true, 
                Message = "Settings updated successfully" 
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new SettingsResponse 
            { 
                Success = false, 
                Message = ex.Message 
            });
        }
    }

    [HttpPut("global")]
    public async Task<ActionResult<SettingsResponse>> UpdateGlobalSettings([FromBody] UpdateSettingsRequest request)
    {
        try
        {
            var settings = await _context.SystemSettings
                .FirstOrDefaultAsync(s => s.UserId == string.Empty);

            if (settings == null)
            {
                settings = new SystemSettings
                {
                    Id = "global-settings",
                    UserId = string.Empty
                };
                _context.SystemSettings.Add(settings);
            }

            // Update only the provided settings sections
            if (request.UI != null)
                settings.UI = request.UI;
            
            if (request.Docker != null)
                settings.Docker = request.Docker;
                
            if (request.Nginx != null)
                settings.Nginx = request.Nginx;
                
            if (request.Containers != null)
                settings.Containers = request.Containers;

            settings.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new SettingsResponse 
            { 
                Settings = settings, 
                Success = true, 
                Message = "Global settings updated successfully" 
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new SettingsResponse 
            { 
                Success = false, 
                Message = ex.Message 
            });
        }
    }

    [HttpPost("docker/remote-host")]
    public async Task<ActionResult> AddRemoteDockerHost([FromBody] RemoteDockerHost host)
    {
        try
        {
            var userId = GetCurrentUserId();
            var settings = await _context.SystemSettings
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (settings == null)
            {
                settings = new SystemSettings
                {
                    Id = $"user-settings-{userId}",
                    UserId = userId
                };
                _context.SystemSettings.Add(settings);
            }

            host.Id = Guid.NewGuid().ToString();
            settings.Docker.RemoteHosts.Add(host);
            settings.UpdatedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();

            return Ok(new { success = true, hostId = host.Id, message = "Remote Docker host added successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpDelete("docker/remote-host/{hostId}")]
    public async Task<ActionResult> RemoveRemoteDockerHost(string hostId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var settings = await _context.SystemSettings
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (settings != null)
            {
                var host = settings.Docker.RemoteHosts.FirstOrDefault(h => h.Id == hostId);
                if (host != null)
                {
                    settings.Docker.RemoteHosts.Remove(host);
                    settings.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    return Ok(new { success = true, message = "Remote Docker host removed successfully" });
                }
            }

            return NotFound(new { success = false, message = "Remote Docker host not found" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }
}
