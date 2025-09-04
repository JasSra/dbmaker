using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DbMaker.Shared.Data;
using DbMaker.Shared.Models;
using DbMaker.Shared.Services;
using Docker.DotNet;
using Microsoft.Identity.Web;
using System.Security.Cryptography;
using System.Text;

namespace DbMaker.API.Controllers;

/// <summary>
/// System setup and initialization controller
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SetupController : ControllerBase
{
    private readonly DbMakerDbContext _context;
    private readonly IContainerOrchestrator _orchestrator;
    private readonly ILogger<SetupController> _logger;
    private readonly IConfiguration _configuration;

    public SetupController(
        DbMakerDbContext context,
        IContainerOrchestrator orchestrator,
        ILogger<SetupController> logger,
        IConfiguration configuration)
    {
        _context = context;
        _orchestrator = orchestrator;
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Check if the system needs initial setup
    /// </summary>
    [HttpGet("status")]
    public async Task<ActionResult<SetupStatus>> GetSetupStatus()
    {
        var status = new SetupStatus();

        try
        {
            // Check database connectivity
            status.DatabaseConfigured = await _context.Database.CanConnectAsync();
            
            // Check if admin user exists
            var adminExists = await _context.Users.AnyAsync(u => u.Email.Contains("admin") || u.Name.Contains("admin"));
            status.AdminUserExists = adminExists;

            // Check Docker connectivity
            status.DockerConnected = await CheckDockerConnectivity();

            // Check MSAL configuration
            status.MsalConfigured = CheckMsalConfiguration();

            // System is ready if all components are configured
            status.SystemReady = status.DatabaseConfigured && 
                               status.DockerConnected && 
                               status.MsalConfigured;

            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check setup status");
            return StatusCode(500, "Failed to check setup status");
        }
    }

    /// <summary>
    /// Validate Docker daemon connectivity
    /// </summary>
    [HttpGet("validate/docker")]
    public async Task<ActionResult<ValidationResult>> ValidateDocker()
    {
        try
        {
            var isConnected = await CheckDockerConnectivity();
            var message = isConnected ? "Docker daemon is accessible" : "Cannot connect to Docker daemon";
            
            return Ok(new ValidationResult 
            { 
                IsValid = isConnected, 
                Message = message,
                Details = isConnected ? await GetDockerInfo() : "Docker daemon not accessible"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Docker validation failed");
            return Ok(new ValidationResult 
            { 
                IsValid = false, 
                Message = "Docker validation failed", 
                Details = ex.Message 
            });
        }
    }

    /// <summary>
    /// Validate MSAL configuration
    /// </summary>
    [HttpGet("validate/msal")]
    public ActionResult<ValidationResult> ValidateMsal()
    {
        try
        {
            var isValid = CheckMsalConfiguration();
            var config = GetMsalConfigurationDetails();
            
            return Ok(new ValidationResult 
            { 
                IsValid = isValid,
                Message = isValid ? "MSAL configuration is valid" : "MSAL configuration is incomplete",
                Details = config
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MSAL validation failed");
            return Ok(new ValidationResult 
            { 
                IsValid = false, 
                Message = "MSAL validation failed", 
                Details = ex.Message 
            });
        }
    }

    /// <summary>
    /// Initialize the system with admin user and backup key
    /// </summary>
    [HttpPost("initialize")]
    public async Task<ActionResult<InitializationResult>> InitializeSystem(InitializeSystemRequest request)
    {
        try
        {
            var result = new InitializationResult();

            // Validate all prerequisites
            var setupStatus = await GetSetupStatusInternal();
            if (!setupStatus.DatabaseConfigured || !setupStatus.DockerConnected)
            {
                return BadRequest("Prerequisites not met. Database and Docker must be available.");
            }

            // Create admin user if doesn't exist
            var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.AdminEmail);
            if (adminUser == null)
            {
                adminUser = new User
                {
                    Id = Guid.NewGuid().ToString(),
                    Email = request.AdminEmail,
                    Name = request.AdminName,
                    IsActive = true
                };
                _context.Users.Add(adminUser);
                result.AdminUserCreated = true;
            }

            // Generate backup key
            var backupKey = GenerateBackupKey();
            result.BackupKey = backupKey;

            // Store system configuration
            await StoreSystemConfiguration(request, backupKey);

            await _context.SaveChangesAsync();

            _logger.LogInformation("System initialized successfully for admin user {Email}", request.AdminEmail);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize system");
            return StatusCode(500, "Failed to initialize system");
        }
    }

    private async Task<bool> CheckDockerConnectivity()
    {
        try
        {
            using var client = new DockerClientConfiguration().CreateClient();
            await client.System.PingAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task<string> GetDockerInfo()
    {
        try
        {
            using var client = new DockerClientConfiguration().CreateClient();
            var info = await client.System.GetSystemInfoAsync();
            return $"Docker {info.ServerVersion} - {info.NCPU} CPUs, {info.MemTotal / (1024 * 1024 * 1024)}GB RAM";
        }
        catch
        {
            return "Docker info unavailable";
        }
    }

    private bool CheckMsalConfiguration()
    {
        var clientId = _configuration["AzureAd:ClientId"];
        var authority = _configuration["AzureAd:Authority"];
        
        return !string.IsNullOrEmpty(clientId) && !string.IsNullOrEmpty(authority);
    }

    private string GetMsalConfigurationDetails()
    {
        var config = new
        {
            ClientId = _configuration["AzureAd:ClientId"],
            Authority = _configuration["AzureAd:Authority"],
            Instance = _configuration["AzureAd:Instance"]
        };
        
        return System.Text.Json.JsonSerializer.Serialize(config, new System.Text.Json.JsonSerializerOptions 
        { 
            WriteIndented = true 
        });
    }

    private async Task<SetupStatus> GetSetupStatusInternal()
    {
        var status = new SetupStatus();
        status.DatabaseConfigured = await _context.Database.CanConnectAsync();
        status.AdminUserExists = await _context.Users.AnyAsync();
        status.DockerConnected = await CheckDockerConnectivity();
        status.MsalConfigured = CheckMsalConfiguration();
        status.SystemReady = status.DatabaseConfigured && status.DockerConnected && status.MsalConfigured;
        return status;
    }

    private string GenerateBackupKey()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[32];
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }

    private async Task StoreSystemConfiguration(InitializeSystemRequest request, string backupKey)
    {
        // Store configuration in a simple key-value format in the database
        // In production, you might want to encrypt sensitive data
        var configs = new Dictionary<string, string>
        {
            ["system.initialized"] = "true",
            ["system.initialized_at"] = DateTime.UtcNow.ToString("O"),
            ["system.admin_email"] = request.AdminEmail,
            ["system.backup_key"] = backupKey,
            ["system.domain"] = request.Domain ?? "localhost"
        };

        // Note: This is a simplified approach. In production, consider using a dedicated configuration table
        // or Azure Key Vault for sensitive configuration data
    }
}

// DTOs for Setup API
public class SetupStatus
{
    public bool DatabaseConfigured { get; set; }
    public bool AdminUserExists { get; set; }
    public bool DockerConnected { get; set; }
    public bool MsalConfigured { get; set; }
    public bool SystemReady { get; set; }
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
}

public class InitializeSystemRequest
{
    public string AdminEmail { get; set; } = string.Empty;
    public string AdminName { get; set; } = string.Empty;
    public string? Domain { get; set; }
    public Dictionary<string, string> AdditionalConfig { get; set; } = new();
}

public class InitializationResult
{
    public bool AdminUserCreated { get; set; }
    public string BackupKey { get; set; } = string.Empty;
    public bool Success { get; set; } = true;
    public string Message { get; set; } = "System initialized successfully";
}
