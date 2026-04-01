using System.Diagnostics.CodeAnalysis;

namespace BetaSharp.Client.Guis;

public readonly partial struct Color
{
    private readonly int _value;

    public int A => BitConverter.IsLittleEndian ? _value >> 24 : _value & 0xFF;
    public int R => BitConverter.IsLittleEndian ? _value & 0xFF : _value >> 24;
    public int G => BitConverter.IsLittleEndian ? (_value >> 8) & 0xFF : (_value >> 16) & 0xFF;
    public int B => BitConverter.IsLittleEndian ? (_value >> 16) & 0xFF : (_value >> 8) & 0xFF;

    private Color(uint value)
    {
        _value = (int)value;
    }

    private Color(int value)
    {
        _value = value;
    }

    public static Color FromArgb(uint v)
    {
        if (BitConverter.IsLittleEndian)
        {
            return new((v & 0xFF00FF00) | ((v & 0x000000FF) << 16) | ((v & 0x00FF0000) >> 16));
        }
        else
        {
            return new((v << 8) | (v >> 24));
        }
    }

    public static Color FromRgb(uint v)
    {
        if (BitConverter.IsLittleEndian)
        {
            return new(0xFF000000 | ((v & 0x000000FF) << 16) | (v & 0x0000FF00) | ((v & 0x00FF0000) >> 16));
        }
        else
        {
            return new((v << 8) | 0xFF);
        }
    }

    public static Color FromColorCode(int colorCode, byte alpha = 0xFF, bool darken = false)
    {
        int baseColorOffset = (colorCode >> 3 & 1) * 85;
        int r = (colorCode >> 2 & 1) * 170 + baseColorOffset;
        int g = (colorCode >> 1 & 1) * 170 + baseColorOffset;
        int b = (colorCode >> 0 & 1) * 170 + baseColorOffset;

        if (colorCode == 6)
        {
            r += 85;
        }

        if (BitConverter.IsLittleEndian)
        {
            int v = b << 16 | g << 8 | r;
            if (darken) v = ((v & 0x00FCFCFC) >> 2);
            return new(v | (alpha << 24));
        }
        else
        {
            int v = r << 24 | g << 16 | b << 8;
            if (darken) v = (int)(((uint)v & 0xFCFCFC00) >> 2);
            return new(v | alpha);
        }
    }

    public Color Darken()
    {
        if (BitConverter.IsLittleEndian)
        {
            int a = _value >> 24;
            return new(((_value & 0x00FCFCFC) >> 2) | a << 24);
        }
        else
        {
            int a = _value & 0xFF;
            return new((int)(((uint)_value & 0xFCFCFC00) >> 2) | a);
        }
    }

    public Color(byte r, byte g, byte b, byte a = 0xFF)
    {
        if (BitConverter.IsLittleEndian)
        {
            _value = a << 24 | b << 16 | g << 8 | r;
        }
        else
        {
            _value = r << 24 | g << 16 | b << 8 | a;
        }
    }

    public Color(Color c, byte a = 0xFF)
    {
        if (BitConverter.IsLittleEndian)
        {
            _value = (int)((uint)c & 0x00FFFFFF) | a << 24;
        }
        else
        {
            _value = (int)((uint)c & 0xFFFFFF00) | a;
        }
    }

    public Color(float r, float g, float b, float a = 1f) : this((byte)(r * 0xFF), (byte)(g * 0xFF), (byte)(b * 0xFF), (byte)(a * 0xFF)) { }

    public static explicit operator uint(Color color) => (uint)color._value;
    public static explicit operator int(Color color) => color._value;
    public static explicit operator Color(uint color) => new(color);
    public static explicit operator Color(int color) => new(color);

    public override string ToString() => _value.ToString("x8");
    public override bool Equals([NotNullWhen(true)] object? obj) => _value.Equals(obj);
    public override int GetHashCode() => _value;

    public static bool operator ==(Color left, Color right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Color left, Color right)
    {
        return !(left == right);
    }
}
