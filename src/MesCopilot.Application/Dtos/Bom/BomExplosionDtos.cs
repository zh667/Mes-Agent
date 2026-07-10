namespace MesCopilot.Application.Dtos.Bom;

public sealed record BomProductOptionDto(int ProductId, string Code, string Name, string BomVersion);

public sealed record BomExplosionItemDto(
    int MaterialId,
    string MaterialCode,
    string MaterialName,
    string Unit,
    decimal RequiredQuantity,
    decimal AvailableQuantity,
    decimal ShortageQuantity,
    int Depth,
    IReadOnlyList<string> Path);

public sealed record BomMaterialSummaryDto(
    int MaterialId,
    string MaterialCode,
    string MaterialName,
    string Unit,
    decimal RequiredQuantity,
    decimal AvailableQuantity,
    decimal ShortageQuantity);

public sealed record BomExplosionResultDto(
    int ProductId,
    string ProductCode,
    string ProductName,
    decimal Quantity,
    IReadOnlyList<BomExplosionItemDto> Items,
    IReadOnlyList<BomMaterialSummaryDto> TotalMaterials);
