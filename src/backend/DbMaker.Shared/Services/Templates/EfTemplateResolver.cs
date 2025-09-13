using DbMaker.Shared.Models;

namespace DbMaker.Shared.Services.Templates;

public class EfTemplateResolver : ITemplateResolver
{
    private readonly ITemplateRepository _repo;

    public EfTemplateResolver(ITemplateRepository repo)
    {
        _repo = repo;
    }

    public async Task<DatabaseTemplate?> ResolveAsync(string templateKey, string? version = null, CancellationToken ct = default)
    {
        var template = await _repo.GetByKeyAsync(templateKey, ct);
        if (template == null || !template.IsEnabled) return null;

        var resolvedVersion = version;
        if (string.IsNullOrWhiteSpace(resolvedVersion))
        {
            resolvedVersion = template.LatestVersion ?? template.Versions.OrderByDescending(v => v.CreatedAt).FirstOrDefault()?.Version;
        }
        if (string.IsNullOrWhiteSpace(resolvedVersion)) return null;

        var v = template.Versions.FirstOrDefault(x => x.Version == resolvedVersion) ?? await _repo.GetVersionAsync(templateKey, resolvedVersion, ct);
        if (v == null) return null;

        return new DatabaseTemplate
        {
            Type = template.Key,
            DisplayName = template.DisplayName,
            Description = template.Description,
            DockerImage = v.DockerImage,
            Ports = v.Ports,
            Volumes = v.Volumes,
            DefaultEnvironment = v.DefaultEnvironment,
            DefaultConfiguration = v.DefaultConfiguration?.ToDictionary(k => k.Key, k => (object)k.Value) ?? new Dictionary<string, object>(),
            ConnectionStringTemplate = v.ConnectionStringTemplate
        };
    }
}
