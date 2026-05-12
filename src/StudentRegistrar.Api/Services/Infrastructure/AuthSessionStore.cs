using Microsoft.Extensions.Caching.Memory;

namespace StudentRegistrar.Api.Services.Infrastructure;

public interface IAuthSessionStore
{
    Task StoreAsync(AuthSession session, CancellationToken cancellationToken = default);
    Task<AuthSession?> GetAsync(string sessionId, CancellationToken cancellationToken = default);
    Task RemoveAsync(string sessionId, CancellationToken cancellationToken = default);
}

public sealed class AuthSession
{
    public string SessionId { get; init; } = string.Empty;
    public string CsrfToken { get; init; } = string.Empty;
    public Guid TenantId { get; init; }
    public string TenantRealm { get; init; } = string.Empty;
    public string KeycloakUserId { get; init; } = string.Empty;
    public string AccessToken { get; init; } = string.Empty;
    public string? RefreshToken { get; init; }
    public DateTimeOffset AccessTokenExpiresAt { get; init; }
    public DateTimeOffset? RefreshTokenExpiresAt { get; init; }
}

public sealed class MemoryAuthSessionStore : IAuthSessionStore
{
    private readonly IMemoryCache _memoryCache;

    public MemoryAuthSessionStore(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
    }

    public Task StoreAsync(AuthSession session, CancellationToken cancellationToken = default)
    {
        var expiry = session.RefreshTokenExpiresAt ?? session.AccessTokenExpiresAt;
        _memoryCache.Set(GetCacheKey(session.SessionId), session, expiry);
        return Task.CompletedTask;
    }

    public Task<AuthSession?> GetAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        _memoryCache.TryGetValue(GetCacheKey(sessionId), out AuthSession? session);
        return Task.FromResult(session);
    }

    public Task RemoveAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        _memoryCache.Remove(GetCacheKey(sessionId));
        return Task.CompletedTask;
    }

    private static string GetCacheKey(string sessionId)
    {
        return $"auth:session:{sessionId}";
    }
}
