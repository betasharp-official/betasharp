namespace BetaSharp.Stats;

internal class StatBasic : StatBase
{
    public StatBasic(int id, string statName, Func<int, string> formatter) : base(id, statName, formatter) { }

    public StatBasic(int id, string statName) : base(id, statName) { }

    public override StatBase RegisterStat()
    {
        base.RegisterStat();
        Stats.GeneralStats.Add(this);
        return this;
    }
}
