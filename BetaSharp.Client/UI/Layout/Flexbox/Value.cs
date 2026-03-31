namespace BetaSharp.Client.UI.Layout.Flexbox;

public class Value
{
    public float value;
    public Unit unit;

    public Value(float v, Unit u)
    {
        this.value = v;
        this.unit = u;
    }

    public static Value UndefinedValue
    {
        get
        {
            return new Value(float.NaN, Unit.Undefined);
        }
    }

    public static void CopyValue(Value[] dest, Value[] src)
    {
        for (int i = 0; i < src.Length; i++)
        {
            dest[i].value = src[i].value;
            dest[i].unit = src[i].unit;
        }
    }

    public Value Clone()
    {
        return new Value(value, unit);
    }
}
