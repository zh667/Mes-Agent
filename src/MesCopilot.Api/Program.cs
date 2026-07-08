using MesCopilot.Application;
using MesCopilot.Api.Hubs;
using MesCopilot.Agent.Plugins.KnowledgeAgentPlugin;
using MesCopilot.Agent.Plugins.OeeAgentPlugin;
using MesCopilot.Agent.Plugins.ProductionAgentPlugin;
using MesCopilot.Agent.Plugins.QualityAgentPlugin;
using MesCopilot.Infrastructure;
using MesCopilot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();
builder.Services.AddMesCopilotApplication();
builder.Services.AddMesCopilotInfrastructure(builder.Configuration);
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
app.UseAuthorization();
app.MapControllers();
app.MapHub<EquipmentHub>("/hubs/equipment");

app.Run();

public partial class Program
{
}
