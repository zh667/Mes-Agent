using System.Text;
using MesCopilot.Application;
using MesCopilot.Api.Hubs;
using MesCopilot.Api.Middleware;
using MesCopilot.Agent;
using MesCopilot.Agent.Caching;
using MesCopilot.Agent.Plugins.KnowledgeAgentPlugin;
using MesCopilot.Agent.Plugins.OeeAgentPlugin;
using MesCopilot.Agent.Plugins.ProductionAgentPlugin;
using MesCopilot.Agent.Plugins.QualityAgentPlugin;
using MesCopilot.Infrastructure;
using MesCopilot.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();
builder.Services.AddMesCopilotApplication();
builder.Services.AddMesCopilotInfrastructure(builder.Configuration);
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
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdmin", policy => policy.RequireRole("Admin"));
    options.AddPolicy("RequireTeamLead", policy => policy.RequireRole("Admin", "TeamLead"));
    options.AddPolicy("RequireQAInspector", policy => policy.RequireRole("Admin", "TeamLead", "QAInspector"));
    options.AddPolicy("RequireOperator", policy => policy.RequireRole("Admin", "TeamLead", "QAInspector", "Operator"));
});
builder.Services.AddSingleton<IAgentResponseCache, InMemoryAgentResponseCache>();
builder.Services.AddScoped<ProductionAgentPlugin>();
builder.Services.AddScoped<QualityAgentPlugin>();
builder.Services.AddScoped<OeeAgentPlugin>();
builder.Services.AddScoped<KnowledgeAgentPlugin>();

var app = builder.Build();

var connectionString = app.Configuration.GetConnectionString("MesDatabase");
if (app.Environment.IsDevelopment() &&
    !string.IsNullOrWhiteSpace(connectionString) &&
    !connectionString.Contains("CHANGE_ME", StringComparison.OrdinalIgnoreCase))
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<MesDbContext>();
    await dbContext.Database.MigrateAsync();
    await SeedData.SeedAsync(dbContext);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseMiddleware<ResponseTimeMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));
app.MapControllers();
app.MapHub<EquipmentHub>("/hubs/equipment");

app.Run();

public partial class Program
{
}
