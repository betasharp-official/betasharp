using BetaSharp.Client.Rendering.Core.Textures;
using BetaSharp.Items;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace BetaSharp.Client.DynamicTexture;

internal class ClockSprite : Rendering.Core.Textures.DynamicTexture
{
    private readonly ILogger<ClockSprite> _logger = Log.Instance.For<ClockSprite>();

    private double _angle;
    private double _angleDelta;
    private int[] _clock = new int[256];
    private int[] _dial = new int[256];
    private int _dialResolution = 16;
    private BetaSharp _game;
    private int _resolution = 16;

    public ClockSprite(BetaSharp game) : base(Item.Clock.getTextureId(0))
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
        if (_clock.Length != pixelCount)
        {
            _clock = new int[pixelCount];
            _dial = new int[pixelCount];
            Pixels = new byte[pixelCount * 4];
        }

        try
        {
            using Stream? stream = game.texturePackList.SelectedTexturePack.GetResourceAsStream("gui/items.png");
            if (stream != null)
            {
                using Image<Rgba32> atlasImage = Image.Load<Rgba32>(stream);
                int atlasResolution = atlasImage.Width / 16;
                int sourceX = (Sprite % 16) * atlasResolution;
                int sourceY = (Sprite / 16) * atlasResolution;

                for (int y = 0; y < _resolution; y++)
                {
                    for (int x = 0; x < _resolution; x++)
                    {
                        int srcX = sourceX + (x * atlasResolution / _resolution);
                        int srcY = sourceY + (y * atlasResolution / _resolution);

                        Rgba32 pixel = atlasImage[srcX, srcY];
                        _clock[y * _resolution + x] = (pixel.A << 24) | (pixel.R << 16) | (pixel.G << 8) | pixel.B;
                    }
                }
            }

            using Stream? dialStream = game.texturePackList.SelectedTexturePack.GetResourceAsStream("misc/dial.png");
            if (dialStream != null)
            {
                using Image<Rgba32> dialImage = Image.Load<Rgba32>(dialStream);
                _dialResolution = dialImage.Width;
                int dialPixelCount = _dialResolution * _dialResolution;

                if (_dial.Length != dialPixelCount)
                {
                    _dial = new int[dialPixelCount];
                }

                for (int y = 0; y < _dialResolution; y++)
                {
                    for (int x = 0; x < _dialResolution; x++)
                    {
                        Rgba32 pixel = dialImage[x, y];
                        _dial[y * _dialResolution + x] = (pixel.A << 24) | (pixel.R << 16) | (pixel.G << 8) | pixel.B;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading clock sprite");
        }
    }

    public override void tick()
    {
        double targetAngle = 0.0D;
        if (_game.world != null && _game.player != null)
        {
            float worldTime = _game.world.getTime(1.0F);
            targetAngle = -worldTime * (float)Math.PI * 2.0F;
            if (_game.world.dimension.IsNether)
            {
                targetAngle = Random.Shared.NextDouble() * (float)Math.PI * 2.0D;
            }
        }

        double angleDifference = Math.Atan2(Math.Sin(targetAngle - _angle), Math.Cos(targetAngle - _angle));

        while (angleDifference >= Math.PI)
        {
            angleDifference -= Math.PI * 2.0D;
        }

        if (angleDifference < -1.0D)
        {
            angleDifference = -1.0D;
        }

        if (angleDifference > 1.0D)
        {
            angleDifference = 1.0D;
        }

        _angleDelta += angleDifference * 0.1D;
        _angleDelta *= 0.8D;
        _angle += _angleDelta;

        double sinAngle = Math.Sin(_angle);
        double cosAngle = Math.Cos(_angle);

        int pixelCount = _resolution * _resolution;
        float invResMinus1 = 1.0f / (_resolution - 1);

        for (int pixelIdx = 0; pixelIdx < pixelCount; ++pixelIdx)
        {
            int alpha = (_clock[pixelIdx] >> 24) & 255;
            int red = (_clock[pixelIdx] >> 16) & 255;
            int green = (_clock[pixelIdx] >> 8) & 255;
            int blue = (_clock[pixelIdx] >> 0) & 255;

            // Logic to detect the "clock face" area (looks for specific bluish-gray tint)
            if (Math.Abs(red - blue) < 10 && green < 40 && red > 100)
            {
                double relX = -(pixelIdx % _resolution * invResMinus1 - 0.5D);
                double relY = pixelIdx / _resolution * invResMinus1 - 0.5D;
                int origRed = red;

                int dialX = (int)((relX * cosAngle + relY * sinAngle + 0.5D) * _dialResolution);
                int dialY = (int)((relY * cosAngle - relX * sinAngle + 0.5D) * _dialResolution);

                int dialIdx = (dialX & (_dialResolution - 1)) + (dialY & (_dialResolution - 1)) * _dialResolution;

                alpha = (_dial[dialIdx] >> 24) & 255;
                red = ((_dial[dialIdx] >> 16) & 255) * red / 255;
                green = ((_dial[dialIdx] >> 8) & 255) * origRed / 255;
                blue = ((_dial[dialIdx] >> 0) & 255) * origRed / 255;
            }

            Pixels[pixelIdx * 4 + 0] = (byte)red;
            Pixels[pixelIdx * 4 + 1] = (byte)green;
            Pixels[pixelIdx * 4 + 2] = (byte)blue;
            Pixels[pixelIdx * 4 + 3] = (byte)alpha;
        }
    }
}
