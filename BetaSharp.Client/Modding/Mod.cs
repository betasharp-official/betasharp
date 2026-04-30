using BetaSharp.Client;

namespace BetaSharp.Client.Modding;

public abstract class Mod
{
    public abstract string ID { get; }
    public abstract string Name { get; }
    public abstract string Description { get; }
    public abstract string Author { get; }

    public BetaSharp Game { get; internal set; }

    public abstract void Start();
}
