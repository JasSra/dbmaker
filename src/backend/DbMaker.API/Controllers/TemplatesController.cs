using Microsoft.AspNetCore.Mvc;
using DbMaker.Shared.Services.Templates;
using DbMaker.Shared.Models;

namespace DbMaker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TemplatesController : ControllerBase
{
    private readonly ILogger<TemplatesController> _logger;
    private readonly ITemplateRepository _repo;
    private readonly ITemplateResolver _resolver;

    public TemplatesController(ILogger<TemplatesController> logger, ITemplateRepository repo, ITemplateResolver resolver)
    {
        _logger = logger;
        _repo = repo;
        _resolver = resolver;
    }

    [HttpGet]
    public async Task<ActionResult<object>> List([FromQuery] string? category = null, [FromQuery] string? q = null, CancellationToken ct = default)
    {
        var items = await _repo.GetAllAsync(category, q, ct);
        var result = items.Select(t => new
        {
            key = t.Key,
            displayName = t.DisplayName,
            category = t.Category,
            icon = t.Icon,
            description = t.Description,
            latestVersion = t.LatestVersion,
            versions = t.Versions.Select(v => v.Version).OrderBy(v => v).ToArray()
        });
        return Ok(result);
    }

    [HttpGet("{key}")]
    public async Task<ActionResult<object>> Detail(string key, CancellationToken ct)
    {
        var t = await _repo.GetByKeyAsync(key, ct);
        if (t == null) return NotFound();
        var result = new
        {
            key = t.Key,
            displayName = t.DisplayName,
            category = t.Category,
            icon = t.Icon,
            description = t.Description,
            latestVersion = t.LatestVersion,
            versions = t.Versions.Select(v => new
            {
                version = v.Version,
                dockerImage = v.DockerImage
            }).OrderBy(v => v.version)
        };
        return Ok(result);
    }

    [HttpGet("{key}/versions/{version}")]
    public async Task<ActionResult<object>> VersionDetail(string key, string version, CancellationToken ct)
    {
        var v = await _repo.GetVersionAsync(key, version, ct);
        if (v == null) return NotFound();
        return Ok(new
        {
            version = v.Version,
            dockerImage = v.DockerImage,
            connectionStringTemplate = v.ConnectionStringTemplate,
            ports = v.Ports,
            volumes = v.Volumes,
            defaultEnvironment = v.DefaultEnvironment,
            defaultConfiguration = v.DefaultConfiguration,
            healthcheck = v.Healthcheck
        });
    }

    [HttpGet("{key}/preview")]
    public async Task<ActionResult<object>> Preview(string key, [FromQuery] string? version = null, [FromQuery] string? overrides = null, CancellationToken ct = default)
    {
        var template = await _resolver.ResolveAsync(key, version, ct);
        if (template == null) return NotFound();
        var env = new Dictionary<string, string>(template.DefaultEnvironment);
        if (!string.IsNullOrWhiteSpace(overrides))
        {
            try
            {
                var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(overrides);
                if (dict != null)
                {
                    foreach (var kv in dict) env[kv.Key] = kv.Value;
                }
            }
            catch { /* ignore parse errors */ }
        }
        // sample values
        var hostPort = template.Ports.FirstOrDefault()?.ContainerPort ?? 0;
        var subdomain = $"{key}-preview";
        var connectionString = (template.ConnectionStringTemplate ?? string.Empty)
            .Replace("{HOST_PORT}", hostPort.ToString())
            .Replace("{SUBDOMAIN}", subdomain);
        foreach (var kv in env)
        {
            connectionString = connectionString.Replace("{" + kv.Key + "}", kv.Value);
        }
        return Ok(new
        {
            resolved = new
            {
                template.Type,
                template.DisplayName,
                template.DockerImage,
                template.Ports,
                template.Volumes
            },
            environment = env,
            connectionString
        });
    }
}
