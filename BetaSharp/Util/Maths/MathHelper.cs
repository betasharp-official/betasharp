using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace BetaSharp.Util.Maths;

public static class MathHelper
{
    private static readonly float[] SinTable = new float[65536];
    private const float FastMathFactor = 65536.0f / (float)(Math.PI * 2.0);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Sin(float value)
    {
        // Bypasses .NET array bounds checking for maximum performance
        ref float tableRef = ref MemoryMarshal.GetArrayDataReference(SinTable);
        return Unsafe.Add(ref tableRef, (int)(value * FastMathFactor) & 0xFFFF);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Cos(float value)
    {
        // 16384 is 90 degrees in this 65536-step table
        ref float tableRef = ref MemoryMarshal.GetArrayDataReference(SinTable);
        return Unsafe.Add(ref tableRef, (int)(value * FastMathFactor + 16384.0f) & 0xFFFF);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Sqrt(float value) => MathF.Sqrt(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Sqrt(double value) => (float)Math.Sqrt(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Floor(float value)
    {
        int truncatedValue = (int)value;
        return value < truncatedValue ? truncatedValue - 1 : truncatedValue;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Floor(double value)
    {
        int i = (int)value;
        return value < i ? i - 1 : i;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Abs(float value) => MathF.Abs(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int FloorDiv(int a, int b)
    {
        return a < 0 ? -((-a - 1) / b) - 1 : a / b;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Round(float value)
    {
        return (int)MathF.Round(value, MidpointRounding.ToPositiveInfinity);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long Round(double value)
    {
        return (long)Math.Round(value, MidpointRounding.ToPositiveInfinity);
    }

    static MathHelper()
    {
        for (int i = 0; i < 65536; ++i)
        {
            SinTable[i] = (float)Math.Sin(i * Math.PI * 2.0D / 65536.0D);
        }
    }
}
