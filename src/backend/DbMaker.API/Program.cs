using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using System.Text.Json.Serialization;
using DbMaker.Shared.Data;
using DbMaker.Shared.Services;
using DbMaker.API.Authentication;
using Serilog;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using DbMaker.Shared.Services.Templates;
using DbMaker.API.Services;

// Build configuration first to get Serilog settings
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json")
    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

// Configure Serilog from configuration
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("ApplicationName", "DbMaker.API")
    .Enrich.WithProperty("Environment", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production")
    .CreateLogger();

try
{
    Log.Information("Starting DbMaker API application");

    var builder = WebApplication.CreateBuilder(args);

    // Use Serilog for logging
    builder.Host.UseSerilog();

    // Configure OpenTelemetry
    var serviceName = builder.Configuration["OpenTelemetry:ServiceName"] ?? "DbMaker.API";
    var serviceVersion = builder.Configuration["OpenTelemetry:ServiceVersion"] ?? "1.0.0";
    var otlpEndpoint = builder.Configuration["OpenTelemetry:OtlpExporter:Endpoint"] ?? "http://localhost:3001";
    var otlpHeaders = builder.Configuration["OpenTelemetry:OtlpExporter:Headers"] ?? "x-source=dbmaker-api";

    builder.Services.AddOpenTelemetry()
        .ConfigureResource(resource => resource
            .AddService(serviceName: serviceName, serviceVersion: serviceVersion)
            .AddAttributes(new Dictionary<string, object>
            {
                ["deployment.environment"] = builder.Environment.EnvironmentName,
                ["service.instance.id"] = Environment.MachineName
            }))
        .WithTracing(tracing => tracing
            .AddAspNetCoreInstrumentation(options =>
            {
                options.RecordException = true;
                options.Filter = (httpContext) =>
                {
                    // Don't trace health checks and other noisy endpoints
                    var path = httpContext.Request.Path.Value?.ToLower();
                    return !path?.Contains("/health") == true && !path?.Contains("/metrics") == true;
                };
                options.EnrichWithHttpRequest = (activity, httpRequest) =>
                {
                    activity.SetTag("http.request_content_length", httpRequest.ContentLength);
                    activity.SetTag("http.user_agent", httpRequest.Headers.UserAgent.ToString());
                };
                options.EnrichWithHttpResponse = (activity, httpResponse) =>
                {
                    activity.SetTag("http.response_content_length", httpResponse.ContentLength);
                };
            })
            .AddHttpClientInstrumentation(options =>
            {
                options.RecordException = true;
                options.FilterHttpRequestMessage = (httpRequestMessage) =>
                {
                    // Don't trace requests to OTLP endpoint to avoid circular tracing
                    return !httpRequestMessage.RequestUri?.ToString().Contains("localhost:3001") == true;
                };
            })
            .AddEntityFrameworkCoreInstrumentation(options =>
            {
                options.SetDbStatementForText = true;
                options.SetDbStatementForStoredProcedure = true;
                options.EnrichWithIDbCommand = (activity, command) =>
                {
                    activity.SetTag("db.operation", command.CommandType.ToString());
                };
            })
            .AddOtlpExporter(options =>
            {
                options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
                options.Endpoint = new Uri(otlpEndpoint);
                options.Headers = otlpHeaders;
            }))
        .WithMetrics(metrics => metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddMeter("DbMaker.API") // Custom meter for application-specific metrics
            .AddOtlpExporter(options =>
            {
                options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
                options.Endpoint = new Uri(otlpEndpoint);
                options.Headers = otlpHeaders;
            }))
        .WithLogging(logging => logging
            .AddOtlpExporter(options =>
            {
                options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
                options.Endpoint = new Uri(otlpEndpoint);
                options.Headers = otlpHeaders;
            }));

    // Add services to the container.
    
    // Configure Azure AD B2C configuration
    var azureAdConfig = builder.Configuration.GetSection("AzureAd");
    var idpConfiguration = IdpConfiguration.FromAzureAdB2C(
        name: "AzureAdB2C",
        instance: azureAdConfig["Instance"]!,
        tenantId: azureAdConfig["Domain"]!,
        clientId: azureAdConfig["ClientId"]!,
        policyId: azureAdConfig["SignUpSignInPolicyId"]!
    );

    // Register services
    builder.Services.AddHttpClient<ITokenValidator, OidcTokenValidator>();
    builder.Services.AddSingleton(idpConfiguration);
    builder.Services.AddScoped<ITokenValidator, OidcTokenValidator>();

    // Configure custom authentication
    builder.Services.AddAuthentication("CustomJwt")
        .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, CustomJwtAuthenticationHandler>(
            "CustomJwt", options => { });

    builder.Services.AddAuthorization();
    
    builder.Services.AddControllers().AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new() { 
            Title = "DbMaker API", 
            Version = "v1",
            Description = "Database Container Orchestration API"
        });
        
        // Add JWT authentication to Swagger
        c.AddSecurityDefinition("Bearer", new()
        {
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Enter JWT Bearer token"
        });
        
        c.AddSecurityRequirement(new()
        {
            {
                new()
                {
                    Reference = new() { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" }
                },
                Array.Empty<string>()
            }
        });
        
        // Include XML comments
        var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
        {
            c.IncludeXmlComments(xmlPath);
        }
    });

    // Add CORS for Angular frontend
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAngularDev",
            policy =>
            {
                policy.WithOrigins("http://localhost:4200", "http://localhost:56697", "https://*.servicestack.com.au")
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials();
            });
    });

    // Add Entity Framework (ensure SQLite path is anchored to the API content root)
    builder.Services.AddDbContext<DbMakerDbContext>(options =>
    {
        var rawCstr = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=dbmaker.db";
        // If using a relative SQLite Data Source, anchor it to the content root to avoid cwd differences
        if (rawCstr.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase))
        {
            var dataSource = rawCstr.Substring("Data Source=".Length).Trim();
            // If it's not rooted, combine with content root
            if (!Path.IsPathRooted(dataSource))
            {
                var anchoredPath = Path.Combine(builder.Environment.ContentRootPath, dataSource);
                rawCstr = $"Data Source={anchoredPath}";
            }
        }

        options.UseSqlite(rawCstr, b => b.MigrationsAssembly("DbMaker.API"));
    });

    // Add custom services
    builder.Services.AddScoped<IContainerOrchestrator, ContainerOrchestrator>();
    builder.Services.AddScoped<ITemplateRepository, EfTemplateRepository>();
    builder.Services.AddScoped<ITemplateResolver, EfTemplateResolver>();
    builder.Services.AddScoped<TemplateSeedService>();

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    // Add Serilog request logging
    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
        options.GetLevel = (httpContext, elapsed, ex) =>
        {
            if (ex != null) return Serilog.Events.LogEventLevel.Error;
            if (httpContext.Response.StatusCode > 499) return Serilog.Events.LogEventLevel.Error;
            if (httpContext.Response.StatusCode > 399) return Serilog.Events.LogEventLevel.Warning;
            if (elapsed > 10000) return Serilog.Events.LogEventLevel.Warning; // Slow requests
            return Serilog.Events.LogEventLevel.Information;
        };
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
            diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
            diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());
            diagnosticContext.Set("RemoteIpAddress", httpContext.Connection.RemoteIpAddress?.ToString());
            diagnosticContext.Set("UserId", httpContext.User?.Identity?.Name);
            diagnosticContext.Set("TraceId", System.Diagnostics.Activity.Current?.TraceId.ToString());
        };
    });

    // Disable HTTPS redirection for local development
    // app.UseHttpsRedirection();
    // Serve static files (for template icons, etc.)
    app.UseStaticFiles();
    app.UseCors("AllowAngularDev");
    
    // Add authentication debugging middleware
    app.Use(async (context, next) =>
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
        
        if (!string.IsNullOrEmpty(authHeader))
        {
            logger.LogInformation("🔐 Incoming request with Authorization header: {Path} - {AuthPreview}", 
                context.Request.Path, 
                authHeader.Length > 20 ? authHeader[..20] + "..." : authHeader);
        }
        else
        {
            logger.LogDebug("🔓 Request without Authorization header: {Path}", context.Request.Path);
        }
        
        await next();
        
        logger.LogDebug("🔍 After auth pipeline - IsAuthenticated: {IsAuth}, User: {User}", 
            context.User?.Identity?.IsAuthenticated ?? false,
            context.User?.Identity?.Name ?? "Anonymous");
    });
    
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    // Server-Sent Events endpoint for monitoring
    app.MapGet("/api/monitoring/events", async (HttpContext context) =>
    {
        if (!context.User?.Identity?.IsAuthenticated ?? true)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }
        context.Response.Headers["Content-Type"] = "text/event-stream";
        context.Response.Headers["Cache-Control"] = "no-cache";
        context.Response.Headers["Connection"] = "keep-alive";

        var orchestrator = context.RequestServices.GetRequiredService<IContainerOrchestrator>();
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        
        logger.LogInformation("Starting monitoring events stream for user {UserId}", context.User?.Identity?.Name);
        
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
                logger.LogInformation("Monitoring events stream cancelled for user {UserId}", context.User?.Identity?.Name);
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in monitoring events stream for user {UserId}", context.User?.Identity?.Name);
                await context.Response.WriteAsync($"data: {{\"error\": \"{ex.Message}\"}}\n\n");
                await context.Response.Body.FlushAsync();
                await Task.Delay(5000, context.RequestAborted);
            }
        }
    });

    // Ensure database is up to date (apply migrations)
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<DbMakerDbContext>();
        var pending = await context.Database.GetPendingMigrationsAsync();
        if (pending.Any())
        {
            Log.Information("Applying {Count} pending migrations: {Migrations}", pending.Count(), string.Join(", ", pending));
        }
        else
        {
            Log.Information("No pending migrations");
        }

        context.Database.Migrate();
        Log.Information("Database migrated/initialized successfully");

        // Seed templates
        try
        {
            var seeder = scope.ServiceProvider.GetRequiredService<TemplateSeedService>();
            await seeder.SeedAsync();
            Log.Information("Template library seeded");
        }
        catch (Exception seedEx)
        {
            Log.Error(seedEx, "Template seeding failed");
        }
    }

    Log.Information("DbMaker API started successfully on {Environment}", app.Environment.EnvironmentName);
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Helper method for decoding Base64 URL encoded strings
static string DecodeBase64Url(string base64Url)
{
    var base64 = base64Url.Replace('-', '+').Replace('_', '/');
    switch (base64.Length % 4)
    {
        case 2: base64 += "=="; break;
        case 3: base64 += "="; break;
    }
    return System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(base64));
}
