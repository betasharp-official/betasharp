using System.Reflection;

namespace BetaSharp.Client.Modding;

public class ModManager(string modsFolder, BetaSharp game)
{
    public List<Mod> Mods = new List<Mod>();
    private void LoadMod(string dllPath)
    {
        Assembly assembly = Assembly.LoadFrom(dllPath);

        // get mod types
        var modTypes = assembly.GetTypes().Where(t =>
            typeof(Mod).IsAssignableFrom(t) && // Does it implement Mod?
            !t.IsInterface &&                  // Can't be interface
            !t.IsAbstract);                    // Must be class

        foreach (Type type in modTypes)
        {
            // Create instance
            Mod modInstance = (Mod)Activator.CreateInstance(type);

            // Store
            modInstance.Game = game;
            Mods.Add(modInstance);
        }
    }

    public void Start()
    {
        foreach (var mod in Mods) mod.Start();
    }

    public void LoadMods()
    {
        if (!Directory.Exists(modsFolder))
        {
            Directory.CreateDirectory(modsFolder);
            return;
        }

        foreach (string dllPath in Directory.GetFiles(modsFolder, "*.dll"))
        {
            LoadMod(Path.GetFullPath(dllPath));
        }
    }
}
