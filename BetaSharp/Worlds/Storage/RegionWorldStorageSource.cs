using BetaSharp.NBT;
using BetaSharp.Worlds.Chunks.Storage;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Worlds.Storage;

public class RegionWorldStorageSource : IWorldStorageSource
{
    protected readonly DirectoryInfo BaseDir;

    private readonly ILogger<RegionWorldStorageSource> _logger = Log.Instance.For<RegionWorldStorageSource>();

    public RegionWorldStorageSource(string path)
    {
        BaseDir = new DirectoryInfo(path);
        if (!BaseDir.Exists)
        {
            BaseDir.Create();
        }
    }

    public virtual string Name => "Scaevolus' McRegion";

    public virtual List<WorldSaveInfo> GetAll()
    {
        var saves = new List<WorldSaveInfo>();

        if (!BaseDir.Exists) return saves;

        foreach (var subDir in BaseDir.GetDirectories())
        {
            string folderName = subDir.Name;
            WorldProperties? props = GetProperties(folderName);

            if (props != null)
            {
                bool requiresConversion = props.SaveVersion != 19132;
                string displayName = string.IsNullOrEmpty(props.LevelName) ? folderName : props.LevelName;

                saves.Add(new WorldSaveInfo(
                    folderName,
                    displayName,
                    props.LastTimePlayed,
                    props.SizeOnDisk,
                    requiresConversion));
            }
        }

        return saves;
    }

    public virtual void Flush()
    {
        RegionIo.Flush();
    }

    public virtual IWorldStorage Get(string worldName, bool createPlayerStorage)
    {
        return new RegionWorldStorage(BaseDir.FullName, worldName, createPlayerStorage);
    }

    private static long GetFolderSize(DirectoryInfo folder)
    {
        long size = 0;
        foreach (var file in folder.GetFiles()) size += file.Length;
        foreach (var subDir in folder.GetDirectories()) size += GetFolderSize(subDir);
        return size;
    }

    public virtual WorldProperties? GetProperties(string worldName)
    {
        var worldDir = new DirectoryInfo(System.IO.Path.Combine(BaseDir.FullName, worldName));
        if (!worldDir.Exists) return null;

        string[] searchFiles = { "level.dat", "level.dat_old" };

        foreach (var fileName in searchFiles)
        {
            var file = new FileInfo(System.IO.Path.Combine(worldDir.FullName, fileName));
            if (!file.Exists) continue;

            try
            {
                using var stream = file.OpenRead();
                var root = NbtIo.ReadCompressed(stream);
                var data = root.GetCompoundTag("Data");

                var properties = new WorldProperties(data)
                {
                    SizeOnDisk = GetFolderSize(worldDir)
                };
                return properties;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to load {fileName} for {worldName}");
            }
        }

        return null;
    }

    public void Rename(string worldFolder, string newName)
    {
        var file = new FileInfo(System.IO.Path.Combine(BaseDir.FullName, worldFolder, "level.dat"));
        if (!file.Exists) return;

        try
        {
            NBTTagCompound root;
            using (var readStream = file.OpenRead())
            {
                root = NbtIo.ReadCompressed(readStream);
            }

            root.GetCompoundTag("Data").SetString("LevelName", newName);

            using (var writeStream = file.OpenWrite())
            {
                NbtIo.WriteCompressed(root, writeStream);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error renaming world {worldFolder}");
        }
    }

    public void Delete(string worldFolder)
    {
        var dir = new DirectoryInfo(System.IO.Path.Combine(BaseDir.FullName, worldFolder));
        if (dir.Exists)
        {
            dir.Delete(recursive: true);
        }
    }
}
