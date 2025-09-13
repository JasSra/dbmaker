using Microsoft.EntityFrameworkCore;
using DbMaker.Shared.Models;

namespace DbMaker.Shared.Data;

public class DbMakerDbContext : DbContext
{
    public DbMakerDbContext(DbContextOptions<DbMakerDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<DatabaseContainer> DatabaseContainers { get; set; }
    public DbSet<SystemSettings> SystemSettings { get; set; }
    public DbSet<Template> Templates { get; set; }
    public DbSet<TemplateVersion> TemplateVersions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.HasIndex(e => e.Email).IsUnique();
            
            entity.HasMany(e => e.Containers)
                  .WithOne(e => e.User)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // DatabaseContainer configuration
        modelBuilder.Entity<DatabaseContainer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.DatabaseType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ContainerName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.ContainerId).HasMaxLength(200);
            entity.Property(e => e.ConnectionString).HasMaxLength(500);
            entity.Property(e => e.Subdomain).HasMaxLength(100);
            
            // Convert enum to string
            entity.Property(e => e.Status)
                  .HasConversion<string>();
            
            // Store Configuration as JSON
            entity.Property(e => e.Configuration)
                  .HasConversion(
                      v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                      v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new Dictionary<string, string>());
            
            // Indexes for performance
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.ContainerId);
            entity.HasIndex(e => e.Subdomain).IsUnique();
            entity.HasIndex(e => new { e.UserId, e.Name }).IsUnique();
        });

        // SystemSettings configuration
        modelBuilder.Entity<SystemSettings>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).HasMaxLength(255);
            
            // Store complex objects as JSON
            entity.Property(e => e.Docker)
                  .HasConversion(
                      v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                      v => System.Text.Json.JsonSerializer.Deserialize<DockerSettings>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new DockerSettings());

            entity.Property(e => e.UI)
                  .HasConversion(
                      v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                      v => System.Text.Json.JsonSerializer.Deserialize<UISettings>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new UISettings());

            entity.Property(e => e.Nginx)
                  .HasConversion(
                      v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                      v => System.Text.Json.JsonSerializer.Deserialize<NginxSettings>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new NginxSettings());

            entity.Property(e => e.Containers)
                  .HasConversion(
                      v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                      v => System.Text.Json.JsonSerializer.Deserialize<ContainerSettings>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new ContainerSettings());

            entity.HasIndex(e => e.UserId);
        });

        // Template configuration
        modelBuilder.Entity<Template>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Key).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.Key).IsUnique();
            entity.Property(e => e.DisplayName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Category).HasMaxLength(100);
            entity.Property(e => e.Icon).HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            // Default handled via CLR default value
            entity.Property(e => e.LatestVersion).HasMaxLength(100);

            entity.HasMany(e => e.Versions)
                  .WithOne(v => v.Template)
                  .HasForeignKey(v => v.TemplateId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // TemplateVersion configuration
        modelBuilder.Entity<TemplateVersion>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TemplateId).IsRequired();
            entity.Property(e => e.Version).IsRequired().HasMaxLength(100);
            entity.Property(e => e.DockerImage).IsRequired().HasMaxLength(200);
            entity.Property(e => e.ConnectionStringTemplate).HasMaxLength(1000);

            // JSON serialize complex props
            entity.Property(e => e.Ports)
                  .HasConversion(
                      v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                      v => System.Text.Json.JsonSerializer.Deserialize<List<PortMapping>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<PortMapping>());

            entity.Property(e => e.Volumes)
                  .HasConversion(
                      v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                      v => System.Text.Json.JsonSerializer.Deserialize<List<VolumeMapping>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<VolumeMapping>());

            entity.Property(e => e.DefaultEnvironment)
                  .HasConversion(
                      v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                      v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new Dictionary<string, string>());

            entity.Property(e => e.DefaultConfiguration)
                  .HasConversion(
                      v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                      v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new Dictionary<string, string>());

            entity.Property(e => e.Healthcheck)
                  .HasConversion(
                      v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                      v => System.Text.Json.JsonSerializer.Deserialize<HealthcheckSpec>(v, (System.Text.Json.JsonSerializerOptions?)null));

            entity.HasIndex(e => new { e.TemplateId, e.Version }).IsUnique();
        });
    }
}
