namespace BetaSharp.Tests;

/// <summary>
/// Prevents xUnit from running <see cref="RegistryAccessTests"/> and
/// <see cref="GameModesTests"/> in parallel, since both mutate the global
/// <c>RegistryAccess.s_dynamicEntries</c> list.
/// </summary>
[CollectionDefinition("RegistryAccess")]
public class RegistryAccessCollection;
