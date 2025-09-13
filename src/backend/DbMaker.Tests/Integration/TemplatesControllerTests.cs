using System.Net;
using System.Text.Json;
using FluentAssertions;

namespace DbMaker.Tests.Integration;

public class TemplatesControllerTests : IClassFixture<DbMakerWebApplicationFactory>
{
    private readonly DbMakerWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public TemplatesControllerTests(DbMakerWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetTemplates_ReturnsTemplatesList()
    {
        // Act
        var response = await _client.GetAsync("/api/templates");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
        
        var templates = JsonSerializer.Deserialize<List<TemplateListItem>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        templates.Should().NotBeNull();
    }

    [Fact]
    public async Task GetTemplates_WithCategoryFilter_ReturnsFilteredTemplates()
    {
        // Act
        var response = await _client.GetAsync("/api/templates?category=database");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var templates = JsonSerializer.Deserialize<List<TemplateListItem>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        
        templates.Should().NotBeNull();
    }

    [Fact]
    public async Task GetTemplates_WithSearchQuery_ReturnsFilteredTemplates()
    {
        // Act
        var response = await _client.GetAsync("/api/templates?q=postgres");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var templates = JsonSerializer.Deserialize<List<TemplateListItem>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        
        templates.Should().NotBeNull();
    }

    [Fact]
    public async Task GetTemplate_WithValidKey_ReturnsTemplateDetail()
    {
        // First get list of templates to get a valid key
        var listResponse = await _client.GetAsync("/api/templates");
        var listContent = await listResponse.Content.ReadAsStringAsync();
        var templates = JsonSerializer.Deserialize<List<TemplateListItem>>(listContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        
        if (templates?.Any() == true)
        {
            var templateKey = templates.First().Key;
            
            // Act
            var response = await _client.GetAsync($"/api/templates/{templateKey}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            var template = JsonSerializer.Deserialize<TemplateDetail>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            template.Should().NotBeNull();
            template!.Key.Should().Be(templateKey);
        }
    }

    [Fact]
    public async Task GetTemplate_WithInvalidKey_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/templates/non-existent-template");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ResolveTemplate_WithValidKey_ReturnsResolvedTemplate()
    {
        // First get list of templates to get a valid key
        var listResponse = await _client.GetAsync("/api/templates");
        var listContent = await listResponse.Content.ReadAsStringAsync();
        var templates = JsonSerializer.Deserialize<List<TemplateListItem>>(listContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        
        if (templates?.Any() == true)
        {
            var templateKey = templates.First().Key;
            
            // Act
            var response = await _client.GetAsync($"/api/templates/{templateKey}/resolve");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotBeNullOrEmpty();
        }
    }
}

public class TemplateListItem
{
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string LatestVersion { get; set; } = string.Empty;
    public string[] Versions { get; set; } = Array.Empty<string>();
}

public class TemplateDetail
{
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}