using System.Diagnostics;

string clientDir = Path.Combine(
    Environment.GetEnvironmentVariable("INST_DIR")
        ?? throw new Exception("INST_DIR environment variable not set"),
    "client"
    );

if (!File.Exists("b1.7.3.jar") && !CopyClientJar(args))
{
    Console.WriteLine("Failed to copy client jar");
    return 2;
}

CopyAssets(
    Path.Combine(clientDir, "assets"),
    Path.Combine(Directory.GetCurrentDirectory(), "assets"));

Dictionary<string, List<string>> parameters = new();
while (true)
{
    string? line = Console.ReadLine();
    if (line is null)
    {
        Console.WriteLine("Launch aborted by launcher");
        return 1;
    }
    else if (line == "")
    {
        continue;
    }
    else if (line == "launch")
    {
        return Launch(parameters, Path.Combine(clientDir, "BetaSharp.Client"));
    }
    else
    {
        int space = line.IndexOf(' ');
        if (space == -1)
        {
            Console.WriteLine("Expected parameter to be in format [key] [value], got \"{line}\"");
            return 65;
        }
        string key = line.Substring(0, space);
        string value = line.Substring(space + 1);
        List<string>? list;
        if (!parameters.TryGetValue(key, out list))
        {
            list = new();
            parameters[key] = list;
        }
        list.Add(value);
        continue;
    }
}

static int Launch(Dictionary<string, List<string>> parameters, string exe)
{
    var startInfo = new ProcessStartInfo()
    {
        FileName = exe,
        CreateNoWindow = true
    };
    startInfo.ArgumentList.Add(parameters["userName"][0]);
    startInfo.ArgumentList.Add(parameters["sessionId"][0]);
    using var p = Process.Start(startInfo);
    if (p is null)
    {
        throw new Exception("Failed to start client");
    }
    Console.WriteLine($"Client process started, PID: {p.Id}");
    p.WaitForExit();
    return p.ExitCode;
}

static bool CopyClientJar(string[] args)
{
    foreach (string arg in args)
    {
        foreach (string path in arg.Split(':'))
        {
            if (!path.Contains("b1.7.3-client.jar"))
                continue;
            File.Copy(path, "b1.7.3.jar");
            return true;
        }
    }
    return false;
}

static void CopyAssets(string src, string dest)
{
    foreach (string path in Directory.EnumerateFileSystemEntries(src))
    {
        string relative = Path.GetRelativePath(src, path);
        string input = Path.Combine(src, relative);
        string output = Path.Combine(dest, relative);
        if (File.Exists(path))
        {
            if (File.Exists(output))
                continue;
            File.Copy(input, output);
        }
        else
        {
            Directory.CreateDirectory(output);
            CopyAssets(input, output);
        }
    }
}
