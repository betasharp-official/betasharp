using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace BetaSharp.Client.Rendering.Core.Textures;

public class DynamicTexture(int iconIdx)
{
    public byte[] Pixels = new byte[1024];
    public readonly int Sprite = iconIdx;
    public int Replicate = 1;
    public FxImage Atlas = FxImage.Terrain;

    protected byte[][]? CustomFrames;
    protected int CustomFrameIndex;
    protected int CustomFrameCount;

    public enum FxImage
    {
        Terrain,
        Items
    }

    public virtual void Setup(BetaSharp game)
    {
    }

    public virtual void tick()
    {
    }

    protected virtual void TryLoadCustomTexture(BetaSharp game, string resourceName)
    {
        CustomFrames = null;
        CustomFrameIndex = 0;
        CustomFrameCount = 0;

        using Stream? stream = game.texturePackList.SelectedTexturePack.GetResourceAsStream(resourceName);
        if (stream == null)
        {
            if (Pixels.Length != 1024) Pixels = new byte[1024];
            return;
        }

        try
        {
            string atlasPath = Atlas == FxImage.Terrain ? "/terrain.png" : "/gui/items.png";
            int targetWidth = game.textureManager.GetTextureId(atlasPath).Texture?.Width ?? 256;
            int targetTileSize = targetWidth / 16;

            if (targetTileSize < 1) targetTileSize = 1;

            using Image<Rgba32> image = Image.Load<Rgba32>(stream);
            int width = image.Width;
            int height = image.Height;

            if (height % width != 0) return;

            CustomFrameCount = height / width;

            if (width != targetTileSize)
            {
                image.Mutate(x => x.Resize(new ResizeOptions { Size = new Size(targetTileSize, targetTileSize * CustomFrameCount), Sampler = KnownResamplers.NearestNeighbor }));
                width = image.Width;
                height = image.Height;
            }

            CustomFrames = new byte[CustomFrameCount][];

            int pixelsPerFrame = width * height;
            int bytesPerFrame = pixelsPerFrame * 4;

            if (Pixels.Length != bytesPerFrame)
            {
                Pixels = new byte[bytesPerFrame];
            }

            for (int i = 0; i < CustomFrameCount; i++)
            {
                CustomFrames[i] = new byte[bytesPerFrame];
                int currentFrameIndex = i;

                using Image<Rgba32> frame = image.Clone(ctx => ctx.Crop(new Rectangle(0, currentFrameIndex * width, width, width)));
                frame.CopyPixelDataTo(CustomFrames[i]);
            }
        }
        catch (Exception)
        {
            CustomFrames = null;
            if (Pixels.Length != 1024) Pixels = new byte[1024];
        }
    }
}
