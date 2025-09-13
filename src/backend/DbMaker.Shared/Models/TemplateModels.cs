using System.ComponentModel.DataAnnotations;

namespace DbMaker.Shared.Models;

public class Template
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    [Required]
    public string Key { get; set; } = string.Empty; // e.g., "postgresql"
    [Required]
    public string DisplayName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // Database, Cache, etc.
    public string Icon { get; set; } = string.Empty; // wwwroot/template-icons/{key}.svg
    public string Description { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public string? LatestVersion { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<TemplateVersion> Versions { get; set; } = new();
}

public class TemplateVersion
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    [Required]
    public string TemplateId { get; set; } = string.Empty;
    public Template? Template { get; set; }

    [Required]
    public string Version { get; set; } = string.Empty; // e.g., "16-alpine"
    [Required]
    public string DockerImage { get; set; } = string.Empty; // e.g., postgres:16-alpine
    public string ConnectionStringTemplate { get; set; } = string.Empty;

    // JSON-backed fields
    public List<PortMapping> Ports { get; set; } = new();
    public List<VolumeMapping> Volumes { get; set; } = new();
    public Dictionary<string, string> DefaultEnvironment { get; set; } = new();
    public Dictionary<string, string> DefaultConfiguration { get; set; } = new();
    public HealthcheckSpec? Healthcheck { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class HealthcheckSpec
{
    public List<string> Test { get; set; } = new();
    public string? Interval { get; set; }
    public string? Timeout { get; set; }
    public int? Retries { get; set; }
    public string? StartPeriod { get; set; }
}
