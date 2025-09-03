using Microsoft.AspNetCore.Mvc;

namespace DbMaker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TemplatesController : ControllerBase
{
    private readonly ILogger<TemplatesController> _logger;

    public TemplatesController(ILogger<TemplatesController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public ActionResult<List<object>> GetAvailableTemplates()
    {
        var templates = new object[]
        {
            new
            {
                Type = "redis",
                DisplayName = "Redis",
                Description = "In-memory data structure store",
                Icon = "redis",
                Category = "Cache",
                DefaultConfiguration = new
                {
                    maxmemory = "256mb",
                    maxmemory_policy = "allkeys-lru"
                }
            },
            new
            {
                Type = "postgresql",
                DisplayName = "PostgreSQL",
                Description = "Advanced open source relational database",
                Icon = "postgresql",
                Category = "Database",
                DefaultConfiguration = new
                {
                    database_name = "userdb",
                    username = "dbuser"
                }
            }
        };

        return Ok(templates);
    }

    [HttpGet("{type}")]
    public ActionResult<object> GetTemplate(string type)
    {
        var template = type.ToLower() switch
        {
            "redis" => new
            {
                Type = "redis",
                DisplayName = "Redis",
                Description = "In-memory data structure store with persistence support",
                Documentation = "https://redis.io/docs/",
                Port = 6379,
                ConfigurationOptions = new object[]
                {
                    new { Name = "maxmemory", Type = "string", Default = "256mb", Description = "Maximum memory limit" },
                    new { Name = "maxmemory-policy", Type = "select", Default = "allkeys-lru", Options = new[] { "allkeys-lru", "volatile-lru", "noeviction" }, Description = "Memory eviction policy" }
                }
            },
            "postgresql" => new
            {
                Type = "postgresql",
                DisplayName = "PostgreSQL",
                Description = "Advanced open source relational database",
                Documentation = "https://www.postgresql.org/docs/",
                Port = 5432,
                ConfigurationOptions = new object[]
                {
                    new { Name = "database_name", Type = "string", Default = "userdb", Description = "Database name" },
                    new { Name = "username", Type = "string", Default = "dbuser", Description = "Database username" }
                }
            },
            _ => (object?)null
        };

        if (template == null)
            return NotFound($"Template '{type}' not found");

        return Ok(template);
    }
}
