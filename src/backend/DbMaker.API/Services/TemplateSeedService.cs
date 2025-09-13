using System.Text.Json;
using DbMaker.Shared.Data;
using DbMaker.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace DbMaker.API.Services;

public class TemplateSeedService
{
    private readonly DbMakerDbContext _db;
    private readonly ILogger<TemplateSeedService> _logger;
    private readonly IWebHostEnvironment _env;

    public TemplateSeedService(DbMakerDbContext db, ILogger<TemplateSeedService> logger, IWebHostEnvironment env)
    {
        _db = db;
        _logger = logger;
        _env = env;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        var seedRoot = Path.Combine(_env.ContentRootPath, "Seed", "templates");
        if (!Directory.Exists(seedRoot))
        {
            _logger.LogInformation("No template seed directory found at {Path}", seedRoot);
            return;
        }

        var indexPath = Path.Combine(seedRoot, "index.json");
        if (!File.Exists(indexPath))
        {
            _logger.LogInformation("No template index.json found in {Path}", seedRoot);
            return;
        }

    var indexJson = await File.ReadAllTextAsync(indexPath, ct);
    var index = JsonSerializer.Deserialize<List<TemplateIndexItem>>(indexJson, _jsonOptions) ?? new();
        _logger.LogInformation("Template index loaded with {Count} items from {IndexPath}", index.Count, indexPath);
        foreach (var item in index)
        {
            try
            {
                _logger.LogInformation("Seeding template {Key}", item.Key);
                await UpsertTemplateAsync(seedRoot, item, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to seed template {Key}", item.Key);
            }
        }
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Template seeding completed");
    }

    private async Task UpsertTemplateAsync(string seedRoot, TemplateIndexItem item, CancellationToken ct)
    {
        var key = item.Key;
        var templateDir = Path.Combine(seedRoot, key);
        var templateMetaPath = Path.Combine(templateDir, "template.json");
        if (!File.Exists(templateMetaPath))
        {
            _logger.LogWarning("template.json missing for template {Key}", key);
            return;
        }
    var meta = JsonSerializer.Deserialize<TemplateMeta>(await File.ReadAllTextAsync(templateMetaPath, ct), _jsonOptions)!;

        var tpl = await _db.Templates.FirstOrDefaultAsync(t => t.Key == key, ct);
        if (tpl == null)
        {
            tpl = new Template
            {
                Key = key,
                DisplayName = meta.DisplayName ?? key,
                Category = meta.Category ?? "Database",
                Icon = meta.Icon ?? $"/template-icons/{key}.svg",
                Description = meta.Description ?? string.Empty,
                IsEnabled = meta.IsEnabled ?? true,
                LatestVersion = meta.LatestVersion
            };
            _db.Templates.Add(tpl);
        }
        else
        {
            tpl.DisplayName = meta.DisplayName ?? tpl.DisplayName;
            tpl.Category = meta.Category ?? tpl.Category;
            tpl.Icon = meta.Icon ?? tpl.Icon;
            tpl.Description = meta.Description ?? tpl.Description;
            tpl.IsEnabled = meta.IsEnabled ?? tpl.IsEnabled;
            tpl.LatestVersion = meta.LatestVersion ?? tpl.LatestVersion;
            tpl.UpdatedAt = DateTime.UtcNow;
        }

        // Versions
        var versionsDir = Path.Combine(templateDir, "versions");
        if (!Directory.Exists(versionsDir)) return;
        foreach (var file in Directory.EnumerateFiles(versionsDir, "*.json"))
        {
            var versionJson = await File.ReadAllTextAsync(file, ct);
            var vmeta = JsonSerializer.Deserialize<TemplateVersionMeta>(versionJson, _jsonOptions);
            if (vmeta == null || string.IsNullOrWhiteSpace(vmeta.Version)) continue;

            var existing = await _db.TemplateVersions.FirstOrDefaultAsync(v => v.TemplateId == tpl.Id && v.Version == vmeta.Version, ct);
            if (existing == null)
            {
                existing = new TemplateVersion
                {
                    TemplateId = tpl.Id,
                    Version = vmeta.Version,
                    DockerImage = vmeta.DockerImage ?? string.Empty,
                    ConnectionStringTemplate = vmeta.ConnectionStringTemplate ?? string.Empty,
                    Ports = vmeta.Ports ?? new(),
                    Volumes = vmeta.Volumes ?? new(),
                    DefaultEnvironment = vmeta.DefaultEnvironment ?? new(),
                    DefaultConfiguration = vmeta.DefaultConfiguration ?? new(),
                    Healthcheck = vmeta.Healthcheck
                };
                _db.TemplateVersions.Add(existing);
            }
            else
            {
                existing.DockerImage = vmeta.DockerImage ?? existing.DockerImage;
                existing.ConnectionStringTemplate = vmeta.ConnectionStringTemplate ?? existing.ConnectionStringTemplate;
                existing.Ports = vmeta.Ports ?? existing.Ports;
                existing.Volumes = vmeta.Volumes ?? existing.Volumes;
                existing.DefaultEnvironment = vmeta.DefaultEnvironment ?? existing.DefaultEnvironment;
                existing.DefaultConfiguration = vmeta.DefaultConfiguration ?? existing.DefaultConfiguration;
                existing.Healthcheck = vmeta.Healthcheck ?? existing.Healthcheck;
            }
        }

        // fall back LatestVersion
        if (string.IsNullOrWhiteSpace(tpl.LatestVersion))
        {
            var latest = await _db.TemplateVersions.Where(v => v.TemplateId == tpl.Id).OrderByDescending(v => v.CreatedAt).Select(v => v.Version).FirstOrDefaultAsync(ct);
            tpl.LatestVersion = latest ?? tpl.LatestVersion;
        }
    }

    private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };

    private sealed record TemplateIndexItem(string Key);

    private sealed class TemplateMeta
    {
        public string? DisplayName { get; set; }
        public string? Category { get; set; }
        public string? Icon { get; set; }
        public string? Description { get; set; }
        public bool? IsEnabled { get; set; }
        public string? LatestVersion { get; set; }
    }

    private sealed class TemplateVersionMeta
    {
        public string? Version { get; set; }
        public string? DockerImage { get; set; }
        public string? ConnectionStringTemplate { get; set; }
        public List<PortMapping>? Ports { get; set; }
        public List<VolumeMapping>? Volumes { get; set; }
        public Dictionary<string, string>? DefaultEnvironment { get; set; }
        public Dictionary<string, string>? DefaultConfiguration { get; set; }
        public HealthcheckSpec? Healthcheck { get; set; }
    }
}
