namespace BetaSharp.DataAsset;

/// <summary>
/// Essentially a safe pointer.<br/>
/// Allows the Asset to be updated from the <see cref="DataAssetLoader{T}"/>
/// </summary>
public class DataAssetRef<T1> where T1 : class, IDataAsset
{
    private IDataAssetProvider<T1> _dataAssetProvider;

    public T1 Asset
    {
        get => _dataAssetProvider.Asset;
        set => _dataAssetProvider = new LoadedDataAsset<T1>(value);
    }

    public static implicit operator T1(DataAssetRef<T1> n) => n._dataAssetProvider.Asset;

    public string Name => _dataAssetProvider.Name;
    public Namespace Namespace => _dataAssetProvider.Namespace;

    public override string ToString() => _dataAssetProvider.ToString()!;
    public override int GetHashCode() => _dataAssetProvider.GetHashCode();

    public DataAssetRef(T1 asset)
    {
        _dataAssetProvider = new LoadedDataAsset<T1>(asset);
    }

    public DataAssetRef(DataAssetLoader<T1> loader, string path, Namespace ns, string name)
    {
        _dataAssetProvider = new UnresolvedDataAsset<T1>(this, loader, path, ns, name);
    }

    private interface IDataAssetProvider<T> : IDataAsset where T : class, IDataAsset
    {
        T Asset { get; }
    }

    private class LoadedDataAsset<T>(T asset) : IDataAssetProvider<T> where T : class, IDataAsset
    {
        public T Asset { get; } = asset;

        string IDataAsset.Name
        {
            get => Asset.Name;
            set => Asset.Name = value;
        }

        Namespace IDataAsset.Namespace
        {
            get => Asset.Namespace;
            set => Asset.Namespace = value;
        }

        public override string ToString() => Asset.ToString()!;
        public override int GetHashCode() => Asset.GetHashCode();
    }

    private class UnresolvedDataAsset<T> : BaseDataDataAsset, IDataAssetProvider<T> where T : class, IDataAsset
    {
        private readonly DataAssetRef<T> _parent;
        private readonly DataAssetLoader<T> _loader;
        private readonly string _path;

        public T Asset
        {
            get
            {
                _loader.FromJsonReplace(_path, _parent);
                return _parent.Asset;
            }
        }

        public UnresolvedDataAsset(DataAssetRef<T> parent, DataAssetLoader<T> loader, string path, Namespace ns, string name)
        {
            _parent = parent;
            Name = name;
            Namespace = ns;
            _loader = loader;
            _path = path;
        }
    }
}
