namespace BetaSharp.DataAsset;

/// <summary>
/// Essentially a safe pointer.<br/>
/// Allows the Asset to be updated from the <see cref="AssetLoader{T}"/>
/// </summary>
public class AssetRef<T1> where T1 : class, IAsset
{
    private IAssetProvider<T1> _assetProvider;

    public T1 Asset
    {
        get => _assetProvider.Asset;
        set => _assetProvider = new LoadedAsset<T1>(value);
    }

    public static implicit operator T1(AssetRef<T1> n) => n._assetProvider.Asset;

    public string Name => _assetProvider.Name;
    public Namespace Namespace => _assetProvider.Namespace;

    public override string ToString() => _assetProvider.ToString()!;
    public override int GetHashCode() => _assetProvider.GetHashCode();

    public AssetRef(T1 asset)
    {
        _assetProvider = new LoadedAsset<T1>(asset);
    }

    public AssetRef(AssetLoader<T1> loader, string path, Namespace ns, string name)
    {
        _assetProvider = new UnresolvedAsset<T1>(this, loader, path, ns, name);
    }

    private interface IAssetProvider<T> : IAsset where T : class, IAsset
    {
        T Asset { get; }
    }

    private class LoadedAsset<T>(T asset) : IAssetProvider<T> where T : class, IAsset
    {
        public T Asset { get; } = asset;

        string IAsset.Name
        {
            get => Asset.Name;
            set => Asset.Name = value;
        }

        Namespace IAsset.Namespace
        {
            get => Asset.Namespace;
            set => Asset.Namespace = value;
        }

        public override string ToString() => Asset.ToString()!;
        public override int GetHashCode() => Asset.GetHashCode();
    }

    private class UnresolvedAsset<T> : BaseDataAsset, IAssetProvider<T> where T : class, IAsset
    {
        private readonly AssetRef<T> _parent;
        private readonly AssetLoader<T> _loader;
        private readonly string _path;

        public T Asset
        {
            get
            {
                _loader.FromJsonReplace(_path, _parent);
                return _parent.Asset;
            }
        }

        public UnresolvedAsset(AssetRef<T> parent, AssetLoader<T> loader, string path, Namespace ns, string name)
        {
            _parent = parent;
            Name = name;
            Namespace = ns;
            _loader = loader;
            _path = path;
        }
    }
}
