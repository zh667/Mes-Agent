using MesCopilot.Application.Services;
using MesCopilot.Domain.Entities.Equipment;
using MesCopilot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using EquipmentEntity = MesCopilot.Domain.Entities.Equipment.Equipment;

namespace MesCopilot.UnitTests.Application.Services;

public sealed class MaintenancePredictionServiceTests : IDisposable
{
    private readonly MesDbContext _context;

    public MaintenancePredictionServiceTests()
    {
        _context = Infrastructure.Tenancy.TenantTestDbContextFactory.Create();
    }

    [Fact]
    public async Task PredictMaintenanceAsync_WithNoDowntime_ReturnsHealthyPrediction()
    {
        _context.Equipment.Add(new EquipmentEntity
        {
            Id = 1,
            Code = "EQ-001",
            Name = "CNC 1",
            RatedCapacity = 100,
            IdealCycleTime = 1m,
            IsActive = true
        });
        await _context.SaveChangesAsync();
        MaintenancePredictionService service = new(_context);

        var result = await service.PredictMaintenanceAsync(1);

        Assert.True(result.HealthScore >= 80);
        Assert.Equal("Healthy", result.HealthLevel);
        Assert.Equal(720, result.MtbfHours);
    }

    [Fact]
    public async Task PredictMaintenanceAsync_WithFrequentDowntime_LowersHealthScoreAndPredictsFailure()
    {
        DateTime now = DateTime.UtcNow;
        _context.Equipment.Add(new EquipmentEntity
        {
            Id = 1,
            Code = "EQ-001",
            Name = "CNC 1",
            RatedCapacity = 100,
            IdealCycleTime = 1m,
            IsActive = true
        });

        for (int index = 0; index < 6; index++)
        {
            _context.DowntimeRecords.Add(new DowntimeRecord
            {
                EquipmentId = 1,
                Reason = "Spindle fault",
                DowntimeType = "Failure",
                StartTime = now.AddHours(-index * 12),
                EndTime = now.AddHours(-index * 12).AddHours(2),
                DurationMinutes = 120
            });
        }

        await _context.SaveChangesAsync();
        MaintenancePredictionService service = new(_context);

        var result = await service.PredictMaintenanceAsync(1);

        Assert.True(result.HealthScore < 60);
        Assert.True(result.MtbfHours <= 12.1);
        Assert.NotNull(result.PredictedNextFailureAt);
        Assert.NotEmpty(result.RecentFailures);
    }

    [Fact]
    public async Task PredictMaintenanceAsync_WhenEquipmentMissing_ThrowsInvalidOperationException()
    {
        MaintenancePredictionService service = new(_context);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.PredictMaintenanceAsync(404));
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
