using Microsoft.Extensions.Logging;

namespace BetaSharp.Client.Resource.Pack;

public class TexturePacks
{
    private readonly ILogger _logger = Log.Instance.For<TexturePacks>();
    private List<TexturePack> _availTexturePacks = [];
    private readonly TexturePack _defaultTexturePack = new BuiltInTexturePack();
    public TexturePack SelectedTexturePack;
    private readonly Dictionary<string, TexturePack> _texturePacks = [];
    private readonly BetaSharp _game;
    private readonly DirectoryInfo _texturePackDir;
    private string? _currentTexturePack;
    public List<TexturePack> AvailableTexturePacks => _availTexturePacks;

    public TexturePacks(BetaSharp game, DirectoryInfo texturePackDir)
    {
        _game = game;
        _texturePackDir = new DirectoryInfo(System.IO.Path.Combine(texturePackDir.FullName, "texturepacks"));
        if (!_texturePackDir.Exists)
        {
            _texturePackDir.Create();
        }

        _currentTexturePack = game.options.Skin;
        updateAvaliableTexturePacks();
        SelectedTexturePack.func_6482_a();
    }

    public bool setTexturePack(TexturePack texturePack)
    {
        if (texturePack == SelectedTexturePack)
        {
            return false;
        }

        SelectedTexturePack.CloseTexturePackFile();
        _currentTexturePack = texturePack.TexturePackFileName;
        SelectedTexturePack = texturePack;

        _game.options.Skin = _currentTexturePack;
        _game.options.SaveOptions();

        SelectedTexturePack.func_6482_a();
        return true;

    }

    public void updateAvaliableTexturePacks()
    {
        List<TexturePack> availablePacks = [];
        SelectedTexturePack = null!;
        availablePacks.Add(_defaultTexturePack);

        if (_texturePackDir.Exists)
        {
            foreach (FileInfo file in _texturePackDir.GetFiles("*.zip"))
            {
                string signature = $"{file.Name}:{file.Length}:{file.LastWriteTimeUtc.Ticks}";

                try
                {
                    if (!_texturePacks.TryGetValue(signature, out TexturePack? cachedPack))
                    {
                        ZippedTexturePack newPack = new(file)
                        {
                            Signature = signature
                        };
                        _texturePacks[signature] = newPack;
                        newPack.func_6485_a(_game);
                        cachedPack = newPack;
                    }

                    if (cachedPack.TexturePackFileName == _currentTexturePack)
                    {
                        SelectedTexturePack = cachedPack;
                    }

                    availablePacks.Add(cachedPack);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load texture pack {File}", file.Name);
                }

            }
        }

        SelectedTexturePack ??= _defaultTexturePack;

        foreach (TexturePack oldPack in _availTexturePacks)
        {
            if (!availablePacks.Contains(oldPack))
            {
                oldPack.Unload(_game);
                if (oldPack.Signature != null)
                {
                    _texturePacks.Remove(oldPack.Signature);
                }
            }
        }

        _availTexturePacks = availablePacks;
    }
}
