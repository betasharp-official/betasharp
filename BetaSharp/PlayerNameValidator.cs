namespace BetaSharp;

/// <summary>
/// Validates player display names for CLI session and multiplayer login (16-char limit matches login / spawn packets).
/// </summary>
public static class PlayerNameValidator
{
    public const int MaxLength = 16;

    /// <summary>
    /// Ensures <paramref name="name"/> is non-empty, has no whitespace, length at most <see cref="MaxLength"/>, and no leading/trailing space.
    /// </summary>
    /// <exception cref="InvalidPlayerNameException">When the name is not allowed.</exception>
    public static void Validate(string? name)
    {
        if (name is null) throw new InvalidPlayerNameException("Player name is required.");
        string trimmed = name.Trim();
        if (trimmed.Length == 0) throw new InvalidPlayerNameException("Player name cannot be empty.");
        if (trimmed.Length != name.Length) throw new InvalidPlayerNameException("Player name cannot have leading or trailing whitespace.");
        if (trimmed.Length > MaxLength) throw new InvalidPlayerNameException($"Player name cannot be longer than {MaxLength} characters.");
        if (trimmed.Any(char.IsWhiteSpace)) throw new InvalidPlayerNameException("Player name cannot contain whitespace.");
    }
}
