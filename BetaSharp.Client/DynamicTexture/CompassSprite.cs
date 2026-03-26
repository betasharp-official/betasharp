using BetaSharp.Client.Rendering.Core.Textures;
using BetaSharp.Items;
using BetaSharp.Util.Maths;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace BetaSharp.Client.DynamicTexture;

internal class CompassSprite : Rendering.Core.Textures.DynamicTexture
{
    private readonly ILogger<CompassSprite> _logger = Log.Instance.For<CompassSprite>();
    private double _angle;
    private double _angleDelta;
    private int[] _compass = new int[256];
    private BetaSharp _game;
    private int _resolution = 16;

    public CompassSprite(BetaSharp game) : base(Item.Compass.getTextureId(0))
    {
        _game = game;
        Atlas = FxImage.Items;
    }

    public override void Setup(BetaSharp game)
    {
        _game = game;
        TextureManager tm = game.textureManager;
        string atlasPath = "/gui/items.png";

        TextureHandle handle = tm.GetTextureId(atlasPath);
        if (handle.Texture != null)
        {
            _resolution = handle.Texture.Width / 16;
        }
        else
        {
            _resolution = 16;
        }

        int pixelCount = _resolution * _resolution;
        if (_compass.Length != pixelCount)
        {
            _compass = new int[pixelCount];
            Pixels = new byte[pixelCount * 4];
        }

        try
        {
            using Stream? stream = game.texturePackList.SelectedTexturePack.GetResourceAsStream("gui/items.png");
            if (stream != null)
            {
                using Image<Rgba32> atlasImage = Image.Load<Rgba32>(stream);
                int localRes = atlasImage.Width / 16;
                int sourceX = (Sprite % 16) * localRes;
                int sourceY = (Sprite / 16) * localRes;

                for (int y = 0; y < _resolution; y++)
                {
                    for (int x = 0; x < _resolution; x++)
                    {
                        int srcX = sourceX + (x * localRes / _resolution);
                        int srcY = sourceY + (y * localRes / _resolution);

                        Rgba32 pixel = atlasImage[srcX, srcY];
                        _compass[y * _resolution + x] = (pixel.A << 24) | (pixel.R << 16) | (pixel.G << 8) | pixel.B;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,"Error loading compass sprite");
        }
    }

    public override void tick()
    {
        int pixelCount = _resolution * _resolution;

        for (int i = 0; i < pixelCount; ++i)
        {
            int a = (_compass[i] >> 24) & 255;
            int r = (_compass[i] >> 16) & 255;
            int g = (_compass[i] >> 8) & 255;
            int b = (_compass[i] >> 0) & 255;
            Pixels[i * 4 + 0] = (byte)r;
            Pixels[i * 4 + 1] = (byte)g;
            Pixels[i * 4 + 2] = (byte)b;
            Pixels[i * 4 + 3] = (byte)a;
        }

        double targetAngle = 0.0D;
        if (_game.world != null && _game.player != null)
        {
            Vec3i spawnPos = _game.world.Properties.GetSpawnPos();
            double deltaX = spawnPos.X - _game.player.x;
            double deltaZ = spawnPos.Z - _game.player.z;

            targetAngle = (_game.player.yaw - 90.0F) * Math.PI / 180.0D - Math.Atan2(deltaZ, deltaX);

            if (_game.world.Dimension.IsNether)
            {
                targetAngle = Random.Shared.NextDouble() * (float)Math.PI * 2.0D;
            }
        }

        double angleDiff;
        for (angleDiff = targetAngle - _angle; angleDiff < -Math.PI; angleDiff += Math.PI * 2.0D)
        {
            ;
        }

        while (angleDiff >= Math.PI)
        {
            angleDiff -= Math.PI * 2.0D;
        }

        if (angleDiff < -1.0D)
        {
            angleDiff = -1.0D;
        }

        if (angleDiff > 1.0D)
        {
            angleDiff = 1.0D;
        }

        _angleDelta += angleDiff * 0.1D;
        _angleDelta *= 0.8D;
        _angle += _angleDelta;

        double sinAngle = Math.Sin(_angle);
        double cosAngle = Math.Cos(_angle);

        float center = (_resolution - 1) / 2.0f;
        float needleScale = _resolution / 16.0f;

        for (int offset = -Math.Max(1, _resolution / 4); offset <= Math.Max(1, _resolution / 4); ++offset)
        {
            int pixelX = (int)(center + 0.5f + cosAngle * offset * 0.3D * needleScale);
            int pixelY = (int)(center - 0.5f - sinAngle * offset * 0.3D * 0.5D * needleScale);

            if (pixelX < 0 || pixelX >= _resolution || pixelY < 0 || pixelY >= _resolution)
            {
                continue;
            }

            int pixelIdx = pixelY * _resolution + pixelX;
            Pixels[pixelIdx * 4 + 0] = 100; // R
            Pixels[pixelIdx * 4 + 1] = 100; // G
            Pixels[pixelIdx * 4 + 2] = 100; // B
            Pixels[pixelIdx * 4 + 3] = 255; // A
        }

        for (int offset = -Math.Max(1, _resolution / 2); offset <= _resolution; ++offset)
        {
            int pixelX = (int)(center + 0.5f + sinAngle * offset * 0.3D * needleScale);
            int pixelY = (int)(center - 0.5f + cosAngle * offset * 0.3D * 0.5D * needleScale);

            if (pixelX < 0 || pixelX >= _resolution || pixelY < 0 || pixelY >= _resolution)
            {
                continue;
            }

            int pixelIdx = pixelY * _resolution + pixelX;
            bool isPointyEnd = offset >= 0;

            Pixels[pixelIdx * 4 + 0] = (byte)(isPointyEnd ? 255 : 100);
            Pixels[pixelIdx * 4 + 1] = (byte)(isPointyEnd ? 20 : 100);
            Pixels[pixelIdx * 4 + 2] = (byte)(isPointyEnd ? 20 : 100);
            Pixels[pixelIdx * 4 + 3] = 255;
        }
    }
}
