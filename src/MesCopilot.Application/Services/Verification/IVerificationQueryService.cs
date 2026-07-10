namespace MesCopilot.Application.Services.Verification;

public interface IVerificationQueryService
{
    Task<int> GetDelayedOrderCountAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken);

    Task<VerifiedOeeSnapshot?> GetOeeAsync(int equipmentId, DateTime date, CancellationToken cancellationToken);

    Task<VerifiedBatchTrace?> GetBatchTraceAsync(string batchNumber, CancellationToken cancellationToken);

    Task<IReadOnlySet<int>> GetExistingDocumentIdsAsync(
        IReadOnlyCollection<int> documentIds,
        CancellationToken cancellationToken);
}

public sealed record VerifiedOeeSnapshot(int EquipmentId, DateTime Date, decimal Oee);

public sealed record VerifiedBatchTrace(string BatchNumber, int ProductionReportCount, int InspectionCount);
