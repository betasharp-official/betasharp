namespace BetaSharp;

/// <summary>
/// Thrown when a player display name violates <see cref="PlayerNameValidator"/> rules.
/// </summary>
public sealed class InvalidPlayerNameException : ArgumentException
{
    public InvalidPlayerNameException(string message) : base(message) { }

    public static InvalidPlayerNameException NameNull() => new InvalidPlayerNameException("Player name is required.");
    public static InvalidPlayerNameException NameEmpty() => new InvalidPlayerNameException("Player name cannot be empty.");
    public static InvalidPlayerNameException TrimDifferent() =>  new InvalidPlayerNameException("Player name cannot have leading or trailing whitespace.");
    public static InvalidPlayerNameException TooLong() => new InvalidPlayerNameException($"Player name cannot be longer than {PlayerNameValidator.MaxLength} characters.");
    public static InvalidPlayerNameException InvalidChar()  => new InvalidPlayerNameException("Player name may not contain illegal characters. Only [a-zA-Z0-9_] are allowed.");
}
