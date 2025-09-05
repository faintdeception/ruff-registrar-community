namespace StudentRegistrar.Api.Services;

public class PasswordService : IPasswordService
{
    private readonly ILogger<PasswordService> _logger;
    
    // Character sets for password generation (excluding ambiguous characters)
    private const string UppercaseChars = "ABCDEFGHJKLMNPQRSTUVWXYZ"; // Exclude I, O
    private const string LowercaseChars = "abcdefghjkmnpqrstuvwxyz"; // Exclude i, l, o
    private const string DigitChars = "23456789"; // Exclude 0, 1
    private const string SpecialChars = "!@#$%&*+=?";
    private const string AllChars = UppercaseChars + LowercaseChars + DigitChars + SpecialChars;

    public PasswordService(ILogger<PasswordService> logger)
    {
        _logger = logger;
    }

    public string GenerateSecurePassword(int length = 14)
    {
        if (length < 8)
        {
            throw new ArgumentException("Password length must be at least 8 characters", nameof(length));
        }

        try
        {
            using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            var password = new char[length];
            var categoryUsed = new bool[4]; // Track which character categories we've used

            // Ensure at least one character from each category
            password[0] = GetRandomChar(UppercaseChars, rng);
            categoryUsed[0] = true;
            password[1] = GetRandomChar(LowercaseChars, rng);
            categoryUsed[1] = true;
            password[2] = GetRandomChar(DigitChars, rng);
            categoryUsed[2] = true;
            password[3] = GetRandomChar(SpecialChars, rng);
            categoryUsed[3] = true;

            // Fill the rest with random characters from all categories
            for (int i = 4; i < length; i++)
            {
                password[i] = GetRandomChar(AllChars, rng);
            }

            // Shuffle the password to avoid predictable patterns
            ShuffleArray(password, rng);

            var result = new string(password);
            
            // Validate the generated password meets our complexity requirements
            if (!ValidatePasswordComplexity(result))
            {
                _logger.LogWarning("Generated password failed complexity validation, regenerating...");
                return GenerateSecurePassword(length); // Recursive retry
            }

            _logger.LogDebug("Generated secure password of length {Length}", length);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating secure password");
            throw;
        }
    }

    public bool ValidatePasswordComplexity(string password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < 8)
            return false;

        var hasUpper = password.Any(c => UppercaseChars.Contains(c));
        var hasLower = password.Any(c => LowercaseChars.Contains(c));
        var hasDigit = password.Any(c => DigitChars.Contains(c));
        var hasSpecial = password.Any(c => SpecialChars.Contains(c));

        return hasUpper && hasLower && hasDigit && hasSpecial;
    }

    public PasswordStrength AssessPasswordStrength(string password)
    {
        if (string.IsNullOrEmpty(password))
            return PasswordStrength.VeryWeak;

        int score = 0;

        // Length scoring
        if (password.Length >= 8) score += 1;
        if (password.Length >= 12) score += 1;
        if (password.Length >= 16) score += 1;

        // Character diversity scoring
        if (password.Any(c => UppercaseChars.Contains(c))) score += 1;
        if (password.Any(c => LowercaseChars.Contains(c))) score += 1;
        if (password.Any(c => DigitChars.Contains(c))) score += 1;
        if (password.Any(c => SpecialChars.Contains(c))) score += 1;

        // Pattern analysis (basic)
        if (!HasRepeatingPatterns(password)) score += 1;
        if (!HasSequentialPatterns(password)) score += 1;

        return score switch
        {
            <= 2 => PasswordStrength.VeryWeak,
            3 => PasswordStrength.Weak,
            4 => PasswordStrength.Fair,
            5 => PasswordStrength.Good,
            6 => PasswordStrength.Strong,
            >= 7 => PasswordStrength.VeryStrong,
        };
    }

    private static char GetRandomChar(string chars, System.Security.Cryptography.RandomNumberGenerator rng)
    {
        var randomBytes = new byte[4];
        rng.GetBytes(randomBytes);
        var randomValue = BitConverter.ToUInt32(randomBytes, 0);
        return chars[(int)(randomValue % chars.Length)];
    }

    private static void ShuffleArray(char[] array, System.Security.Cryptography.RandomNumberGenerator rng)
    {
        for (int i = array.Length - 1; i > 0; i--)
        {
            var randomBytes = new byte[4];
            rng.GetBytes(randomBytes);
            var randomValue = BitConverter.ToUInt32(randomBytes, 0);
            int j = (int)(randomValue % (i + 1));
            
            (array[i], array[j]) = (array[j], array[i]);
        }
    }

    private static bool HasRepeatingPatterns(string password)
    {
        // Check for 3+ character repetitions
        for (int i = 0; i < password.Length - 2; i++)
        {
            if (password[i] == password[i + 1] && password[i + 1] == password[i + 2])
            {
                return true;
            }
        }
        return false;
    }

    private static bool HasSequentialPatterns(string password)
    {
        // Check for 3+ character sequences (abc, 123, etc.)
        for (int i = 0; i < password.Length - 2; i++)
        {
            if ((password[i + 1] == password[i] + 1) && (password[i + 2] == password[i + 1] + 1))
            {
                return true;
            }
        }
        return false;
    }
}
