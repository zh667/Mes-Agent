using System.Text;
using System.Security.Claims;
using MesCopilot.Application;
using MesCopilot.Api.Hubs;
using MesCopilot.Api.Middleware;
using MesCopilot.Api.OpenApi;
using MesCopilot.Agent;
using MesCopilot.Agent.Caching;
using MesCopilot.Agent.Plugins.KnowledgeAgentPlugin;
using MesCopilot.Agent.Plugins.OeeAgentPlugin;
using MesCopilot.Agent.Plugins.ProductionAgentPlugin;
using MesCopilot.Agent.Plugins.QualityAgentPlugin;
using MesCopilot.Infrastructure;
using MesCopilot.Infrastructure.Data;
using MesCopilot.Infrastructure.Tenancy;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MesCopilot.Infrastructure.Identity;
using MesCopilot.Infrastructure.Caching;
using MesCopilot.Api.HostedServices;
using MesCopilot.Api.Errors;
using Microsoft.AspNetCore.Localization;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    CultureInfo[] cultures = [new("zh-CN"), new("en-US")];
    options.DefaultRequestCulture = new RequestCulture("zh-CN");
    options.SupportedCultures = cultures;
    options.SupportedUICultures = cultures;
    options.RequestCultureProviders = [new AcceptLanguageHeaderRequestCultureProvider()];
});
builder.Services.AddScoped<ApiProblemFactory>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SupportNonNullableReferenceTypes();
    options.SchemaFilter<RequiredPropertiesSchemaFilter>();
    options.OperationFilter<TenantHeaderOperationFilter>();
});
builder.Services.AddSignalR();
builder.Services.AddMesCopilotApplication();
builder.Services.AddMesCopilotInfrastructure(builder.Configuration, builder.Environment.EnvironmentName);
builder.Services.AddMesCopilotAgent();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]!)),
            ClockSkew = TimeSpan.Zero
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                string? accessToken = context.Request.Query["access_token"].SingleOrDefault();
                if (!string.IsNullOrWhiteSpace(accessToken) &&
                    context.HttpContext.Request.Path.StartsWithSegments("/hubs/equipment"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            },
            OnTokenValidated = async context =>
            {
                string? tokenId = context.Principal?.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti) ??
                    context.Principal?.FindFirstValue("jti");
                if (string.IsNullOrWhiteSpace(tokenId))
                {
                    context.Fail("Access token does not contain a token identifier.");
                    return;
                }

                IAccessTokenRevocationStore revocationStore =
                    context.HttpContext.RequestServices.GetRequiredService<IAccessTokenRevocationStore>();
                if (await revocationStore.IsRevokedAsync(tokenId, context.HttpContext.RequestAborted))
                {
                    context.Fail("Access token has been revoked.");
                }
            }
        };
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequirePlatformAdmin", policy =>
        policy.RequireClaim(MesCopilot.Domain.Entities.Identity.MesCopilotClaimTypes.PlatformAdmin, bool.TrueString.ToLowerInvariant()));
    options.AddPolicy("RequireAdmin", policy => policy.RequireRole("Admin"));
    options.AddPolicy("RequireTeamLead", policy => policy.RequireRole("Admin", "TeamLead"));
    options.AddPolicy("RequireQAInspector", policy => policy.RequireRole("Admin", "TeamLead", "QAInspector"));
    options.AddPolicy("RequireOperator", policy => policy.RequireRole("Admin", "TeamLead", "QAInspector", "Operator"));
});
if (string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("Redis")))
{
    builder.Services.AddSingleton<IAgentResponseCache, InMemoryAgentResponseCache>();
}
else
{
    builder.Services.AddSingleton<IAgentResponseCache, RedisAgentResponseCache>();
}
builder.Services.AddScoped<ProductionAgentPlugin>();
builder.Services.AddScoped<QualityAgentPlugin>();
builder.Services.AddScoped<OeeAgentPlugin>();
builder.Services.AddScoped<KnowledgeAgentPlugin>();
builder.Services.AddSingleton<DeviceCollectionCoordinator>();
if (builder.Configuration.GetValue("Devices:CollectionEnabled", true))
{
    builder.Services.AddHostedService<DeviceCollectorWorker>();
}

var app = builder.Build();

var connectionString = app.Configuration.GetConnectionString("MesDatabase");
if (app.Environment.IsDevelopment() &&
    !string.IsNullOrWhiteSpace(connectionString) &&
    !connectionString.Contains("CHANGE_ME", StringComparison.OrdinalIgnoreCase))
{
    using var scope = app.Services.CreateScope();
    CurrentTenantContext tenantContext = scope.ServiceProvider.GetRequiredService<CurrentTenantContext>();
    tenantContext.Initialize(new TenantResolution(SeedData.DefaultTenantId, IsPlatformAdmin: true));
    var dbContext = scope.ServiceProvider.GetRequiredService<MesDbContext>();
    await dbContext.Database.MigrateAsync();
    await SeedData.SeedAsync(dbContext);
    UserManager<MesCopilot.Domain.Entities.Identity.AppUser> userManager =
        scope.ServiceProvider.GetRequiredService<UserManager<MesCopilot.Domain.Entities.Identity.AppUser>>();
    await SeedData.SeedBootstrapAdminAsync(dbContext, userManager, app.Configuration);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseRequestLocalization();
app.UseMiddleware<ResponseTimeMiddleware>();
app.UseAuthentication();
app.UseMiddleware<AuditMiddleware>();
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseAuthorization();
app.MapGet("/health", async (MesDbContext database, ICacheHealthProbe cache, CancellationToken cancellationToken) =>
{
    try
    {
        if (!await database.Database.CanConnectAsync(cancellationToken))
        {
            return Results.Json(
                new { status = "Unhealthy", dependency = "database" },
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }
    catch (Exception) when (!cancellationToken.IsCancellationRequested)
    {
        return Results.Json(
            new { status = "Unhealthy", dependency = "database" },
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    return await cache.IsHealthyAsync(cancellationToken)
        ? Results.Ok(new { status = "Healthy" })
        : Results.Json(
            new { status = "Unhealthy", dependency = "cache" },
            statusCode: StatusCodes.Status503ServiceUnavailable);
});
app.MapControllers();
app.MapHub<EquipmentHub>("/hubs/equipment");

app.Run();

public partial class Program
{
}
