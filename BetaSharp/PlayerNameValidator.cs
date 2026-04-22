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
        if (name is null) throw InvalidPlayerNameException.NameNull();
        string trimmed = name.Trim();
        if (trimmed.Length == 0) throw InvalidPlayerNameException.NameEmpty();
        if (trimmed.Length != name.Length) throw InvalidPlayerNameException.TrimDifferent();
        if (trimmed.Length > MaxLength) throw InvalidPlayerNameException.TooLong();
        if (ContainsIllegalCharacters(trimmed)) throw InvalidPlayerNameException.InvalidChar();
    }

    /// <summary>
    /// Returns if name is does not mach RegEx [a-zA-Z0-9_]
    /// </summary>
    private static bool ContainsIllegalCharacters(string name)
    {
        foreach (char c in name)
        {
            if (
                (c < 'a' || c > 'z') &&
                (c < 'A' || c > 'Z') &&
                (c < '0' || c > '9') &&
                c != '_'
            ) return true;
        }

        return false;
    }
}
