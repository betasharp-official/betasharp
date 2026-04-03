namespace BetaSharp;

/// <summary>
/// Thrown when a player display name violates <see cref="PlayerNameValidator"/> rules.
/// </summary>
public sealed class InvalidPlayerNameException(string message) : ArgumentException(message);
