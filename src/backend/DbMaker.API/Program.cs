using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using DbMaker.Shared.Data;
using DbMaker.Shared.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add CORS for Angular frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularDev",
        policy =>
        {
            policy.WithOrigins("http://localhost:4200", "https://console.mydomain.com")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
});

// Add Entity Framework
builder.Services.AddDbContext<DbMakerDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add custom services
builder.Services.AddScoped<IContainerOrchestrator, ContainerOrchestrator>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAngularDev");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Server-Sent Events endpoint for monitoring
app.MapGet("/api/monitoring/events", async (HttpContext context) =>
{
    context.Response.Headers["Content-Type"] = "text/event-stream";
    context.Response.Headers["Cache-Control"] = "no-cache";
    context.Response.Headers["Connection"] = "keep-alive";

    var orchestrator = context.RequestServices.GetRequiredService<IContainerOrchestrator>();
    
    while (!context.RequestAborted.IsCancellationRequested)
    {
        try
        {
            var stats = await orchestrator.GetAllContainerStatsAsync();
            var data = System.Text.Json.JsonSerializer.Serialize(stats);
            
            await context.Response.WriteAsync($"data: {data}\n\n");
            await context.Response.Body.FlushAsync();
            
            await Task.Delay(5000, context.RequestAborted); // Update every 5 seconds
        }
        catch (OperationCanceledException)
        {
            break;
        }
        catch (Exception ex)
        {
            await context.Response.WriteAsync($"data: {{\"error\": \"{ex.Message}\"}}\n\n");
            await context.Response.Body.FlushAsync();
            await Task.Delay(5000, context.RequestAborted);
        }
    }
});

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<DbMakerDbContext>();
    context.Database.EnsureCreated();
}

app.Run();
