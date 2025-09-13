using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using DbMaker.Shared.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace DbMaker.Tests.Integration;

public class EndToEndWorkflowTests : IClassFixture<DbMakerWebApplicationFactory>
{
    private readonly DbMakerWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public EndToEndWorkflowTests(DbMakerWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private string GenerateTestToken(string userId = "e2e-test-user", string email = "e2e@example.com", string name = "E2E Test User")
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
    public async Task CompleteUserJourney_CreateManageAndDeleteContainer()
    {
        // Arrange
        SetAuthorizationHeader();

        // Step 1: Check health endpoint
        var healthResponse = await _client.GetAsync("/api/health");
        healthResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Step 2: Get current user (should create if not exists)
        var userResponse = await _client.GetAsync("/api/users/me");
        userResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var user = JsonSerializer.Deserialize<User>(
            await userResponse.Content.ReadAsStringAsync(), 
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        user.Should().NotBeNull();
        user!.Id.Should().Be("e2e-test-user");

        // Step 3: Get initial user stats (should be empty)
        var initialStatsResponse = await _client.GetAsync("/api/users/me/stats");
        initialStatsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var initialStats = JsonSerializer.Deserialize<UserStatsResponse>(
            await initialStatsResponse.Content.ReadAsStringAsync(), 
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        initialStats.Should().NotBeNull();
        initialStats!.TotalContainers.Should().Be(0);

        // Step 4: Get available templates
        var templatesResponse = await _client.GetAsync("/api/templates");
        templatesResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var templates = JsonSerializer.Deserialize<List<TemplateListItem>>(
            await templatesResponse.Content.ReadAsStringAsync(), 
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        templates.Should().NotBeNull();

        // Step 5: Get initial container list (should be empty)
        var initialContainersResponse = await _client.GetAsync("/api/containers");
        initialContainersResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var initialContainers = JsonSerializer.Deserialize<List<ContainerResponse>>(
            await initialContainersResponse.Content.ReadAsStringAsync(), 
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        initialContainers.Should().NotBeNull();
        initialContainers!.Should().BeEmpty();

        // Step 6: Create a PostgreSQL container
        var createPostgresRequest = new CreateContainerRequest
        {
            Name = "e2e-postgres-test",
            DatabaseType = "postgresql",
            Configuration = new Dictionary<string, string>
            {
                ["POSTGRES_DB"] = "e2etest",
                ["POSTGRES_USER"] = "testuser",
                ["POSTGRES_PASSWORD"] = "testpass123"
            }
        };

        var createPostgresResponse = await _client.PostAsJsonAsync("/api/containers", createPostgresRequest);
        createPostgresResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var postgresContainer = JsonSerializer.Deserialize<ContainerResponse>(
            await createPostgresResponse.Content.ReadAsStringAsync(), 
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        postgresContainer.Should().NotBeNull();
        postgresContainer!.Name.Should().Be("e2e-postgres-test");
        postgresContainer.DatabaseType.Should().Be("postgresql");

        // Step 7: Create a Redis container
        var createRedisRequest = new CreateContainerRequest
        {
            Name = "e2e-redis-test",
            DatabaseType = "redis",
            Configuration = new Dictionary<string, string>()
        };

        var createRedisResponse = await _client.PostAsJsonAsync("/api/containers", createRedisRequest);
        createRedisResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var redisContainer = JsonSerializer.Deserialize<ContainerResponse>(
            await createRedisResponse.Content.ReadAsStringAsync(), 
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        redisContainer.Should().NotBeNull();
        redisContainer!.Name.Should().Be("e2e-redis-test");
        redisContainer.DatabaseType.Should().Be("redis");

        // Step 8: Verify containers list now shows both containers
        var containersResponse = await _client.GetAsync("/api/containers");
        containersResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var containers = JsonSerializer.Deserialize<List<ContainerResponse>>(
            await containersResponse.Content.ReadAsStringAsync(), 
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        containers.Should().NotBeNull();
        containers!.Should().HaveCount(2);

        // Step 9: Get individual container details
        var postgresDetailResponse = await _client.GetAsync($"/api/containers/{postgresContainer.Id}");
        postgresDetailResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var redisDetailResponse = await _client.GetAsync($"/api/containers/{redisContainer.Id}");
        redisDetailResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Step 10: Check updated user stats
        var updatedStatsResponse = await _client.GetAsync("/api/users/me/stats");
        updatedStatsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var updatedStats = JsonSerializer.Deserialize<UserStatsResponse>(
            await updatedStatsResponse.Content.ReadAsStringAsync(), 
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        updatedStats.Should().NotBeNull();
        updatedStats!.TotalContainers.Should().Be(2);
        updatedStats.ContainersByType.Should().HaveCount(2);

        // Step 11: Stop one container
        var stopResponse = await _client.PostAsync($"/api/containers/{postgresContainer.Id}/stop", null);
        stopResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Step 12: Start the stopped container
        var startResponse = await _client.PostAsync($"/api/containers/{postgresContainer.Id}/start", null);
        startResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Step 13: Delete one container
        var deleteResponse = await _client.DeleteAsync($"/api/containers/{redisContainer.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Step 14: Verify container list is updated
        var finalContainersResponse = await _client.GetAsync("/api/containers");
        finalContainersResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var finalContainers = JsonSerializer.Deserialize<List<ContainerResponse>>(
            await finalContainersResponse.Content.ReadAsStringAsync(), 
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        finalContainers.Should().NotBeNull();
        finalContainers!.Should().HaveCount(1);
        finalContainers.First().Id.Should().Be(postgresContainer.Id);

        // Step 15: Verify deleted container returns 404
        var deletedContainerResponse = await _client.GetAsync($"/api/containers/{redisContainer.Id}");
        deletedContainerResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // Step 16: Clean up - delete remaining container
        var finalDeleteResponse = await _client.DeleteAsync($"/api/containers/{postgresContainer.Id}");
        finalDeleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Step 17: Verify all containers are deleted
        var emptyContainersResponse = await _client.GetAsync("/api/containers");
        emptyContainersResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var emptyContainers = JsonSerializer.Deserialize<List<ContainerResponse>>(
            await emptyContainersResponse.Content.ReadAsStringAsync(), 
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        emptyContainers.Should().NotBeNull();
        emptyContainers!.Should().BeEmpty();
    }

    [Fact]
    public async Task MultiUserIsolation_EachUserSeesOnlyTheirContainers()
    {
        // Arrange - Create containers for user 1
        SetAuthorizationHeader(GenerateTestToken("user-1", "user1@example.com", "User One"));
        
        var user1Container = new CreateContainerRequest
        {
            Name = "user1-container",
            DatabaseType = "redis"
        };
        
        var user1Response = await _client.PostAsJsonAsync("/api/containers", user1Container);
        user1Response.StatusCode.Should().Be(HttpStatusCode.Created);

        // Switch to user 2 and create container
        SetAuthorizationHeader(GenerateTestToken("user-2", "user2@example.com", "User Two"));
        
        var user2Container = new CreateContainerRequest
        {
            Name = "user2-container",
            DatabaseType = "postgresql"
        };
        
        var user2Response = await _client.PostAsJsonAsync("/api/containers", user2Container);
        user2Response.StatusCode.Should().Be(HttpStatusCode.Created);

        // Verify user 1 only sees their container
        SetAuthorizationHeader(GenerateTestToken("user-1", "user1@example.com", "User One"));
        var user1ContainersResponse = await _client.GetAsync("/api/containers");
        var user1Containers = JsonSerializer.Deserialize<List<ContainerResponse>>(
            await user1ContainersResponse.Content.ReadAsStringAsync(), 
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        
        user1Containers.Should().NotBeNull();
        user1Containers!.Should().HaveCount(1);
        user1Containers.First().Name.Should().Be("user1-container");

        // Verify user 2 only sees their container
        SetAuthorizationHeader(GenerateTestToken("user-2", "user2@example.com", "User Two"));
        var user2ContainersResponse = await _client.GetAsync("/api/containers");
        var user2Containers = JsonSerializer.Deserialize<List<ContainerResponse>>(
            await user2ContainersResponse.Content.ReadAsStringAsync(), 
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        
        user2Containers.Should().NotBeNull();
        user2Containers!.Should().HaveCount(1);
        user2Containers.First().Name.Should().Be("user2-container");

        // Clean up
        SetAuthorizationHeader(GenerateTestToken("user-1", "user1@example.com", "User One"));
        await _client.DeleteAsync($"/api/containers/{user1Containers.First().Id}");
        
        SetAuthorizationHeader(GenerateTestToken("user-2", "user2@example.com", "User Two"));
        await _client.DeleteAsync($"/api/containers/{user2Containers.First().Id}");
    }
}