using System.ComponentModel.DataAnnotations;

namespace MesCopilot.Api.Dtos.Bom;

public sealed class BomExplosionRequest
{
    [Required, Range(typeof(decimal), "0.0001", "1000000")]
    public decimal Quantity { get; init; }

    [Required, Range(1, int.MaxValue)]
    public int ProductId { get; init; }
}

public sealed record BomErrorResponse(string Code, string? Message = null);
