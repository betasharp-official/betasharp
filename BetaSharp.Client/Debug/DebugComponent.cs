namespace BetaSharp.Client.Debug;

public abstract class DebugComponent
{
    public bool Right { get; set; }
    public abstract IEnumerable<DebugRowData> GetRows(DebugContext ctx);
    public abstract DebugComponent Duplicate();
}
