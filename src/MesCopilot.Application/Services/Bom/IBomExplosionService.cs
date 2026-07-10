using MesCopilot.Application.Dtos.Bom;

namespace MesCopilot.Application.Services.Bom;

public interface IBomExplosionService
{
    Task<IReadOnlyList<BomProductOptionDto>> GetAvailableProductsAsync(
        CancellationToken cancellationToken = default);

    Task<BomExplosionResultDto> ExplodeAsync(
        int productId,
        decimal quantity,
        CancellationToken cancellationToken = default);
}

public sealed class BomNotFoundException : Exception
{
    public BomNotFoundException(string message) : base(message) { }
}

public sealed class BomCycleException : Exception
{
    public BomCycleException(string message) : base(message) { }
}

public sealed class BomDepthExceededException : Exception
{
    public BomDepthExceededException(string message) : base(message) { }
}
