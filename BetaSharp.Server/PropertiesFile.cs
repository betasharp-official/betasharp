using System.Text;

namespace BetaSharp.Server;

/// <summary>
/// A simple key=value properties file reader/writer compatible with the Java .properties format.
/// </summary>
internal sealed class PropertiesFile
{
    private readonly Dictionary<string, string> _data = new(StringComparer.Ordinal);

    public void Load(string filePath)
    {
        _data.Clear();
        foreach (string line in File.ReadAllLines(filePath, Encoding.UTF8))
        {
            string trimmed = line.TrimStart();
            if (trimmed.Length == 0 || trimmed[0] == '#' || trimmed[0] == '!')
                continue;

            int eq = trimmed.IndexOf('=');
            if (eq <= 0)
                continue;

            string key = trimmed[..eq].Trim();
            string value = trimmed[(eq + 1)..].Trim();
            _data[key] = value;
        }
    }

    public void Save(string filePath, string header)
    {
        StringBuilder sb = new();
        sb.AppendLine($"# {header}");
        foreach (KeyValuePair<string, string> pair in _data)
        {
            sb.AppendLine($"{pair.Key}={pair.Value}");
        }
        File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
    }

    public bool ContainsKey(string key) => _data.ContainsKey(key);

    public string GetProperty(string key, string fallback)
        => _data.TryGetValue(key, out string? value) ? value : fallback;

    public void SetProperty(string key, string value)
        => _data[key] = value;
}
