using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using DbMaker.Shared.Data;
using DbMaker.Shared.Services;
using Moq;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;

namespace DbMaker.Tests.Integration;

public class DbMakerWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the app's DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<DbMakerDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Add DbContext using an in-memory database for testing
            services.AddDbContext<DbMakerDbContext>(options =>
            {
                options.UseInMemoryDatabase("InMemoryDbForTesting");
            });

            // Mock the container orchestrator
            var mockOrchestrator = new Mock<IContainerOrchestrator>();
            mockOrchestrator.Setup(x => x.CreateContainerAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
                .ReturnsAsync(new Shared.Models.ContainerCreationResult 
                { 
                    Success = true, 
                    ContainerId = "test-container-id",
                    Port = 5432,
                    ConnectionString = "Host=localhost;Port=5432;Database=testdb;Username=testuser;Password=testpass"
                });

            mockOrchestrator.Setup(x => x.StopContainerAsync(It.IsAny<string>()))
                .ReturnsAsync(true);

            mockOrchestrator.Setup(x => x.DeleteContainerAsync(It.IsAny<string>()))
                .ReturnsAsync(true);

            services.AddSingleton(mockOrchestrator.Object);

            // Configure authentication for testing
            services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.Authority = "https://test-authority.com";
                options.Audience = "test-audience";
                options.RequireHttpsMetadata = false;
                options.TokenValidationParameters.ValidateIssuer = false;
                options.TokenValidationParameters.ValidateAudience = false;
                options.TokenValidationParameters.ValidateLifetime = false;
                options.TokenValidationParameters.ValidateIssuerSigningKey = false;
                options.TokenValidationParameters.SignatureValidator = (token, parameters) => new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(token);
            });

            var sp = services.BuildServiceProvider();

            // Create the database and apply any pending migrations
            using var scope = sp.CreateScope();
            var scopedServices = scope.ServiceProvider;
            var db = scopedServices.GetRequiredService<DbMakerDbContext>();
            var logger = scopedServices.GetRequiredService<ILogger<DbMakerWebApplicationFactory>>();

            db.Database.EnsureCreated();

            try
            {
                // Seed test data if needed
                SeedTestData(db);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred seeding the database with test data. Error: {Message}", ex.Message);
            }
        });

        builder.UseEnvironment("Testing");
    }

    private static void SeedTestData(DbMakerDbContext context)
    {
        // Add any test seed data here
        if (!context.Users.Any())
        {
            context.Users.Add(new Shared.Models.User
            {
                Id = "test-user-123",
                Email = "test@example.com",
                Name = "Test User",
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow,
                IsActive = true
            });

            context.SaveChanges();
        }
    }
}