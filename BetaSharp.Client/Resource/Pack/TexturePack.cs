namespace BetaSharp.Client.Resource.Pack;

public abstract class TexturePack
{
    public string? TexturePackFileName;
    public string? FirstDescriptionLine;
    public string? SecondDescriptionLine;
    public string? Signature;

    public virtual void func_6482_a()
    {
    }

    public virtual void CloseTexturePackFile()
    {
    }

    public virtual void func_6485_a(BetaSharp game)
    {
    }

    public virtual void Unload(BetaSharp game)
    {
    }

    public virtual void BindThumbnailTexture(BetaSharp game)
    {
    }

    public virtual Stream? GetResourceAsStream(string path)
    {
        try
        {
            AssetManager.Asset asset = AssetManager.Instance.getAsset(path);
            if (asset == null) return null;
            return new MemoryStream(asset.getBinaryContent());
        }
        catch (Exception)
        {
            return null;
        }
    }
}
