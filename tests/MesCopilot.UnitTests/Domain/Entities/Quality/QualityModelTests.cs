using MesCopilot.Domain.Entities.Quality;
using MesCopilot.Domain.Enums;

namespace MesCopilot.UnitTests.Domain.Entities.Quality;

public class QualityModelTests
{
    [Fact]
    public void QualityInspection_ShouldKeepTraceabilityFieldsAndInitializeDefects()
    {
        var inspectionTime = DateTime.UtcNow;
        var inspection = new QualityInspection
        {
            Code = "QI-001",
            BatchNumber = "B20260708",
            WorkOrderId = 1,
            ProcessStepId = 2,
            InspectorId = "qc-1",
            InspectorName = "李四",
            InspectedQuantity = 100,
            PassedQuantity = 98,
            FailedQuantity = 2,
            Status = InspectionStatus.Fail,
            InspectionTime = inspectionTime
        };

        Assert.Equal("QI-001", inspection.Code);
        Assert.Equal("B20260708", inspection.BatchNumber);
        Assert.Equal(1, inspection.WorkOrderId);
        Assert.Equal(2, inspection.ProcessStepId);
        Assert.Equal("qc-1", inspection.InspectorId);
        Assert.Equal("李四", inspection.InspectorName);
        Assert.Equal(100, inspection.InspectedQuantity);
        Assert.Equal(98, inspection.PassedQuantity);
        Assert.Equal(2, inspection.FailedQuantity);
        Assert.Equal(InspectionStatus.Fail, inspection.Status);
        Assert.Equal(inspectionTime, inspection.InspectionTime);
        Assert.NotNull(inspection.DefectRecords);
        Assert.Empty(inspection.DefectRecords);
    }

    [Fact]
    public void DefectType_ShouldDefaultToActiveAndInitializeRecords()
    {
        var defectType = new DefectType();

        Assert.True(defectType.IsActive);
        Assert.NotNull(defectType.DefectRecords);
        Assert.Empty(defectType.DefectRecords);
    }
}
