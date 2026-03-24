namespace BetaSharp.Server;

using System.Text;

public sealed class Properties : Dictionary<string, string>
{
    public string GetProperty(string key, string defaultValue = "")
        => TryGetValue(key, out string? value) ? value : defaultValue;

    public void SetProperty(string key, string value)
        => this[key] = value;

    public static Properties Load(string path)
    {
        var props = new Properties();

        if (!File.Exists(path))
            return props;

        foreach (string rawLine in File.ReadAllLines(path, Encoding.UTF8))
        {
            string line = rawLine.Trim();

            if (string.IsNullOrWhiteSpace(line))
                continue;

            if (line.StartsWith('#') || line.StartsWith('!'))
                continue;

            int idx = line.IndexOf('=');
            if (idx < 0)
                idx = line.IndexOf(':');

            if (idx < 0)
            {
                props[line] = "";
                continue;
            }

            string key = line[..idx].Trim();
            string value = line[(idx + 1)..].Trim();

            props[key] = value;
        }

        return props;
    }

    public void Save(string path, string? comment = null)
    {
        using var writer = new StreamWriter(path, false, Encoding.UTF8);

        if (!string.IsNullOrEmpty(comment))
            writer.WriteLine($"# {comment}");

        foreach (var pair in this)
            writer.WriteLine($"{pair.Key}={pair.Value}");
    }
}
