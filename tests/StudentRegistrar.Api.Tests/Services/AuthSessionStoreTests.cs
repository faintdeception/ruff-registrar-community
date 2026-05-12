using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using StudentRegistrar.Api.Services.Infrastructure;
using Xunit;

namespace StudentRegistrar.Api.Tests.Services;

public class AuthSessionStoreTests
{
    [Fact]
    public async Task StoreAsync_ThenGetAsync_ReturnsStoredSession()
    {
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var store = new MemoryAuthSessionStore(memoryCache);
        var session = new AuthSession
        {
            SessionId = "session-1",
            TenantId = Guid.NewGuid(),
            TenantRealm = "test-org",
            KeycloakUserId = "kc-user-1",
            AccessToken = "access-token",
            RefreshToken = "refresh-token",
            AccessTokenExpiresAt = DateTimeOffset.UtcNow.AddMinutes(5),
            RefreshTokenExpiresAt = DateTimeOffset.UtcNow.AddMinutes(30)
        };

        await store.StoreAsync(session);

        var result = await store.GetAsync(session.SessionId);

        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(session);
    }

    [Fact]
    public async Task RemoveAsync_RemovesStoredSession()
    {
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var store = new MemoryAuthSessionStore(memoryCache);
        var session = new AuthSession
        {
            SessionId = "session-2",
            TenantId = Guid.NewGuid(),
            TenantRealm = "test-org",
            KeycloakUserId = "kc-user-2",
            AccessToken = "access-token",
            AccessTokenExpiresAt = DateTimeOffset.UtcNow.AddMinutes(5)
        };

        await store.StoreAsync(session);
        await store.RemoveAsync(session.SessionId);

        var result = await store.GetAsync(session.SessionId);

        result.Should().BeNull();
    }
}
