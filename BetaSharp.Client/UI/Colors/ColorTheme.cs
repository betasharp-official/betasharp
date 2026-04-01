using System.Reflection;

namespace BetaSharp.Client.Guis;

public readonly partial struct Color
{
    /// <summary>
    /// Set colors by name using json.
    /// </summary>
    /// <param name="json">Color value given in hex ARGB or RGB</param>
    /// <exception cref="ArgumentException">Bad input</exception>
    /// <example>
    /// Swap black and white
    /// <code>
    /// SetTheme("{\"White\":\"000000\",\"Black\":\"FFFFFF\"}");
    /// </code>
    /// </example>
    public static void SetTheme(string json)
    {
        string[] split = json.Split('"');
        if (((split.Length - 1) % 4) != 0) throw new ArgumentException("Invalid json theme format", nameof(json));

        for (int i = 1, l = split.Length - 1; i < l; i += 4)
        {
            SetColorWithStr(split[i], split[i + 2]);
        }
    }

    /// <summary>
    /// Set colors by name using dictionary.
    /// </summary>
    /// <param name="dict">Color value given in hex ARGB or RGB</param>
    /// <exception cref="ArgumentException">Bad input</exception>
    /// <example>
    /// Swap black and white
    /// <code>
    /// SetTheme(new Dictionary&lt;string, string&gt;() {{"White", "000000"}, {"Black", "FFFFFF"}});
    /// </code>
    /// </example>
    public static void SetTheme(IReadOnlyDictionary<string, string> dict)
    {
        foreach (KeyValuePair<string, string> pair in dict)
        {
            SetColorWithStr(pair.Key, pair.Value);
        }
    }

    /// <summary>
    /// Set color by name and set color by hex string
    /// </summary>
    /// <param name="name">name of color</param>
    /// <param name="colorStr">Color in hexadecimal</param>
    /// <exception cref="ArgumentException">Invalid color</exception>
    private static void SetColorWithStr(string name, string colorStr)
    {
        PropertyInfo? prop = typeof(Color).GetProperty(name);
        if (prop == null)
        {
            Console.WriteLine($"Color by name \"{name}\" not found");
            return;
        }

        // remove "0x" hex header.
        if (colorStr.StartsWith("0x")) colorStr = colorStr.Substring(2);

        int lenght = colorStr.Length;
        if (lenght > 8) throw new ArgumentException($"Invalid json color format \"{colorStr}\"", nameof(colorStr));

        Color color = lenght <= 6 ? FromRgb(Convert.ToUInt32(colorStr, 16)) : FromArgb(Convert.ToUInt32(colorStr, 16));
        prop.SetValue(null, color);
    }
}
