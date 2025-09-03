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
