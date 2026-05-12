using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AutoMapper;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using StudentRegistrar.Api.DTOs;
using StudentRegistrar.Api.Services;
using StudentRegistrar.Api.Services.Infrastructure;
using StudentRegistrar.Data;

namespace StudentRegistrar.Api.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    public const string SessionIdClaimType = "studentregistrar:session_id";
    public const string CsrfCookieName = "studentregistrar.csrf";
    public const string CsrfHeaderName = "X-CSRF-TOKEN";

    private readonly StudentRegistrarDbContext _dbContext;
    private readonly ITenantContextAccessor _tenantContextAccessor;
    private readonly IKeycloakService _keycloakService;
    private readonly IAuthSessionStore _authSessionStore;
    private readonly IMapper _mapper;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        StudentRegistrarDbContext dbContext,
        ITenantContextAccessor tenantContextAccessor,
        IKeycloakService keycloakService,
        IAuthSessionStore authSessionStore,
        IMapper mapper,
        ILogger<AuthController> logger)
    {
        _dbContext = dbContext;
        _tenantContextAccessor = tenantContextAccessor;
        _keycloakService = keycloakService;
        _authSessionStore = authSessionStore;
        _mapper = mapper;
        _logger = logger;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<SessionLoginResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new SessionLoginResponse
            {
                Success = false,
                ErrorMessage = "Email and password are required."
            });
        }

        var tenantContext = _tenantContextAccessor.TenantContext;
        if (tenantContext?.Tenant == null)
        {
            return BadRequest(new SessionLoginResponse
            {
                Success = false,
                ErrorMessage = "Tenant context is required for sign-in."
            });
        }

        try
        {
            var tokenResponse = await _keycloakService.AuthenticateUserAsync(request.Email, request.Password, cancellationToken);
            var principal = BuildPrincipal(tokenResponse, tenantContext.Tenant.Id);
            var keycloakUserId = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");

            if (string.IsNullOrWhiteSpace(keycloakUserId))
            {
                return Unauthorized(new SessionLoginResponse
                {
                    Success = false,
                    ErrorMessage = "Authenticated user did not include an identifier."
                });
            }

            var user = await _dbContext.Users
                .Include(u => u.UserProfile)
                .FirstOrDefaultAsync(u => u.KeycloakId == keycloakUserId, cancellationToken);

            if (user == null)
            {
                return Unauthorized(new SessionLoginResponse
                {
                    Success = false,
                    ErrorMessage = "User is not registered in this organization."
                });
            }

            var sessionId = Guid.NewGuid().ToString("N");
            var csrfToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray()) + Convert.ToBase64String(Guid.NewGuid().ToByteArray());
            var claimsIdentity = (ClaimsIdentity)principal.Identity!;
            claimsIdentity.AddClaim(new Claim(SessionIdClaimType, sessionId));

            await _authSessionStore.StoreAsync(new AuthSession
            {
                SessionId = sessionId,
                CsrfToken = csrfToken,
                TenantId = tenantContext.Tenant.Id,
                TenantRealm = tenantContext.Tenant.KeycloakRealm,
                KeycloakUserId = keycloakUserId,
                AccessToken = tokenResponse.AccessToken,
                RefreshToken = tokenResponse.RefreshToken,
                AccessTokenExpiresAt = tokenResponse.AccessTokenExpiresAt,
                RefreshTokenExpiresAt = tokenResponse.RefreshTokenExpiresAt
            }, cancellationToken);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties
            {
                IsPersistent = true,
                AllowRefresh = true,
                ExpiresUtc = tokenResponse.RefreshTokenExpiresAt ?? tokenResponse.AccessTokenExpiresAt
            });

            AppendCsrfCookie(csrfToken, tokenResponse.RefreshTokenExpiresAt ?? tokenResponse.AccessTokenExpiresAt);

            return Ok(new SessionLoginResponse
            {
                Success = true,
                User = MapUser(user, principal)
            });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new SessionLoginResponse
            {
                Success = false,
                ErrorMessage = "Invalid email or password."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login failed for {Email}", request.Email);
            return StatusCode(StatusCodes.Status500InternalServerError, new SessionLoginResponse
            {
                Success = false,
                ErrorMessage = "Unable to sign in right now."
            });
        }
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var sessionId = User.FindFirstValue(SessionIdClaimType);
        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            await _authSessionStore.RemoveAsync(sessionId, cancellationToken);
        }

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        Response.Cookies.Delete(CsrfCookieName, BuildCookieOptions(null));
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<AuthUserDto>> Me(CancellationToken cancellationToken)
    {
        var keycloakUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(keycloakUserId))
        {
            return Unauthorized();
        }

        var user = await _dbContext.Users
            .Include(u => u.UserProfile)
            .FirstOrDefaultAsync(u => u.KeycloakId == keycloakUserId, cancellationToken);

        if (user == null)
        {
            return NotFound();
        }

        var sessionId = User.FindFirstValue(SessionIdClaimType);
        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            var session = await _authSessionStore.GetAsync(sessionId, cancellationToken);
            if (session != null && !string.IsNullOrWhiteSpace(session.CsrfToken))
            {
                AppendCsrfCookie(session.CsrfToken, session.RefreshTokenExpiresAt ?? session.AccessTokenExpiresAt);
            }
        }

        return Ok(MapUser(user, User));
    }

    private void AppendCsrfCookie(string csrfToken, DateTimeOffset expiresAt)
    {
        Response.Cookies.Append(CsrfCookieName, csrfToken, BuildCookieOptions(expiresAt));
    }

    private CookieOptions BuildCookieOptions(DateTimeOffset? expiresAt)
    {
        var isSecureRequest = Request.IsHttps;

        return new CookieOptions
        {
            HttpOnly = false,
            Secure = isSecureRequest,
            SameSite = isSecureRequest ? SameSiteMode.None : SameSiteMode.Lax,
            Expires = expiresAt,
            Path = "/"
        };
    }

    private static ClaimsPrincipal BuildPrincipal(KeycloakTokenResponse tokenResponse, Guid tenantId)
    {
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(tokenResponse.AccessToken);
        var claims = new List<Claim>();

        foreach (var claim in token.Claims)
        {
            claims.Add(claim);
            if (claim.Type == "sub")
            {
                claims.Add(new Claim(ClaimTypes.NameIdentifier, claim.Value));
            }
            else if (claim.Type == "email")
            {
                claims.Add(new Claim(ClaimTypes.Email, claim.Value));
            }
            else if (claim.Type == "given_name")
            {
                claims.Add(new Claim(ClaimTypes.GivenName, claim.Value));
            }
            else if (claim.Type == "family_name")
            {
                claims.Add(new Claim(ClaimTypes.Surname, claim.Value));
            }
        }

        foreach (var role in token.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var realmAccessClaim = token.Claims.FirstOrDefault(c => c.Type == "realm_access")?.Value;
        if (!string.IsNullOrWhiteSpace(realmAccessClaim))
        {
            using var realmAccessJson = JsonDocument.Parse(realmAccessClaim);
            if (realmAccessJson.RootElement.TryGetProperty("roles", out var rolesElement) && rolesElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var roleElement in rolesElement.EnumerateArray())
                {
                    if (roleElement.ValueKind != JsonValueKind.String)
                    {
                        continue;
                    }

                    var roleName = roleElement.GetString();
                    if (string.IsNullOrWhiteSpace(roleName))
                    {
                        continue;
                    }

                    claims.Add(new Claim(ClaimTypes.Role, roleName));
                }
            }
        }

        claims.Add(new Claim("studentregistrar:tenant_id", tenantId.ToString()));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme, ClaimTypes.Name, ClaimTypes.Role);
        return new ClaimsPrincipal(identity);
    }

    private AuthUserDto MapUser(Models.User user, ClaimsPrincipal principal)
    {
        return new AuthUserDto
        {
            Id = user.Id,
            Email = user.Email,
            Username = principal.FindFirstValue("preferred_username") ?? user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            KeycloakId = user.KeycloakId,
            Roles = principal.FindAll(ClaimTypes.Role).Select(c => c.Value).Distinct(StringComparer.OrdinalIgnoreCase).ToArray()
        };
    }
}
