using Docker.DotNet;
using Docker.DotNet.Models;
using DbMaker.Shared.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using DockerContainerStatus = Docker.DotNet.Models.ContainerStatus;
using AppContainerStatus = DbMaker.Shared.Models.ContainerStatus;

namespace DbMaker.Shared.Services;

public interface IContainerOrchestrator
{
    Task<DatabaseContainer> CreateContainerAsync(string userId, CreateContainerRequest request);
    Task<bool> StartContainerAsync(string containerId);
    Task<bool> StopContainerAsync(string containerId);
    Task<bool> RemoveContainerAsync(string containerId);
    Task<ContainerMonitoringData?> GetContainerStatsAsync(string containerId);
    Task<List<ContainerMonitoringData>> GetAllContainerStatsAsync();
    int GetAvailablePort();
    string GenerateSubdomain(string userId, string containerName, string databaseType);
}

public class ContainerOrchestrator : IContainerOrchestrator
{
    private readonly DockerClient _dockerClient;
    private readonly ILogger<ContainerOrchestrator> _logger;
    private readonly ConcurrentDictionary<int, bool> _usedPorts = new();
    private readonly Dictionary<string, DatabaseTemplate> _templates;
    private int _currentPortStart = 10000;

    public ContainerOrchestrator(ILogger<ContainerOrchestrator> logger)
    {
        _logger = logger;
        _dockerClient = new DockerClientConfiguration().CreateClient();
        _templates = InitializeTemplates();
    }

    private Dictionary<string, DatabaseTemplate> InitializeTemplates()
    {
        return new Dictionary<string, DatabaseTemplate>
        {
            ["redis"] = new DatabaseTemplate
            {
                Type = "redis",
                DisplayName = "Redis",
                Description = "In-memory data structure store",
                DockerImage = "redis:7-alpine",
                Ports = new List<PortMapping> { new() { ContainerPort = 6379 } },
                DefaultEnvironment = new Dictionary<string, string>(),
                DefaultConfiguration = new Dictionary<string, object>
                {
                    ["maxmemory"] = "256mb",
                    ["maxmemory-policy"] = "allkeys-lru"
                }
            },
            ["postgresql"] = new DatabaseTemplate
            {
                Type = "postgresql",
                DisplayName = "PostgreSQL",
                Description = "Advanced open source relational database",
                DockerImage = "postgres:16-alpine",
                Ports = new List<PortMapping> { new() { ContainerPort = 5432 } },
                DefaultEnvironment = new Dictionary<string, string>
                {
                    ["POSTGRES_DB"] = "userdb",
                    ["POSTGRES_USER"] = "dbuser",
                    ["POSTGRES_PASSWORD"] = "secure_password_123"
                },
                Volumes = new List<VolumeMapping>
                {
                    new() { ContainerPath = "/var/lib/postgresql/data" }
                }
            }
        };
    }

    public async Task<DatabaseContainer> CreateContainerAsync(string userId, CreateContainerRequest request)
    {
        try
        {
            if (!_templates.TryGetValue(request.DatabaseType, out var template))
            {
                throw new ArgumentException($"Unknown database type: {request.DatabaseType}");
            }

            var port = GetAvailablePort();
            var subdomain = GenerateSubdomain(userId, request.Name, request.DatabaseType);
            var containerName = $"dbmaker-{userId}-{request.DatabaseType}-{request.Name}";

            var container = new DatabaseContainer
            {
                UserId = userId,
                Name = request.Name,
                DatabaseType = request.DatabaseType,
                ContainerName = containerName,
                Port = port,
                Subdomain = subdomain,
                Status = AppContainerStatus.Creating,
                Configuration = request.Configuration
            };

            // Merge configuration with template defaults
            var environment = new Dictionary<string, string>(template.DefaultEnvironment);
            foreach (var config in request.Configuration)
            {
                environment[config.Key] = config.Value;
            }

            // Create port bindings
            var portBindings = new Dictionary<string, IList<PortBinding>>();
            foreach (var portMapping in template.Ports)
            {
                portBindings[$"{portMapping.ContainerPort}/{portMapping.Protocol}"] = new List<PortBinding>
                {
                    new() { HostPort = port.ToString() }
                };
            }

            // Create container
            var createContainerParameters = new CreateContainerParameters
            {
                Name = containerName,
                Image = template.DockerImage,
                Env = environment.Select(kvp => $"{kvp.Key}={kvp.Value}").ToList(),
                HostConfig = new HostConfig
                {
                    PortBindings = portBindings,
                    RestartPolicy = new RestartPolicy { Name = RestartPolicyKind.UnlessStopped }
                },
                Labels = new Dictionary<string, string>
                {
                    ["dbmaker.userId"] = userId,
                    ["dbmaker.databaseType"] = request.DatabaseType,
                    ["dbmaker.containerName"] = request.Name,
                    ["dbmaker.subdomain"] = subdomain
                }
            };

            // Add volumes if specified
            if (template.Volumes.Any())
            {
                createContainerParameters.HostConfig.Binds = template.Volumes
                    .Select(v => $"dbmaker-{containerName}-data:{v.ContainerPath}")
                    .ToList();
            }

            var response = await _dockerClient.Containers.CreateContainerAsync(createContainerParameters);
            container.ContainerId = response.ID;

            // Generate connection string
            container.ConnectionString = GenerateConnectionString(template, port, environment, subdomain);

            // Start the container
            await _dockerClient.Containers.StartContainerAsync(response.ID, new ContainerStartParameters());
            container.Status = AppContainerStatus.Running;

            _logger.LogInformation("Created and started container {ContainerName} for user {UserId}", containerName, userId);

            return container;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create container for user {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> StartContainerAsync(string containerId)
    {
        try
        {
            await _dockerClient.Containers.StartContainerAsync(containerId, new ContainerStartParameters());
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start container {ContainerId}", containerId);
            return false;
        }
    }

    public async Task<bool> StopContainerAsync(string containerId)
    {
        try
        {
            await _dockerClient.Containers.StopContainerAsync(containerId, new ContainerStopParameters());
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop container {ContainerId}", containerId);
            return false;
        }
    }

    public async Task<bool> RemoveContainerAsync(string containerId)
    {
        try
        {
            await _dockerClient.Containers.RemoveContainerAsync(containerId, new ContainerRemoveParameters { Force = true });
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove container {ContainerId}", containerId);
            return false;
        }
    }

    public async Task<ContainerMonitoringData?> GetContainerStatsAsync(string containerId)
    {
        try
        {
            var inspect = await _dockerClient.Containers.InspectContainerAsync(containerId);
            
            // For simplified monitoring, we'll use container state information
            // In production, you might want to implement proper stats collection
            return new ContainerMonitoringData
            {
                ContainerId = containerId,
                Status = GetContainerStatus(inspect.State),
                CpuUsage = 0, // Would need more complex implementation for real CPU stats
                MemoryUsage = 0, // Would need more complex implementation for real memory stats
                MemoryLimit = 0,
                IsHealthy = inspect.State.Running && inspect.State.Health?.Status != "unhealthy"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get stats for container {ContainerId}", containerId);
            return null;
        }
    }

    public async Task<List<ContainerMonitoringData>> GetAllContainerStatsAsync()
    {
        var containers = await _dockerClient.Containers.ListContainersAsync(new ContainersListParameters
        {
            Filters = new Dictionary<string, IDictionary<string, bool>>
            {
                ["label"] = new Dictionary<string, bool> { ["dbmaker.userId"] = true }
            }
        });

        var stats = new List<ContainerMonitoringData>();
        foreach (var container in containers)
        {
            var containerStats = await GetContainerStatsAsync(container.ID);
            if (containerStats != null)
            {
                // Extract user ID from labels
                if (container.Labels.TryGetValue("dbmaker.userId", out var userId))
                {
                    containerStats.UserId = userId;
                }
                stats.Add(containerStats);
            }
        }

        return stats;
    }

    public int GetAvailablePort()
    {
        for (int port = _currentPortStart; port < _currentPortStart + 10000; port++)
        {
            if (_usedPorts.TryAdd(port, true))
            {
                return port;
            }
        }
        throw new InvalidOperationException("No available ports");
    }

    public string GenerateSubdomain(string userId, string containerName, string databaseType)
    {
        // Create a safe subdomain name
        var subdomain = $"{userId}-{containerName}-{databaseType}".ToLower()
            .Replace("_", "-")
            .Replace(" ", "-");
        
        // Remove invalid characters
        subdomain = new string(subdomain.Where(c => char.IsLetterOrDigit(c) || c == '-').ToArray());
        
        return subdomain;
    }

    private string GenerateConnectionString(DatabaseTemplate template, int port, Dictionary<string, string> environment, string subdomain)
    {
        return template.Type switch
        {
            "redis" => $"redis://{subdomain}.mydomain.com:{port}",
            "postgresql" => $"postgresql://{environment.GetValueOrDefault("POSTGRES_USER", "dbuser")}:{environment.GetValueOrDefault("POSTGRES_PASSWORD", "password")}@{subdomain}.mydomain.com:{port}/{environment.GetValueOrDefault("POSTGRES_DB", "userdb")}",
            _ => $"{template.Type}://{subdomain}.mydomain.com:{port}"
        };
    }

    private AppContainerStatus GetContainerStatus(ContainerState state)
    {
        if (state.Running) return AppContainerStatus.Running;
        if (state.Dead) return AppContainerStatus.Failed;
        return AppContainerStatus.Stopped;
    }

    private double CalculateCpuUsage(ContainerStatsResponse stats)
    {
        // Simplified CPU usage calculation
        var cpuDelta = stats.CPUStats?.CPUUsage?.TotalUsage - stats.PreCPUStats?.CPUUsage?.TotalUsage;
        var systemDelta = stats.CPUStats?.SystemUsage - stats.PreCPUStats?.SystemUsage;
        
        if (cpuDelta > 0 && systemDelta > 0)
        {
            return (double)(cpuDelta ?? 0) / (double)(systemDelta ?? 1) * 100.0;
        }
        
        return 0.0;
    }

    public void Dispose()
    {
        _dockerClient?.Dispose();
    }
}
