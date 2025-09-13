using System.Net;
using System.Text.Json;
using FluentAssertions;
using DbMaker.Shared.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace DbMaker.Tests.Integration;

public class UsersControllerTests : IClassFixture<DbMakerWebApplicationFactory>
{
    private readonly DbMakerWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public UsersControllerTests(DbMakerWebApplicationFactory factory)
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
    public async Task GetCurrentUser_WithValidToken_ReturnsUserInfo()
    {
        // Arrange
        SetAuthorizationHeader();

        // Act
        var response = await _client.GetAsync("/api/users/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var user = JsonSerializer.Deserialize<User>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        
        user.Should().NotBeNull();
        user!.Id.Should().Be("test-user-123");
        user.Email.Should().Be("test@example.com");
        user.Name.Should().Be("Test User");
    }

    [Fact]
    public async Task GetCurrentUser_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange - No auth header

        // Act
        var response = await _client.GetAsync("/api/users/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetUserStats_WithValidToken_ReturnsStats()
    {
        // Arrange
        SetAuthorizationHeader();

        // Act
        var response = await _client.GetAsync("/api/users/me/stats");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var stats = JsonSerializer.Deserialize<UserStatsResponse>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        
        stats.Should().NotBeNull();
        stats!.TotalContainers.Should().BeGreaterThanOrEqualTo(0);
        stats.RunningContainers.Should().BeGreaterThanOrEqualTo(0);
        stats.ContainersByType.Should().NotBeNull();
    }

    [Fact]
    public async Task GetCurrentUser_CreatesNewUserIfNotExists()
    {
        // Arrange
        var newUserId = "new-user-456";
        var newUserEmail = "newuser@example.com";
        var newUserName = "New User";
        
        SetAuthorizationHeader(GenerateTestToken(newUserId, newUserEmail, newUserName));

        // Act
        var response = await _client.GetAsync("/api/users/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var user = JsonSerializer.Deserialize<User>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        
        user.Should().NotBeNull();
        user!.Id.Should().Be(newUserId);
        user.Email.Should().Be(newUserEmail);
        user.Name.Should().Be(newUserName);
        user.IsActive.Should().BeTrue();
    }
}

public class UserStatsResponse
{
    public int TotalContainers { get; set; }
    public int RunningContainers { get; set; }
    public List<ContainerTypeCount> ContainersByType { get; set; } = new();
}

public class ContainerTypeCount
{
    public string DatabaseType { get; set; } = string.Empty;
    public int Count { get; set; }
}