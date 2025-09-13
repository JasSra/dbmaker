using System.ComponentModel.DataAnnotations;

namespace DbMaker.Shared.Models;

public class User
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastLoginAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    
    public List<DatabaseContainer> Containers { get; set; } = new();
}

public class DatabaseContainer
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DatabaseType { get; set; } = string.Empty; // redis, postgresql, etc.
    public string ContainerName { get; set; } = string.Empty;
    public string ContainerId { get; set; } = string.Empty;
    public int Port { get; set; }
    public string ConnectionString { get; set; } = string.Empty;
    public ContainerStatus Status { get; set; } = ContainerStatus.Creating;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastAccessedAt { get; set; } = DateTime.UtcNow;
    public Dictionary<string, string> Configuration { get; set; } = new();
    public string Subdomain { get; set; } = string.Empty;
    
    public User User { get; set; } = null!;
}

public enum ContainerStatus
{
    Creating,
    Running,
    Stopped,
    Failed,
    Removing
}

public class DatabaseTemplate
{
    public string Type { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DockerImage { get; set; } = string.Empty;
    public Dictionary<string, string> DefaultEnvironment { get; set; } = new();
    public Dictionary<string, object> DefaultConfiguration { get; set; } = new();
    public List<PortMapping> Ports { get; set; } = new();
    public List<VolumeMapping> Volumes { get; set; } = new();
    public string? ConnectionStringTemplate { get; set; }
    public bool IsEnabled { get; set; } = true;
}

public class PortMapping
{
    public int ContainerPort { get; set; }
    public string Protocol { get; set; } = "tcp";
}

public class VolumeMapping
{
    public string ContainerPath { get; set; } = string.Empty;
    public string HostPath { get; set; } = string.Empty;
    public bool IsReadOnly { get; set; } = false;
}

public class ContainerMonitoringData
{
    public string ContainerId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public ContainerStatus Status { get; set; }
    public double CpuUsage { get; set; }
    public long MemoryUsage { get; set; }
    public long MemoryLimit { get; set; }
    public Dictionary<string, long> NetworkIO { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public bool IsHealthy { get; set; } = true;
    public string? ErrorMessage { get; set; }
}

public class CreateContainerRequest
{
    [Required]
    public string DatabaseType { get; set; } = string.Empty;
    
    [Required]
    public string Name { get; set; } = string.Empty;
    
    public string? UserId { get; set; } // Optional - can be provided or taken from auth
    
    public Dictionary<string, string> Configuration { get; set; } = new();
}

public class ContainerResponse
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DatabaseType { get; set; } = string.Empty;
    public string ConnectionString { get; set; } = string.Empty;
    public ContainerStatus Status { get; set; }
    public string Subdomain { get; set; } = string.Empty;
    public int Port { get; set; }
    public DateTime CreatedAt { get; set; }
    public Dictionary<string, string> Configuration { get; set; } = new();
}

// Settings and Configuration Models
public class SystemSettings
{
    public string Id { get; set; } = "system-settings";
    public string UserId { get; set; } = string.Empty; // Empty for global settings, user-specific for user settings
    
    // Docker Configuration
    public DockerSettings Docker { get; set; } = new();
    
    // UI Preferences
    public UISettings UI { get; set; } = new();
    
    // Nginx Configuration
    public NginxSettings Nginx { get; set; } = new();
    
    // Container Visibility
    public ContainerSettings Containers { get; set; } = new();
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class DockerSettings
{
    public string DefaultHost { get; set; } = "npipe://./pipe/docker_engine";
    public bool EnableMaintenance { get; set; } = true;
    public bool AutoCleanup { get; set; } = true;
    public int MaintenanceInterval { get; set; } = 3600; // seconds
    public List<RemoteDockerHost> RemoteHosts { get; set; } = new();
    public string? CurrentRemoteHost { get; set; } // null = local
}

public class UISettings
{
    public bool DarkMode { get; set; } = false;
    public string Theme { get; set; } = "default";
    public bool EnableAnimations { get; set; } = true;
    public int RefreshInterval { get; set; } = 30; // seconds
}

public class NginxSettings
{
    public bool EnableDynamicSubdomains { get; set; } = true;
    public string BaseDomain { get; set; } = "starklink.local";
    public int ListenPort { get; set; } = 8080;
    public bool UseGuidSubdomains { get; set; } = true;
    public Dictionary<string, string> SubdomainMappings { get; set; } = new();
}

public class ContainerSettings
{
    public bool ShowAllContainers { get; set; } = false; // true = show all Docker containers, false = only managed
    public bool ShowSystemContainers { get; set; } = false;
    public bool EnableVisualization { get; set; } = true;
    public List<string> HiddenContainers { get; set; } = new();
}

public class RemoteDockerHost
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty; // tcp://host:port, ssh://user@host, etc.
    public bool UseTLS { get; set; } = false;
    public string? CertPath { get; set; }
    public string? KeyPath { get; set; }
    public bool IsActive { get; set; } = false;
    public DateTime LastConnected { get; set; }
    public string? LastError { get; set; }
}

// Request/Response Models for Settings API
public class UpdateSettingsRequest
{
    public DockerSettings? Docker { get; set; }
    public UISettings? UI { get; set; }
    public NginxSettings? Nginx { get; set; }
    public ContainerSettings? Containers { get; set; }
}

public class SettingsResponse
{
    public SystemSettings Settings { get; set; } = new();
    public bool Success { get; set; } = true;
    public string? Message { get; set; }
}
