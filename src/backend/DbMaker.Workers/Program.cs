using DbMaker.Workers;
using DbMaker.Shared.Data;
using DbMaker.Shared.Services;
using DbMaker.Shared.Services.Templates;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

// Add Entity Framework
builder.Services.AddDbContext<DbMakerDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=dbmaker.db"));

// Add services
builder.Services.AddScoped<IContainerOrchestrator, ContainerOrchestrator>();
builder.Services.AddScoped<ITemplateRepository, EfTemplateRepository>();
builder.Services.AddScoped<ITemplateResolver, EfTemplateResolver>();

// Add workers
builder.Services.AddHostedService<ContainerMonitoringWorker>();
builder.Services.AddHostedService<ContainerCleanupWorker>();

var host = builder.Build();
host.Run();
