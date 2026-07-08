using MesCopilot.Application;
using MesCopilot.Api.Hubs;
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

var app = builder.Build();

var connectionString = app.Configuration.GetConnectionString("MesDatabase");
if (!string.IsNullOrWhiteSpace(connectionString) &&
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
