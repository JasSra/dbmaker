using DbMaker.Shared.Models;

namespace DbMaker.Shared.Services;

public interface IContainerOrchestrator : IDisposable
{
    Task<DatabaseContainer> CreateContainerAsync(string userId, CreateContainerRequest request);
    Task<bool> StartContainerAsync(string containerId);
    Task<bool> StopContainerAsync(string containerId);
    Task<bool> RemoveContainerAsync(string containerId);
    Task<ContainerMonitoringData?> GetContainerStatsAsync(string containerId);
    Task<List<ContainerMonitoringData>> GetAllContainerStatsAsync();
    int GetAvailablePort();
    Task<int> GetAvailablePortAsync();
    string GenerateSubdomain(string userId, string containerName, string databaseType);
}
