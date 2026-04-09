using BetaSharp.Client.Rendering;
using BetaSharp.Client.UI.Screens;
using BetaSharp.Stats;

namespace BetaSharp.Client.Network;

public sealed class ClientNetworkContext(
    IClientPlayerHost playerHost,
    IWorldHost worldHost,
    IScreenNavigator navigator,
    Session session,
    StatFileWriter statFileWriter,
    IParticleManager particleManager,
    Action<string> addChatMessage,
    IClientNetworkFactory factory)
{
    public IClientPlayerHost PlayerHost => playerHost;
    public IWorldHost WorldHost => worldHost;
    public IScreenNavigator Navigator => navigator;
    public Session Session => session;
    public StatFileWriter StatFileWriter => statFileWriter;
    public IParticleManager ParticleManager => particleManager;
    public Action<string> AddChatMessage => addChatMessage;
    public IClientNetworkFactory Factory => factory;
}
