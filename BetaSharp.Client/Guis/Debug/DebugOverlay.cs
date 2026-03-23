namespace BetaSharp.Client.Guis.Debug;

public class DebugOverlay(BetaSharp game)
{
    public readonly DebugContext Context = new(game);
    public List<DebugComponent> Components = [];

    public void Draw()
    {
        Context.Initialize();

        foreach (DebugComponent component in Components)
        {
            Context.DrawComponent(component);
        }
    }
}
