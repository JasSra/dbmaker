using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using DbMaker.Shared.Models;
using Microsoft.Extensions.DependencyInjection;
using DbMaker.Shared.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace DbMaker.Tests.Integration;

public class ContainersControllerTests : IClassFixture<DbMakerWebApplicationFactory>
{
    private readonly DbMakerWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ContainersControllerTests(DbMakerWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private string GenerateTestToken(string userId = "test-user-123", string email = "test@example.com", string name = "Test User")
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes("this-is-a-test-key-for-jwt-token-generation-with-at-least-256-bits");
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("oid", userId),
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Name, name)
            }),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    private void SetAuthorizationHeader(string? token = null)
    {
        token ??= GenerateTestToken();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }

    [Fact]
    public async Task GetContainers_WithValidUser_ReturnsContainersList()
    {
        // Arrange
        SetAuthorizationHeader();

        // Act
        var response = await _client.GetAsync("/api/containers");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
        
        var containers = JsonSerializer.Deserialize<List<ContainerResponse>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        containers.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateContainer_WithValidRequest_ReturnsCreatedContainer()
    {
        // Arrange
        SetAuthorizationHeader();
        var createRequest = new CreateContainerRequest
        {
            Name = "test-postgres-db",
            DatabaseType = "postgresql",
            Configuration = new Dictionary<string, string>
            {
                ["POSTGRES_DB"] = "testdb",
                ["POSTGRES_USER"] = "testuser",
                ["POSTGRES_PASSWORD"] = "testpass"
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/containers", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var content = await response.Content.ReadAsStringAsync();
        var container = JsonSerializer.Deserialize<ContainerResponse>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        
        container.Should().NotBeNull();
        container!.Name.Should().Be("test-postgres-db");
        container.DatabaseType.Should().Be("postgresql");
        container.Status.Should().Be("Creating");
    }

    [Fact]
    public async Task CreateContainer_WithInvalidRequest_ReturnsBadRequest()
    {
        // Arrange
        SetAuthorizationHeader();
        var invalidRequest = new CreateContainerRequest
        {
            Name = "", // Empty name should be invalid
            DatabaseType = "postgresql"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/containers", invalidRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetContainer_WithValidId_ReturnsContainer()
    {
        // Arrange
        SetAuthorizationHeader();
        
        // First create a container
        var createRequest = new CreateContainerRequest
        {
            Name = "test-redis-db",
            DatabaseType = "redis",
            Configuration = new Dictionary<string, string>()
        };
        
        var createResponse = await _client.PostAsJsonAsync("/api/containers", createRequest);
        var createdContainer = JsonSerializer.Deserialize<ContainerResponse>(
            await createResponse.Content.ReadAsStringAsync(), 
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        // Act
        var response = await _client.GetAsync($"/api/containers/{createdContainer!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var container = JsonSerializer.Deserialize<ContainerResponse>(
            await response.Content.ReadAsStringAsync(), 
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        
        container.Should().NotBeNull();
        container!.Id.Should().Be(createdContainer.Id);
        container.Name.Should().Be("test-redis-db");
    }

    [Fact]
    public async Task GetContainer_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        SetAuthorizationHeader();

        // Act
        var response = await _client.GetAsync("/api/containers/non-existent-id");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task StopContainer_WithValidId_ReturnsNoContent()
    {
        // Arrange
        SetAuthorizationHeader();
        
        // First create a container
        var createRequest = new CreateContainerRequest
        {
            Name = "test-stop-container",
            DatabaseType = "postgresql"
        };
        
        var createResponse = await _client.PostAsJsonAsync("/api/containers", createRequest);
        var createdContainer = JsonSerializer.Deserialize<ContainerResponse>(
            await createResponse.Content.ReadAsStringAsync(), 
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        // Act
        var response = await _client.PostAsync($"/api/containers/{createdContainer!.Id}/stop", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteContainer_WithValidId_ReturnsNoContent()
    {
        // Arrange
        SetAuthorizationHeader();
        
        // First create a container
        var createRequest = new CreateContainerRequest
        {
            Name = "test-delete-container",
            DatabaseType = "redis"
        };
        
        var createResponse = await _client.PostAsJsonAsync("/api/containers", createRequest);
        var createdContainer = JsonSerializer.Deserialize<ContainerResponse>(
            await createResponse.Content.ReadAsStringAsync(), 
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        // Act
        var response = await _client.DeleteAsync($"/api/containers/{createdContainer!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task GetContainers_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange - No auth header

        // Act
        var response = await _client.GetAsync("/api/containers");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

public class CreateContainerRequest
{
    public string Name { get; set; } = string.Empty;
    public string DatabaseType { get; set; } = string.Empty;
    public Dictionary<string, string> Configuration { get; set; } = new();
}

public class ContainerResponse
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DatabaseType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string ConnectionString { get; set; } = string.Empty;
    public string Subdomain { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public Dictionary<string, string> Configuration { get; set; } = new();
}