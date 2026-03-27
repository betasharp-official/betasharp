using BetaSharp.NBT;
using BetaSharp.Worlds.Chunks.Storage;
using BetaSharp.Worlds.Core.Systems;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Worlds.Storage;

public class RegionWorldStorageSource : IWorldStorageSource
{
    private readonly ILogger<RegionWorldStorageSource> _logger = Log.Instance.For<RegionWorldStorageSource>();
    protected readonly DirectoryInfo BaseDir;

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
        List<WorldSaveInfo> saves = new();

        if (!BaseDir.Exists)
        {
            return saves;
        }

        foreach (DirectoryInfo subDir in BaseDir.GetDirectories())
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

    public virtual void Flush() => RegionIo.Flush();

    public virtual IWorldStorage Get(string worldName, bool createPlayerStorage) => new RegionWorldStorage(BaseDir.FullName, worldName, createPlayerStorage);

    public virtual WorldProperties? GetProperties(string worldName)
    {
        DirectoryInfo worldDir = new(Path.Combine(BaseDir.FullName, worldName));
        if (!worldDir.Exists)
        {
            return null;
        }

        string[] searchFiles = { "level.dat", "level.dat_old" };

        foreach (string fileName in searchFiles)
        {
            FileInfo file = new(Path.Combine(worldDir.FullName, fileName));
            if (!file.Exists)
            {
                continue;
            }

            try
            {
                using FileStream stream = file.OpenRead();
                NBTTagCompound root = NbtIo.ReadCompressed(stream);
                NBTTagCompound data = root.GetCompoundTag("Data");

                WorldProperties properties = new(data)
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
        FileInfo file = new(Path.Combine(BaseDir.FullName, worldFolder, "level.dat"));
        if (!file.Exists)
        {
            return;
        }

        try
        {
            NBTTagCompound root;
            using (FileStream readStream = file.OpenRead())
            {
                root = NbtIo.ReadCompressed(readStream);
            }

            root.GetCompoundTag("Data").SetString("LevelName", newName);

            using (var writeStream = file.Create())
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
        DirectoryInfo dir = new(Path.Combine(BaseDir.FullName, worldFolder));
        if (dir.Exists)
        {
            dir.Delete(true);
        }
    }

    private static long GetFolderSize(DirectoryInfo folder)
    {
        long size = 0;
        foreach (FileInfo file in folder.GetFiles())
        {
            size += file.Length;
        }

        foreach (DirectoryInfo subDir in folder.GetDirectories())
        {
            size += GetFolderSize(subDir);
        }

        return size;
    }
}
