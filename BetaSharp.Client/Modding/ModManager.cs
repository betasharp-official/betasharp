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
            Mods.Add(modInstance);
        }
    }

    public void ApplyPatches()
    {
        foreach (var mod in Mods) mod.ApplyPatches();
    }

    public void InitMods()
    {
        foreach (var mod in Mods) mod.InternalInit(game);
    }

    public void LoadMods()
    {
        AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
        {
            // Get just the name (e.g., "BetaSharp" or "0Harmony")
            string assemblyName = new AssemblyName(args.Name).Name;

            // Check if it's already loaded in the game's memory
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.GetName().Name == assemblyName) return assembly;
            }
            return null;
        };

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
