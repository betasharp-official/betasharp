using BetaSharp.Client.Rendering;
using BetaSharp.Client.Rendering.Entities;
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
    IEntityRenderDispatcher entityRenderDispatcher,
    Action<string> addChatMessage,
    IClientNetworkFactory factory)
{
    public IClientPlayerHost PlayerHost => playerHost;
    public IWorldHost WorldHost => worldHost;
    public IScreenNavigator Navigator => navigator;
    public Session Session => session;
    public StatFileWriter StatFileWriter => statFileWriter;
    public IParticleManager ParticleManager => particleManager;
    public IEntityRenderDispatcher EntityRenderDispatcher => entityRenderDispatcher;
    public Action<string> AddChatMessage => addChatMessage;
    public IClientNetworkFactory Factory => factory;
}
