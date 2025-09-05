namespace StudentRegistrar.Api.Services;

public interface IPasswordService
{
    string GenerateSecurePassword(int length = 14);
    bool ValidatePasswordComplexity(string password);
    PasswordStrength AssessPasswordStrength(string password);
}

public enum PasswordStrength
{
    VeryWeak,
    Weak,
    Fair,
    Good,
    Strong,
    VeryStrong
}
