using BetaSharp.Entities;
using BetaSharp.NBT;
using BetaSharp.Server.Worlds;
using BetaSharp.Worlds.Chunks.Storage;
using BetaSharp.Worlds.Core.Systems;
using BetaSharp.Worlds.Dimensions;
using BetaSharp.Worlds.Storage.RegionFormat;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Worlds.Storage;

internal class RegionWorldStorage : IWorldStorage, IPlayerStorage
{
    private readonly DirectoryInfo _saveDirectory;
    private readonly DirectoryInfo _playersDirectory;
    private readonly DirectoryInfo _dataDir;


    private readonly long _now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    private readonly ILogger<RegionWorldStorage> _logger = Log.Instance.For<RegionWorldStorage>();

    public RegionWorldStorage(string baseDir, string worldName, bool createPlayersDir)
    {
        _saveDirectory = new DirectoryInfo(Path.Combine(baseDir, worldName));
        if (!_saveDirectory.Exists) _saveDirectory.Create();

        _playersDirectory = new DirectoryInfo(Path.Combine(_saveDirectory.FullName, "players"));

        _dataDir = new DirectoryInfo(Path.Combine(_saveDirectory.FullName, "data"));
        if (!_dataDir.Exists) _dataDir.Create();

        if (createPlayersDir && !_playersDirectory.Exists)
        {
            _playersDirectory.Create();
        }

        WriteSessionLock();
    }

    private void WriteSessionLock()
    {
        try
        {
            string lockFile = Path.Combine(_saveDirectory.FullName, "session.lock");

            using var stream = File.Create(lockFile);
            using var writer = new BinaryWriter(stream);
            writer.Write(_now);
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "Failed to check session lock, aborting.");
            throw new InvalidOperationException("Failed to check session lock, aborting", ex);
        }
    }

    public void CheckSessionLock()
    {
        try
        {
            string lockFile = Path.Combine(_saveDirectory.FullName, "session.lock");
            using var stream = File.OpenRead(lockFile);
            using var reader = new BinaryReader(stream);

            if (reader.ReadInt64() != _now)
            {
                throw new Exception("The save is being accessed from another location, aborting");
            }
        }
        catch (IOException ex)
        {
            throw new Exception("Failed to check session lock, aborting", ex);
        }
    }

    public virtual IChunkStorage GetChunkStorage(Dimension dimension)
    {
        if (dimension is NetherDimension)
        {
            var netherDir = new DirectoryInfo(Path.Combine(_saveDirectory.FullName, "DIM-1"));
            if (!netherDir.Exists) netherDir.Create();

            return new RegionChunkStorage(netherDir.FullName);
        }

        return new RegionChunkStorage(_saveDirectory.FullName);
    }

    public virtual void Save(WorldProperties properties, List<EntityPlayer> players)
    {
        properties.SaveVersion = 19132;

        NBTTagCompound playerData = properties.getNBTTagCompoundWithPlayer(players);
        NBTTagCompound rootTag = new();
        rootTag.SetTag("Data", playerData);

        WriteLevelDat(rootTag);
    }

    public void Save(WorldProperties properties)
    {
        NBTTagCompound dataTag = properties.getNBTTagCompound();
        NBTTagCompound rootTag = new();
        rootTag.SetTag("Data", dataTag);

        WriteLevelDat(rootTag);
    }

    private void WriteLevelDat(NBTTagCompound rootTag)
    {
        try
        {
            string levelDatNew = Path.Combine(_saveDirectory.FullName, "level.dat_new");
            string levelDatOld = Path.Combine(_saveDirectory.FullName, "level.dat_old");
            string levelDat = Path.Combine(_saveDirectory.FullName, "level.dat");

            using (var stream = File.Create(levelDatNew))
            {
                NbtIo.WriteCompressed(rootTag, stream);
            }

            if (File.Exists(levelDatOld)) File.Delete(levelDatOld);
            if (File.Exists(levelDat)) File.Move(levelDat, levelDatOld);
            if (File.Exists(levelDatNew)) File.Move(levelDatNew, levelDat);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception writing level.dat");
        }
    }

    public WorldProperties? LoadProperties()
    {
        string levelDat = Path.Combine(_saveDirectory.FullName, "level.dat");
        string levelDatOld = Path.Combine(_saveDirectory.FullName, "level.dat_old");

        string[] filesToTry = { levelDat, levelDatOld };

        foreach (var file in filesToTry)
        {
            if (!File.Exists(file)) continue;

            try
            {
                using var stream = File.OpenRead(file);
                NBTTagCompound root = NbtIo.ReadCompressed(stream);
                NBTTagCompound data = root.GetCompoundTag("Data");
                return new WorldProperties(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception loading properties from {file}");
            }
        }

        return null;
    }

    public FileInfo GetWorldPropertiesFile(string name)
    {
        return new FileInfo(Path.Combine(_dataDir.FullName, $"{name}.dat"));
    }

    public void SavePlayerData(EntityPlayer player)
    {
        try
        {
            NBTTagCompound tag = new();
            player.write(tag);

            string tempFile = Path.Combine(_playersDirectory.FullName, "_tmp_.dat");
            string finalFile = Path.Combine(_playersDirectory.FullName, $"{player.name}.dat");

            using (var stream = File.Create(tempFile))
            {
                NbtIo.WriteCompressed(tag, stream);
            }

            File.Move(tempFile, finalFile, overwrite: true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"Failed to save player data for {player.name}");
        }
    }

    public void LoadPlayerData(EntityPlayer player)
    {
        NBTTagCompound tag = loadPlayerData(player.name);
        if (tag != null)
        {
            player.read(tag);
        }
    }

    public NBTTagCompound loadPlayerData(string playerName)
    {
        try
        {
            string playerFile = Path.Combine(_playersDirectory.FullName, $"{playerName}.dat");
            if (File.Exists(playerFile))
            {
                using var stream = File.OpenRead(playerFile);
                return NbtIo.ReadCompressed(stream);
            }

            string levelFile = Path.Combine(_saveDirectory.FullName, "level.dat");
            if (File.Exists(levelFile))
            {
                try
                {
                    using var stream = File.OpenRead(levelFile);
                    NBTTagCompound levelDat = NbtIo.ReadCompressed(stream);
                    NBTTagCompound data = levelDat.GetCompoundTag("Data");

                    if (data.HasKey("Player"))
                    {
                        NBTTagCompound playerTag = data.GetCompoundTag("Player");

                        using var writeStream = File.Create(playerFile);
                        NbtIo.WriteCompressed(playerTag, writeStream);

                        _logger.LogInformation($"Migrated singleplayer player data from level.dat to {playerName}.dat");
                        return playerTag;
                    }
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, "Failed to migrate player data from level.dat");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"Failed to load player data for {playerName}");
        }

        return null;
    }

    public IPlayerStorage GetPlayerStorage() => this;

    public void ForceSave() { }
}
