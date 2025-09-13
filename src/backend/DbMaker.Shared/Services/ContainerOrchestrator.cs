using Docker.DotNet;
using Docker.DotNet.Models;
using DbMaker.Shared.Models;
using DbMaker.Shared.Services.Templates;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text;
using DockerContainerStatus = Docker.DotNet.Models.ContainerStatus;
using AppContainerStatus = DbMaker.Shared.Models.ContainerStatus;

namespace DbMaker.Shared.Services;

public class ContainerOrchestrator : IContainerOrchestrator
{
    private DockerClient? _dockerClient;
    private readonly ILogger<ContainerOrchestrator> _logger;
    private readonly ITemplateResolver _templateResolver;
    private readonly ConcurrentDictionary<int, bool> _usedPorts = new();
    private readonly Dictionary<string, DatabaseTemplate> _templates;
    private int _currentPortStart = 10000;
    private bool _dockerConnectionTested = false;
    private readonly string _nginxConfigPath = @"c:\dev\DbMaker\nginx\conf.d\dynamic-upstreams.conf";

    public ContainerOrchestrator(ILogger<ContainerOrchestrator> logger, ITemplateResolver templateResolver)
    {
        _logger = logger;
        _templateResolver = templateResolver;
        _templates = InitializeTemplates();
        LoadExistingPortMappings();
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
                _logger.LogError(ex2, "All Docker connection attempts failed");
                throw new InvalidOperationException("Cannot connect to Docker daemon. Please ensure Docker is running and accessible.", ex2);
            }
        }
    }

    private async void LoadExistingPortMappings()
    {
        try
        {
            // Load existing Docker container ports to avoid conflicts
            await LoadDockerPortMappings();
            _logger.LogInformation("Initialized port mapping system with existing Docker ports");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to initialize port mappings, will check availability dynamically");
        }
    }

    private async Task LoadDockerPortMappings()
    {
        try
        {
            var dockerClient = await GetDockerClientAsync();
            var containers = await dockerClient.Containers.ListContainersAsync(new ContainersListParameters
            {
                All = true // Include both running and stopped containers
            });

            foreach (var container in containers)
            {
                if (container.Ports != null)
                {
                    foreach (var port in container.Ports)
                    {
                        if (port.PublicPort > 0 && port.PublicPort >= _currentPortStart)
                        {
                            _usedPorts.TryAdd((int)port.PublicPort, true);
                            _logger.LogDebug("Marked port {Port} as used (existing Docker container)", port.PublicPort);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load existing Docker port mappings");
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
                    ["POSTGRES_USER"] = "admin",
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
        int port = 0;
        try
        {
            // Support optional version hints: "key@version" or "key:version"
            string templateKey = request.DatabaseType;
            string? templateVersion = null;
            if (templateKey.Contains('@'))
            {
                var parts = templateKey.Split('@', 2);
                templateKey = parts[0];
                templateVersion = parts[1];
            }
            else if (templateKey.Contains(':'))
            {
                var parts = templateKey.Split(':', 2);
                templateKey = parts[0];
                templateVersion = parts[1];
            }

            // Try resolve from template library first
            DatabaseTemplate? template = await _templateResolver.ResolveAsync(templateKey, templateVersion);
            if (template == null)
            {
                _logger.LogWarning("Template '{TemplateKey}'{Version} not found in library, falling back to built-in templates", templateKey, templateVersion != null ? $"@{templateVersion}" : string.Empty);
                if (!_templates.TryGetValue(templateKey, out template))
                {
                    throw new ArgumentException($"Unknown database type: {request.DatabaseType}");
                }
            }

            // Get Docker client and test connectivity
            var dockerClient = await GetDockerClientAsync();

            // Get an available port with Docker-aware checking
            port = await GetAvailablePortAsync();
            var subdomain = GenerateSubdomain(userId, request.Name, request.DatabaseType);
            var containerName = $"dbmaker-{userId}-{request.DatabaseType}-{request.Name}";

            _logger.LogInformation("Creating container: {ContainerName} on port {Port} with subdomain {Subdomain}", 
                containerName, port, subdomain);

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
            
            // Override with user-provided configuration
            foreach (var config in request.Configuration)
            {
                environment[config.Key] = config.Value;
            }

            // Set database name for PostgreSQL if not provided
            if (request.DatabaseType == "postgresql" && !environment.ContainsKey("POSTGRES_DB"))
            {
                environment["POSTGRES_DB"] = request.Name;
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
                    ["dbmaker.subdomain"] = subdomain,
                    ["dbmaker.port"] = port.ToString(),
                    ["dbmaker.createdAt"] = DateTime.UtcNow.ToString("O")
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
            
            // Wait a bit for the container to start
            await Task.Delay(2000);
            
            // Verify container is running
            var inspectResult = await dockerClient.Containers.InspectContainerAsync(response.ID);
            if (inspectResult.State.Running)
            {
                container.Status = AppContainerStatus.Running;
                
                // Update nginx configuration
                await UpdateNginxConfiguration(subdomain, port, request.DatabaseType);
                
                _logger.LogInformation("Successfully created and started container {ContainerName} for user {UserId} on port {Port}", 
                    containerName, userId, port);
            }
            else
            {
                container.Status = AppContainerStatus.Failed;
                _logger.LogError("Container {ContainerName} failed to start", containerName);
            }

            return container;
        }
        catch (Exception ex)
        {
            // Release the port if container creation failed
            if (port > 0 && _usedPorts.ContainsKey(port))
            {
                _usedPorts.TryRemove(port, out _);
                _logger.LogInformation("Released port {Port} due to container creation failure", port);
            }
            
            _logger.LogError(ex, "Failed to create container for user {UserId}: {Error}", userId, ex.Message);
            throw;
        }
    }

    private async Task UpdateNginxConfiguration(string subdomain, int port, string databaseType)
    {
        try
        {
            var configLine = $"~^{subdomain}\\.mydomain\\.com$ {port};";
            
            // Read existing configuration
            var existingLines = new List<string>();
            if (File.Exists(_nginxConfigPath))
            {
                existingLines = (await File.ReadAllLinesAsync(_nginxConfigPath)).ToList();
            }

            // Remove any existing entry for this subdomain
            existingLines.RemoveAll(line => line.Contains($"~^{subdomain}\\.mydomain\\.com$"));
            
            // Add the new entry
            existingLines.Add(configLine);
            
            // Write back to file
            var configContent = new StringBuilder();
            configContent.AppendLine("# Dynamic upstream configuration file");
            configContent.AppendLine("# This file is managed by the container orchestrator");
            configContent.AppendLine("# Each line maps a subdomain pattern to a port number");
            configContent.AppendLine();
            
            foreach (var line in existingLines.Where(l => !l.StartsWith("#") && !string.IsNullOrWhiteSpace(l)))
            {
                configContent.AppendLine(line);
            }
            
            await File.WriteAllTextAsync(_nginxConfigPath, configContent.ToString());
            
            // Reload nginx configuration
            await ReloadNginxConfiguration();
            
            _logger.LogInformation("Updated nginx configuration for subdomain {Subdomain} -> port {Port}", subdomain, port);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update nginx configuration for subdomain {Subdomain}", subdomain);
        }
    }

    private async Task ReloadNginxConfiguration()
    {
        try
        {
            // This would normally reload nginx configuration
            // For development, we'll just log it
            _logger.LogInformation("Nginx configuration reload requested (development mode)");
            
            // In production, you would do something like:
            // await System.Diagnostics.Process.Start("docker", "exec nginx-container nginx -s reload");
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reload nginx configuration");
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
            
            // Get container info before removing
            var containerInfo = await dockerClient.Containers.InspectContainerAsync(containerId);
            var subdomain = containerInfo.Config.Labels?.TryGetValue("dbmaker.subdomain", out var subdomainValue) == true ? subdomainValue : null;
            var port = containerInfo.Config.Labels?.TryGetValue("dbmaker.port", out var portValue) == true ? portValue : null;
            
            // Remove the container
            await dockerClient.Containers.RemoveContainerAsync(containerId, new ContainerRemoveParameters { Force = true });
            
            // Update nginx configuration to remove the subdomain mapping
            if (!string.IsNullOrEmpty(subdomain))
            {
                await RemoveNginxConfiguration(subdomain);
            }
            
            // Free up the port
            if (!string.IsNullOrEmpty(port) && int.TryParse(port, out var portNum))
            {
                _usedPorts.TryRemove(portNum, out _);
            }
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove container {ContainerId}", containerId);
            return false;
        }
    }

    private async Task RemoveNginxConfiguration(string subdomain)
    {
        try
        {
            if (!File.Exists(_nginxConfigPath)) return;
            
            var existingLines = (await File.ReadAllLinesAsync(_nginxConfigPath)).ToList();
            var updatedLines = existingLines.Where(line => !line.Contains($"~^{subdomain}\\.mydomain\\.com$")).ToList();
            
            var configContent = new StringBuilder();
            configContent.AppendLine("# Dynamic upstream configuration file");
            configContent.AppendLine("# This file is managed by the container orchestrator");
            configContent.AppendLine("# Each line maps a subdomain pattern to a port number");
            configContent.AppendLine();
            
            foreach (var line in updatedLines.Where(l => !l.StartsWith("#") && !string.IsNullOrWhiteSpace(l)))
            {
                configContent.AppendLine(line);
            }
            
            await File.WriteAllTextAsync(_nginxConfigPath, configContent.ToString());
            await ReloadNginxConfiguration();
            
            _logger.LogInformation("Removed nginx configuration for subdomain {Subdomain}", subdomain);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove nginx configuration for subdomain {Subdomain}", subdomain);
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
        // Use the async version but block on it for the interface compatibility
        return GetAvailablePortAsync().GetAwaiter().GetResult();
    }

    public async Task<int> GetAvailablePortAsync()
    {
        // Refresh Docker port mappings before allocating
        await LoadDockerPortMappings();

        _logger.LogInformation("Starting port allocation search from {StartPort}", _currentPortStart);
        _logger.LogDebug("Currently tracking {UsedPortCount} used ports", _usedPorts.Count);

        for (int port = _currentPortStart; port < _currentPortStart + 10000; port++)
        {
            // Skip if we know this port is already used
            if (_usedPorts.ContainsKey(port))
            {
                _logger.LogDebug("Port {Port} is already tracked as used, skipping", port);
                continue;
            }

            // Check if port is available on the system
            if (await IsPortAvailableAsync(port))
            {
                _usedPorts.TryAdd(port, true); // Mark it as used
                _logger.LogInformation("Successfully allocated port {Port} (checked system + Docker)", port);
                return port;
            }
            else
            {
                _logger.LogDebug("Port {Port} is not available on system, marking as used", port);
                _usedPorts.TryAdd(port, true); // Mark unavailable port as used too
            }
        }
        
        var errorMessage = $"No available ports in range {_currentPortStart}-{_currentPortStart + 9999}. " +
                          $"Total tracked ports: {_usedPorts.Count}";
        _logger.LogError(errorMessage);
        throw new InvalidOperationException(errorMessage);
    }

    private async Task<bool> IsPortAvailableAsync(int port)
    {
        try
        {
            // Check system port availability first
            using var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Any, port);
            listener.Start();
            listener.Stop();

            // Additional check: verify Docker isn't using this port
            try
            {
                var dockerClient = await GetDockerClientAsync();
                var containers = await dockerClient.Containers.ListContainersAsync(new ContainersListParameters
                {
                    All = true
                });

                foreach (var container in containers)
                {
                    if (container.Ports != null)
                    {
                        foreach (var dockerPort in container.Ports)
                        {
                            // Check if the port matches - PublicPort is ushort, not nullable
                            if (dockerPort.PublicPort == port)
                            {
                                _logger.LogDebug("Port {Port} is used by Docker container {ContainerId}", port, container.ID);
                                return false;
                            }
                        }
                    }
                }
            }
            catch (Exception dockerEx)
            {
                _logger.LogWarning(dockerEx, "Could not check Docker ports for availability, port {Port} may be in use", port);
                // If Docker check fails, rely on system port check above
            }

            return true;
        }
        catch (System.Net.Sockets.SocketException)
        {
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking port {Port} availability, assuming unavailable", port);
            return false;
        }
    }

    public string GenerateSubdomain(string userId, string containerName, string databaseType)
    {
        // Create a safe subdomain name
        var subdomain = $"{userId}-{containerName}-{databaseType}".ToLower()
            .Replace("_", "-")
            .Replace(" ", "-");
        
        // Remove invalid characters
        subdomain = new string(subdomain.Where(c => char.IsLetterOrDigit(c) || c == '-').ToArray());
        
        // Ensure it doesn't exceed DNS limits
        if (subdomain.Length > 63)
        {
            subdomain = subdomain.Substring(0, 63);
        }
        
        return subdomain;
    }

    private string GenerateConnectionString(DatabaseTemplate template, int port, Dictionary<string, string> environment, string subdomain)
    {
        // Prefer explicit connection string template if provided by the template library
        if (!string.IsNullOrWhiteSpace(template.ConnectionStringTemplate))
        {
            var cs = template.ConnectionStringTemplate!;
            // Common placeholders
            cs = cs.Replace("{HOST_PORT}", port.ToString())
                   .Replace("{SUBDOMAIN}", subdomain)
                   .Replace("{HOST}", $"{subdomain}.mydomain.com")
                   .Replace("{HTTP_PORT}", "80");
            // Replace environment keys e.g., {POSTGRES_USER}
            foreach (var kv in environment)
            {
                cs = cs.Replace("{" + kv.Key + "}", kv.Value);
            }
            return cs;
        }

        // Fallback legacy patterns
        return template.Type switch
        {
            "redis" => $"redis://{subdomain}.mydomain.com:80",
            "postgresql" => $"postgresql://{(environment.TryGetValue("POSTGRES_USER", out var user) ? user : "admin")}:{(environment.TryGetValue("POSTGRES_PASSWORD", out var pass) ? pass : "password")}@{subdomain}.mydomain.com:80/{(environment.TryGetValue("POSTGRES_DB", out var db) ? db : "userdb")}",
            _ => $"{template.Type}://{subdomain}.mydomain.com:80"
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
