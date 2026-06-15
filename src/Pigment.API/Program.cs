using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Microsoft.OpenApi.Models;
using Pigment.API.Middleware;
using Pigment.Core.Interfaces;
using Pigment.Core.Services;
using Pigment.Infrastructure.Data;
using Pigment.Infrastructure.Repositories;
using Pigment.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// 1.  CONFIGURATION
// ============================================================
var configuration   = builder.Configuration;
var connectionString = configuration.GetConnectionString("MiddlewareDb")
    ?? throw new InvalidOperationException("Connection string 'MiddlewareDb' not found.");

// ============================================================
// 2.  AUTHENTICATION  — Azure Entra ID (Bearer JWT)
// ============================================================
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(configuration.GetSection("AzureAd"));

// ============================================================
// 3.  AUTHORISATION  — Entra ID group-based policy
// ============================================================
var allowedGroups = configuration
    .GetSection("Authorization:AllowedGroups")
    .Get<string[]>() ?? Array.Empty<string>();

builder.Services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationHandler,
    Pigment.API.Authorization.GroupAuthorizationHandler>();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("PigmentUser", policy =>
        policy.RequireAuthenticatedUser()
              .AddRequirements(new Pigment.API.Authorization.GroupRequirement(allowedGroups)));
});

// ============================================================
// 4.  EF CORE  — PigmentDbContext (run-history audit table)
// ============================================================
builder.Services.AddDbContext<PigmentDbContext>(options =>
    options.UseSqlServer(connectionString));

// ============================================================
// 5.  INFRASTRUCTURE SERVICES
// ============================================================

// Dapper-based HR data repository
builder.Services.AddScoped<IHrDataRepository>(sp =>
    new HrDataRepository(
        connectionString,
        sp.GetRequiredService<ILogger<HrDataRepository>>()));

// Azure Blob Storage
builder.Services.AddScoped<IBlobStorageService>(sp =>
{
    var storageConnStr = configuration["AzureStorage:ConnectionString"]
        ?? throw new InvalidOperationException("AzureStorage:ConnectionString not configured.");

    var containerName = configuration["AzureStorage:ContainerName"] ?? "pigment-hr-files";
    var blobPrefix    = configuration["AzureStorage:BlobPrefix"]    ?? "pigment-hr";

    return new BlobStorageService(
        new BlobServiceClient(storageConnStr),
        containerName,
        blobPrefix,
        configuration,
        sp.GetRequiredService<ILogger<BlobStorageService>>());
});

// ============================================================
// 6.  CORE / APPLICATION SERVICES
// ============================================================
builder.Services.AddScoped<IHrDataService, HrDataService>();

// ============================================================
// 7.  CONTROLLERS
// ============================================================
builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        // camelCase JSON, nulls omitted
        opts.JsonSerializerOptions.DefaultIgnoreCondition =
            System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
        opts.JsonSerializerOptions.PropertyNamingPolicy =
            System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// ============================================================
// 8.  SWAGGER / OPENAPI
// ============================================================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title   = "Pigment HR Integration API",
        Version = "v1",
        Description = "Exposes ResourceLink HR payroll data to the Pigment planning system. " +
                      "All endpoints require a valid Azure Entra ID Bearer token."
    });

    // Add Bearer token support in Swagger UI
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name         = "Authorization",
        Type         = SecuritySchemeType.Http,
        Scheme       = "bearer",
        BearerFormat = "JWT",
        In           = ParameterLocation.Header,
        Description  = "Enter your Entra ID Bearer token."
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id   = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ============================================================
// 9.  APPLICATION INSIGHTS
// ============================================================
var appInsightsConnStr = configuration["ApplicationInsights:ConnectionString"];
if (!string.IsNullOrWhiteSpace(appInsightsConnStr))
{
    builder.Services.AddApplicationInsightsTelemetry(opts =>
        opts.ConnectionString = appInsightsConnStr);
}

// ============================================================
// 10. CORS  (tighten per-environment as needed)
// ============================================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("PigmentCors", policy =>
        policy.WithOrigins(configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                           ?? Array.Empty<string>())
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// ============================================================
// BUILD
// ============================================================
var app = builder.Build();

// ============================================================
// 11. AUTO-MIGRATE DATABASE on startup (creates PigmentRuns table)
// ============================================================
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PigmentDbContext>();
    db.Database.Migrate();
}

// ============================================================
// 12. MIDDLEWARE PIPELINE
// ============================================================

// Global exception handler — always first
app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Pigment HR API v1");
        c.RoutePrefix = string.Empty;   // Swagger at root: https://localhost:PORT/
    });
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseCors("PigmentCors");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
