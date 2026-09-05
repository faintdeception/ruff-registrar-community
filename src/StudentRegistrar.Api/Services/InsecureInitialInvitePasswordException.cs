namespace StudentRegistrar.Api.Services;

/// <summary>
/// Thrown when a tenant's configured initial invite password (used for bulk/CSV member import)
/// is blank or fails password-policy validation. Must never be swallowed \u2014 hosts need a clear,
/// actionable signal that `InitialInvitePassword` has to be set to something non-blank and secure,
/// even when the realm's actual policy could not be retrieved from Keycloak.
/// </summary>
public class InsecureInitialInvitePasswordException : InvalidOperationException
{
    public IReadOnlyList<string> FailureReasons { get; }

    public InsecureInitialInvitePasswordException(IReadOnlyList<string> failureReasons)
        : base(BuildMessage(failureReasons))
    {
        FailureReasons = failureReasons;
    }

    private static string BuildMessage(IReadOnlyList<string> failureReasons) =>
        "The 'initial_invite_password' setting is missing or insecure and must be fixed before " +
        "members can be invited: " + string.Join("; ", failureReasons);
}
