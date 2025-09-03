using DbMaker.Shared.Data;
using DbMaker.Shared.Services;
using DbMaker.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using AppContainerStatus = DbMaker.Shared.Models.ContainerStatus;

namespace DbMaker.Workers;

public class ContainerMonitoringWorker : BackgroundService
{
    private readonly ILogger<ContainerMonitoringWorker> _logger;
    private readonly IServiceProvider _serviceProvider;

    public ContainerMonitoringWorker(ILogger<ContainerMonitoringWorker> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Container Monitoring Worker starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<DbMakerDbContext>();
                var orchestrator = scope.ServiceProvider.GetRequiredService<IContainerOrchestrator>();

                await MonitorContainers(context, orchestrator);
                
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in monitoring worker");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }

    private async Task MonitorContainers(DbMakerDbContext context, IContainerOrchestrator orchestrator)
    {
        var containers = await context.DatabaseContainers
            .Where(c => c.Status != AppContainerStatus.Removing)
            .ToListAsync();

        foreach (var container in containers)
        {
            try
            {
                var stats = await orchestrator.GetContainerStatsAsync(container.ContainerId);
                if (stats != null)
                {
                    // Update container status if it has changed
                    if (container.Status != stats.Status)
                    {
                        _logger.LogInformation("Container {ContainerId} status changed from {OldStatus} to {NewStatus}",
                            container.Id, container.Status, stats.Status);
                        
                        container.Status = stats.Status;
                        container.LastAccessedAt = DateTime.UtcNow;
                        await context.SaveChangesAsync();
                    }

                    // Log unhealthy containers
                    if (!stats.IsHealthy)
                    {
                        _logger.LogWarning("Container {ContainerId} is unhealthy: {ErrorMessage}",
                            container.Id, stats.ErrorMessage);
                    }
                }
                else
                {
                    // Container not found, mark as failed
                    if (container.Status != AppContainerStatus.Failed)
                    {
                        _logger.LogWarning("Container {ContainerId} not found, marking as failed", container.Id);
                        container.Status = AppContainerStatus.Failed;
                        await context.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error monitoring container {ContainerId}", container.Id);
            }
        }
    }
}

public class ContainerCleanupWorker : BackgroundService
{
    private readonly ILogger<ContainerCleanupWorker> _logger;
    private readonly IServiceProvider _serviceProvider;

    public ContainerCleanupWorker(ILogger<ContainerCleanupWorker> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Container Cleanup Worker starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<DbMakerDbContext>();
                var orchestrator = scope.ServiceProvider.GetRequiredService<IContainerOrchestrator>();

                await CleanupInactiveContainers(context, orchestrator);
                await RemoveFailedContainers(context, orchestrator);
                
                // Run cleanup every hour
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in cleanup worker");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }

    private async Task CleanupInactiveContainers(DbMakerDbContext context, IContainerOrchestrator orchestrator)
    {
        var inactiveThreshold = DateTime.UtcNow.AddDays(-7); // 7 days of inactivity
        
        var inactiveContainers = await context.DatabaseContainers
            .Where(c => c.LastAccessedAt < inactiveThreshold && 
                       c.Status == AppContainerStatus.Running)
            .ToListAsync();

        foreach (var container in inactiveContainers)
        {
            try
            {
                _logger.LogInformation("Stopping inactive container {ContainerId}", container.Id);
                await orchestrator.StopContainerAsync(container.ContainerId);
                container.Status = AppContainerStatus.Stopped;
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to stop inactive container {ContainerId}", container.Id);
            }
        }
    }

    private async Task RemoveFailedContainers(DbMakerDbContext context, IContainerOrchestrator orchestrator)
    {
        var removalThreshold = DateTime.UtcNow.AddDays(-30); // Remove failed containers after 30 days
        
        var failedContainers = await context.DatabaseContainers
            .Where(c => c.Status == AppContainerStatus.Failed && 
                       c.CreatedAt < removalThreshold)
            .ToListAsync();

        foreach (var container in failedContainers)
        {
            try
            {
                _logger.LogInformation("Removing failed container {ContainerId}", container.Id);
                await orchestrator.RemoveContainerAsync(container.ContainerId);
                context.DatabaseContainers.Remove(container);
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove failed container {ContainerId}", container.Id);
            }
        }
    }
}
