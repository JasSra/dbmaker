using Docker.DotNet;
using Docker.DotNet.Models;
using DbMaker.Shared.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using DockerContainerStatus = Docker.DotNet.Models.ContainerStatus;
using AppContainerStatus = DbMaker.Shared.Models.ContainerStatus;

namespace DbMaker.Shared.Services;

public class SafeContainerOrchestrator : IContainerOrchestrator
{
    private DockerClient? _dockerClient;
    private readonly ILogger<SafeContainerOrchestrator> _logger;
    private readonly ConcurrentDictionary<int, bool> _usedPorts = new();
    private readonly Dictionary<string, DatabaseTemplate> _templates;
    private int _currentPortStart = 10000;
    private bool _dockerConnectionTested = false;

    public SafeContainerOrchestrator(ILogger<SafeContainerOrchestrator> logger)
    {
        _logger = logger;
        _templates = InitializeTemplates();
    }

    private async Task<DockerClient> GetDockerClientAsync()
    {
        if (_dockerClient != null && _dockerConnectionTested)
        {
            return _dockerClient;
        }

        try
        {
            // Try default connection first
            _dockerClient = new DockerClientConfiguration().CreateClient();
            await _dockerClient.System.PingAsync();
            _dockerConnectionTested = true;
            return _dockerClient;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create Docker client with default configuration, trying alternative configs");
            
            try
            {
                // Try named pipe for Windows
                if (Environment.OSVersion.Platform != PlatformID.Unix)
                {
                    _dockerClient = new DockerClientConfiguration(new Uri("npipe://./pipe/docker_engine")).CreateClient();
                    await _dockerClient.System.PingAsync();
                    _dockerConnectionTested = true;
                    return _dockerClient;
                }
                else
                {
                    // Try Unix socket for Linux/macOS
                    _dockerClient = new DockerClientConfiguration(new Uri("unix:///var/run/docker.sock")).CreateClient();
                    await _dockerClient.System.PingAsync();
                    _dockerConnectionTested = true;
                    return _dockerClient;
                }
            }
            catch (Exception ex2)
            {
                _logger.LogError(ex2, "Failed to create Docker client with alternative configurations");
                
                // Try TCP connection as last resort
                try
                {
                    _dockerClient = new DockerClientConfiguration(new Uri("tcp://localhost:2375")).CreateClient();
                    await _dockerClient.System.PingAsync();
                    _dockerConnectionTested = true;
                    return _dockerClient;
                }
                catch (Exception ex3)
                {
                    _logger.LogError(ex3, "All Docker connection attempts failed");
                    throw new InvalidOperationException("Cannot connect to Docker daemon. Please ensure Docker is running and accessible.", ex3);
                }
            }
        }
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

            // Get Docker client and test connectivity
            var dockerClient = await GetDockerClientAsync();

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

            // Ensure required images are available
            try
            {
                await dockerClient.Images.CreateImageAsync(
                    new ImagesCreateParameters
                    {
                        FromImage = template.DockerImage.Split(':')[0],
                        Tag = template.DockerImage.Contains(':') ? template.DockerImage.Split(':')[1] : "latest"
                    },
                    null,
                    new Progress<JSONMessage>(message => _logger.LogDebug("Image pull: {Status}", message.Status))
                );
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to pull image {Image}, assuming it exists locally", template.DockerImage);
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

            var response = await dockerClient.Containers.CreateContainerAsync(createContainerParameters);
            container.ContainerId = response.ID;

            // Generate connection string
            container.ConnectionString = GenerateConnectionString(template, port, environment, subdomain);

            // Start the container
            await dockerClient.Containers.StartContainerAsync(response.ID, new ContainerStartParameters());
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
            var dockerClient = await GetDockerClientAsync();
            await dockerClient.Containers.StartContainerAsync(containerId, new ContainerStartParameters());
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
            var dockerClient = await GetDockerClientAsync();
            await dockerClient.Containers.StopContainerAsync(containerId, new ContainerStopParameters());
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
            var dockerClient = await GetDockerClientAsync();
            await dockerClient.Containers.RemoveContainerAsync(containerId, new ContainerRemoveParameters { Force = true });
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
            var dockerClient = await GetDockerClientAsync();
            var inspectResponse = await dockerClient.Containers.InspectContainerAsync(containerId);
            
            return new ContainerMonitoringData
            {
                ContainerId = containerId,
                Status = GetContainerStatus(inspectResponse.State),
                CpuUsage = 0.0, // Would need stats stream for real CPU usage
                MemoryUsage = 0L,    // Would need stats stream for real memory usage
                MemoryLimit = 0L,
                NetworkIO = new Dictionary<string, long>(),
                Timestamp = DateTime.UtcNow,
                IsHealthy = inspectResponse.State.Running
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
        try
        {
            var dockerClient = await GetDockerClientAsync();
            var containers = await dockerClient.Containers.ListContainersAsync(new ContainersListParameters
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all container stats");
            return new List<ContainerMonitoringData>();
        }
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

    public async Task<int> GetAvailablePortAsync()
    {
        // For the safe orchestrator, we just use the synchronous version
        return GetAvailablePort();
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
            "redis" => $"redis://localhost:{port}",
            "postgresql" => $"postgresql://{environment.GetValueOrDefault("POSTGRES_USER", "dbuser")}:{environment.GetValueOrDefault("POSTGRES_PASSWORD", "password")}@localhost:{port}/{environment.GetValueOrDefault("POSTGRES_DB", "userdb")}",
            _ => $"{template.Type}://localhost:{port}"
        };
    }

    private AppContainerStatus GetContainerStatus(ContainerState state)
    {
        if (state.Running) return AppContainerStatus.Running;
        if (state.Dead) return AppContainerStatus.Failed;
        return AppContainerStatus.Stopped;
    }

    public void Dispose()
    {
        _dockerClient?.Dispose();
    }
}
