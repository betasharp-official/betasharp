namespace BetaSharp.Registries;

/// <summary>
/// Implemented by any system that holds cached data derived from registry entries and
/// needs to refresh that data when datapacks are reloaded via <c>/reload</c>.
/// </summary>
/// <remarks>
/// Register implementations with <see cref="BetaSharp.Server.BetaSharpServer.RegisterReloadListener"/>.
/// </remarks>
public interface IRegistryReloadListener
{
    void OnRegistriesRebuilt(RegistryAccess registryAccess);
}
