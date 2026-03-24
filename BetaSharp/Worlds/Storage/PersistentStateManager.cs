using BetaSharp.NBT;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Worlds.Storage;

public class PersistentStateManager
{
    private readonly Dictionary<string, short> _idCounts = [];
    private readonly List<PersistentState> _loadedDataList = [];
    private readonly Dictionary<string, PersistentState> _loadedDataMap = [];

    private readonly ILogger<PersistentStateManager> _logger = Log.Instance.For<PersistentStateManager>();
    private readonly IWorldStorage? _saveHandler;

    public PersistentStateManager(IWorldStorage? saveHandler)
    {
        _saveHandler = saveHandler;
        LoadIdCounts();
    }

    public T? LoadData<T>(string id) where T : PersistentState => (T?)LoadData(typeof(T), id);

    public PersistentState? LoadData(Type type, string id) // On server never returns null, on client always
    {
        if (_loadedDataMap.TryGetValue(id, out PersistentState? existingState))
        {
            return existingState;
        }

        PersistentState? newState = null;

        if (_saveHandler != null)
        {
            try
            {
                FileInfo? file = _saveHandler.GetWorldPropertiesFile(id);
                if (file != null && file.Exists)
                {
                    try
                    {
                        newState = (PersistentState)Activator.CreateInstance(type, id)!;
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException($"Failed to instantiate {type.Name}", ex);
                    }

                    using FileStream stream = file.OpenRead();
                    NBTTagCompound rootTag = NbtIo.ReadCompressed(stream);

                    newState.ReadNBT(rootTag.GetCompoundTag("data"));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception loading data for state: {id}");
            }
        }

        if (newState != null)
        {
            _loadedDataMap[id] = newState;
            _loadedDataList.Add(newState);
        }

        return newState;
    }

    public void SetData(string id, PersistentState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (_loadedDataMap.ContainsKey(id))
        {
            _loadedDataList.Remove(_loadedDataMap[id]);
        }

        _loadedDataMap[id] = state;
        _loadedDataList.Add(state);
    }

    public void SaveAllData()
    {
        foreach (PersistentState state in _loadedDataList)
        {
            if (state.IsDirty())
            {
                SaveData(state);
                state.SetDirty(false);
            }
        }
    }

    private void SaveData(PersistentState state)
    {
        if (_saveHandler == null)
        {
            return;
        }

        try
        {
            FileInfo? file = _saveHandler.GetWorldPropertiesFile(state.Id);
            if (file != null)
            {
                NBTTagCompound stateTag = new();
                state.WriteNBT(stateTag);

                NBTTagCompound rootTag = new();
                rootTag.SetCompoundTag("data", stateTag);

                using FileStream stream = file.Create();
                NbtIo.WriteCompressed(rootTag, stream);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Exception saving state: {state.Id}");
        }
    }

    private void LoadIdCounts()
    {
        try
        {
            _idCounts.Clear();
            if (_saveHandler == null)
            {
                return;
            }

            FileInfo? file = _saveHandler.GetWorldPropertiesFile("idcounts");
            if (file != null && file.Exists)
            {
                using FileStream stream = file.OpenRead();
                NBTTagCompound rootTag = NbtIo.Read(stream);

                foreach (NBTBase tag in rootTag.Values)
                {
                    if (tag is NBTTagShort shortTag)
                    {
                        _idCounts[shortTag.Key] = shortTag.Value;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception loading idcounts.dat");
        }
    }

    public int GetUniqueDataId(string key)
    {
        short currentId = _idCounts.TryGetValue(key, out short count) ? count : (short)0;
        short nextId = (short)(currentId + 1);

        _idCounts[key] = nextId;

        if (_saveHandler == null)
        {
            return nextId;
        }

        try
        {
            FileInfo? file = _saveHandler.GetWorldPropertiesFile("idcounts");
            if (file != null)
            {
                NBTTagCompound rootTag = new();
                foreach (KeyValuePair<string, short> kvp in _idCounts)
                {
                    rootTag.SetShort(kvp.Key, kvp.Value);
                }

                using FileStream stream = file.Create();
                NbtIo.Write(rootTag, stream);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception saving idcounts.dat");
        }

        return nextId;
    }
}
