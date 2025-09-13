using System.Net;
using FluentAssertions;

namespace DbMaker.Tests.Integration;

public class HealthControllerTests : IClassFixture<DbMakerWebApplicationFactory>
{
    private readonly DbMakerWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public HealthControllerTests(DbMakerWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetHealth_ReturnsHealthy()
    {
        // Act
        var response = await _client.GetAsync("/api/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("healthy", StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetHealth_ShouldNotRequireAuthentication()
    {
        // Arrange - No auth header

        // Act
        var response = await _client.GetAsync("/api/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}