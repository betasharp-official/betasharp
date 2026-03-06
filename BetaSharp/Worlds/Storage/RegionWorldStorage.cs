using BetaSharp.Entities;
using BetaSharp.NBT;
using BetaSharp.Server.Worlds;
using BetaSharp.Worlds.Core;
using BetaSharp.Worlds.Dimensions;
using BetaSharp.Worlds.Storage.RegionFormat;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Worlds.Storage;

internal class RegionWorldStorage : IWorldStorage, IPlayerStorage
{
    private readonly DirectoryInfo _dataDir;
    private readonly ILogger<RegionWorldStorage> _logger = Log.Instance.For<RegionWorldStorage>();


    private readonly long _now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    private readonly DirectoryInfo _playersDirectory;
    private readonly DirectoryInfo _saveDirectory;

    public RegionWorldStorage(string baseDir, string worldName, bool createPlayersDir)
    {
        _saveDirectory = new DirectoryInfo(Path.Combine(baseDir, worldName));
        if (!_saveDirectory.Exists)
        {
            _saveDirectory.Create();
        }

        _playersDirectory = new DirectoryInfo(Path.Combine(_saveDirectory.FullName, "players"));

        _dataDir = new DirectoryInfo(Path.Combine(_saveDirectory.FullName, "data"));
        if (!_dataDir.Exists)
        {
            _dataDir.Create();
        }

        if (createPlayersDir && !_playersDirectory.Exists)
        {
            _playersDirectory.Create();
        }

        WriteSessionLock();
    }

    public void SavePlayerData(EntityPlayer player)
    {
        try
        {
            NBTTagCompound tag = new();
            player.write(tag);

            string tempFile = Path.Combine(_playersDirectory.FullName, "_tmp_.dat");
            string finalFile = Path.Combine(_playersDirectory.FullName, $"{player.name}.dat");

            using (FileStream stream = File.OpenWrite(tempFile))
            {
                NbtIo.WriteCompressed(tag, stream);
            }

            File.Move(tempFile, finalFile, true);
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

    public void CheckSessionLock()
    {
        try
        {
            string lockFile = Path.Combine(_saveDirectory.FullName, "session.lock");
            using FileStream stream = File.OpenRead(lockFile);
            using BinaryReader reader = new(stream);

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
            DirectoryInfo netherDir = new(Path.Combine(_saveDirectory.FullName, "DIM-1"));
            if (!netherDir.Exists)
            {
                netherDir.Create();
            }

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

    public WorldProperties? LoadProperties()
    {
        string levelDat = Path.Combine(_saveDirectory.FullName, "level.dat");
        string levelDatOld = Path.Combine(_saveDirectory.FullName, "level.dat_old");

        string[] filesToTry = { levelDat, levelDatOld };

        foreach (string file in filesToTry)
        {
            if (!File.Exists(file))
            {
                continue;
            }

            try
            {
                using FileStream stream = File.OpenRead(file);
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

    public FileInfo GetWorldPropertiesFile(string name) => new(Path.Combine(_dataDir.FullName, $"{name}.dat"));

    public IPlayerStorage GetPlayerStorage() => this;

    public void ForceSave()
    {
    }

    private void WriteSessionLock()
    {
        try
        {
            string lockFile = Path.Combine(_saveDirectory.FullName, "session.lock");

            // Replaced DataOutputStream with native BinaryWriter
            using FileStream stream = File.OpenWrite(lockFile);
            using BinaryWriter writer = new(stream);
            writer.Write(_now);
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "Failed to check session lock, aborting.");
            throw new InvalidOperationException("Failed to check session lock, aborting", ex);
        }
    }

    private void WriteLevelDat(NBTTagCompound rootTag)
    {
        try
        {
            string levelDatNew = Path.Combine(_saveDirectory.FullName, "level.dat_new");
            string levelDatOld = Path.Combine(_saveDirectory.FullName, "level.dat_old");
            string levelDat = Path.Combine(_saveDirectory.FullName, "level.dat");

            using (FileStream stream = File.OpenWrite(levelDatNew))
            {
                NbtIo.WriteCompressed(rootTag, stream);
            }

            if (File.Exists(levelDatOld))
            {
                File.Delete(levelDatOld);
            }

            if (File.Exists(levelDat))
            {
                File.Move(levelDat, levelDatOld);
            }

            if (File.Exists(levelDatNew))
            {
                File.Move(levelDatNew, levelDat);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception writing level.dat");
        }
    }

    public NBTTagCompound loadPlayerData(string playerName)
    {
        try
        {
            string playerFile = Path.Combine(_playersDirectory.FullName, $"{playerName}.dat");
            if (File.Exists(playerFile))
            {
                using FileStream stream = File.OpenRead(playerFile);
                return NbtIo.ReadCompressed(stream);
            }

            // Fallback: Migrate single-player data from level.dat
            string levelFile = Path.Combine(_saveDirectory.FullName, "level.dat");
            if (File.Exists(levelFile))
            {
                try
                {
                    using FileStream stream = File.OpenRead(levelFile);
                    NBTTagCompound levelDat = NbtIo.ReadCompressed(stream);
                    NBTTagCompound data = levelDat.GetCompoundTag("Data");

                    if (data.HasKey("Player"))
                    {
                        NBTTagCompound playerTag = data.GetCompoundTag("Player");

                        using FileStream writeStream = File.OpenWrite(playerFile);
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
}
