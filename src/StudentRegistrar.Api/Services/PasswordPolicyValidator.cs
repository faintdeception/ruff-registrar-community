using System.Text.RegularExpressions;

namespace StudentRegistrar.Api.Services;

public class PasswordPolicyValidationResult
{
    public bool IsValid { get; init; }
    public IReadOnlyList<string> FailureReasons { get; init; } = Array.Empty<string>();

    /// <summary>
    /// True when Keycloak's actual policy could not be retrieved and a conservative baseline
    /// policy was used instead. Callers should surface this so hosts know the check is a
    /// safety net, not a guarantee of matching their real realm policy.
    /// </summary>
    public bool UsedFallbackBaseline { get; init; }

    public static PasswordPolicyValidationResult Valid(bool usedFallbackBaseline) => new()
    {
        IsValid = true,
        UsedFallbackBaseline = usedFallbackBaseline
    };

    public static PasswordPolicyValidationResult Invalid(IReadOnlyList<string> reasons, bool usedFallbackBaseline) => new()
    {
        IsValid = false,
        FailureReasons = reasons,
        UsedFallbackBaseline = usedFallbackBaseline
    };
}

public interface IPasswordPolicyValidator
{
    /// <summary>
    /// Validates a candidate password against the current tenant's realm password policy.
    /// If the policy can't be fetched from Keycloak, falls back to a conservative baseline
    /// (length >= 12, upper/lower/digit/special) rather than skipping validation.
    /// </summary>
    Task<PasswordPolicyValidationResult> ValidateAsync(string? password);
}

public class PasswordPolicyValidator : IPasswordPolicyValidator
{
    // Conservative baseline used only when the real Keycloak policy is unavailable.
    private const int BaselineMinLength = 12;

    private readonly IKeycloakService _keycloakService;
    private readonly ILogger<PasswordPolicyValidator> _logger;

    private static readonly Regex TokenRegex = new(@"^(?<name>[A-Za-z]+)(\((?<arg>[^)]*)\))?$", RegexOptions.Compiled);

    public PasswordPolicyValidator(IKeycloakService keycloakService, ILogger<PasswordPolicyValidator> logger)
    {
        _keycloakService = keycloakService;
        _logger = logger;
    }

    public async Task<PasswordPolicyValidationResult> ValidateAsync(string? password)
    {
        string? policy = null;
        try
        {
            policy = await _keycloakService.GetRealmPasswordPolicyAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not retrieve Keycloak password policy; falling back to baseline validation");
        }

        return string.IsNullOrWhiteSpace(policy)
            ? ValidateAgainstBaseline(password)
            : ValidateAgainstKeycloakPolicy(password, policy);
    }

    private static PasswordPolicyValidationResult ValidateAgainstBaseline(string? password)
    {
        var reasons = new List<string>();

        if (string.IsNullOrEmpty(password))
        {
            reasons.Add("password must not be blank");
        }
        else
        {
            if (password.Length < BaselineMinLength)
            {
                reasons.Add($"must be at least {BaselineMinLength} characters");
            }
            if (!password.Any(char.IsUpper))
            {
                reasons.Add("must contain an uppercase letter");
            }
            if (!password.Any(char.IsLower))
            {
                reasons.Add("must contain a lowercase letter");
            }
            if (!password.Any(char.IsDigit))
            {
                reasons.Add("must contain a digit");
            }
            if (password.All(char.IsLetterOrDigit))
            {
                reasons.Add("must contain a special character");
            }
        }

        return reasons.Count == 0
            ? PasswordPolicyValidationResult.Valid(usedFallbackBaseline: true)
            : PasswordPolicyValidationResult.Invalid(reasons, usedFallbackBaseline: true);
    }

    private static PasswordPolicyValidationResult ValidateAgainstKeycloakPolicy(string? password, string policy)
    {
        var reasons = new List<string>();
        password ??= string.Empty;

        if (password.Length == 0)
        {
            reasons.Add("password must not be blank");
            return PasswordPolicyValidationResult.Invalid(reasons, usedFallbackBaseline: false);
        }

        var tokens = policy.Split(" and ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var token in tokens)
        {
            var match = TokenRegex.Match(token);
            if (!match.Success)
            {
                continue; // Unrecognized token format; skip rather than guess.
            }

            var name = match.Groups["name"].Value;
            var arg = match.Groups["arg"].Success ? match.Groups["arg"].Value : null;

            switch (name)
            {
                case "length" when int.TryParse(arg, out var minLength) && password.Length < minLength:
                    reasons.Add($"must be at least {minLength} characters");
                    break;
                case "upperCase" when int.TryParse(arg, out var minUpper) && password.Count(char.IsUpper) < minUpper:
                    reasons.Add($"must contain at least {minUpper} uppercase character(s)");
                    break;
                case "lowerCase" when int.TryParse(arg, out var minLower) && password.Count(char.IsLower) < minLower:
                    reasons.Add($"must contain at least {minLower} lowercase character(s)");
                    break;
                case "digits" when int.TryParse(arg, out var minDigits) && password.Count(char.IsDigit) < minDigits:
                    reasons.Add($"must contain at least {minDigits} digit(s)");
                    break;
                case "specialChars" when int.TryParse(arg, out var minSpecial) && password.Count(c => !char.IsLetterOrDigit(c)) < minSpecial:
                    reasons.Add($"must contain at least {minSpecial} special character(s)");
                    break;
                // notUsername/notEmail/passwordHistory/forceExpiredPasswordChange/hashAlgorithm/regexPattern
                // require account or hashing context we don't have here; intentionally not checked.
            }
        }

        return reasons.Count == 0
            ? PasswordPolicyValidationResult.Valid(usedFallbackBaseline: false)
            : PasswordPolicyValidationResult.Invalid(reasons, usedFallbackBaseline: false);
    }
}
