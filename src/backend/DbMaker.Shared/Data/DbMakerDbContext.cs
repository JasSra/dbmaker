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
    }
}
