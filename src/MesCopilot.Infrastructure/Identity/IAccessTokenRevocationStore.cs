namespace MesCopilot.Infrastructure.Identity;

public interface IAccessTokenRevocationStore
{
    Task RevokeAsync(string tokenId, DateTimeOffset expiresAt, CancellationToken cancellationToken = default);

    Task<bool> IsRevokedAsync(string tokenId, CancellationToken cancellationToken = default);
}
