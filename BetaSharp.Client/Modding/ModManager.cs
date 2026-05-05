using System.IO.Compression;
using System.Reflection;
using SixLabors.ImageSharp.Drawing.Processing;

namespace BetaSharp.Client.Modding;

public class ModManager(string modsFolder, BetaSharp game)
{
    public List<Mod> Mods = new List<Mod>();

    private void LoadModAssembly(Assembly assembly, ZipArchive? assetArchive = null)
    {
        // get mod types
        var modTypes = assembly.GetTypes().Where(t =>
            typeof(Mod).IsAssignableFrom(t) && // Does it implement Mod?
            !t.IsInterface &&                  // Can't be interface
            !t.IsAbstract);                    // Must be class

        foreach (Type type in modTypes)
        {
            // Create instance
            Mod modInstance = (Mod)Activator.CreateInstance(type);

            if (assetArchive is ZipArchive archive) {
                foreach (var tuple in modInstance.Assets)
                {
                    string entryPath = "assets" + "/" + tuple.path;
                    ZipArchiveEntry entry = archive.GetEntry(entryPath);
                    using (Stream stream = entry.Open())
                    {
                        if (tuple.type == AssetManager.AssetType.Binary)
                        {
                            using (MemoryStream ms = new MemoryStream())
                            {
                                stream.CopyTo(ms);
                                AssetManager.Instance.AddBinaryAsset(tuple.path, ms.ToArray());
                            }
                        } else {
                            using (StreamReader reader = new StreamReader(stream))
                            {
                                string text = reader.ReadToEnd();
                                AssetManager.Instance.AddTextAsset(tuple.path, text);
                            }
                        }
                    }
                }
            }


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

        Mod.Game = game;

        if (!Directory.Exists(modsFolder))
        {
            Directory.CreateDirectory(modsFolder);
            return;
        }

        foreach (string dllPath in Directory.GetFiles(modsFolder, "*.dll"))
        {
            LoadModAssembly(Assembly.LoadFrom(Path.GetFullPath(dllPath)));
        }

        foreach (string zipPath in Directory.GetFiles(modsFolder, "*.zip"))
        {
            using (ZipArchive archive = ZipFile.OpenRead(zipPath))
            {
                // load dlls
                
                foreach (var entry in archive.Entries.Where((e) => e.FullName.EndsWith(".dll")))
                {
                    using (var stream = entry.Open())
                    using (var ms = new MemoryStream())
                    {
                        stream.CopyTo(ms);
                        byte[] assemblyBytes = ms.ToArray();
                        Assembly assembly = Assembly.Load(assemblyBytes);
                        LoadModAssembly(assembly, archive);
                    }
                }
            }
        }
    }
}
