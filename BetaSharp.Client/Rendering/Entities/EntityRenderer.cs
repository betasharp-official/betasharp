using BetaSharp.Blocks;
using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Core.Textures;
using BetaSharp.Entities;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Client.Rendering.Entities;

public abstract class EntityRenderer
{
    public IEntityRenderDispatcher Dispatcher { get; set; } = null!;
    protected float ShadowRadius = 0.0F;
    protected float ShadowStrength = 1.0F;
    protected ISceneRenderBackend Scene => Dispatcher.SceneRenderBackend;

    protected World World => Dispatcher.World;
    public ITextRenderer TextRenderer => Dispatcher.GetTextRenderer();

    public abstract void Render(Entity target, double x, double y, double z, float yaw, float tickDelta);

    protected void loadTexture(string path)
    {
        ITextureManager? textureManager = Dispatcher.TextureManager;
        textureManager?.BindTexture(textureManager.GetTextureId(path));
    }

    protected bool LoadDownloadableImageTexture(string? url, string fallbackPath)
    {
        if (!string.IsNullOrEmpty(url))
        {
            TextureHandle? skinHandle = Dispatcher.SkinManager?.GetTextureHandle(url);
            if (skinHandle != null)
            {
                skinHandle.Bind();
                return true;
            }
        }

        if (string.IsNullOrEmpty(fallbackPath)) return false;

        loadTexture(fallbackPath);
        return true;
    }

    private void RenderOnFire(Entity ent, Vec3D pos, float tickDelta)
    {
        Scene.Disable(SceneRenderCapability.Lighting);

        int textureId = Block.Fire.TextureId;
        int texX = (textureId & 15) << 4;
        int texY = textureId & 240;

        float minU;
        float maxU;
        float minV;
        float maxV;

        Scene.PushMatrix();
        Scene.Translate((float)pos.x, (float)pos.y, (float)pos.z);

        float scale = ent.width * 1.4F;
        Scene.Scale(scale, scale, scale);

        loadTexture("/terrain.png");
        Tessellator tess = Tessellator.instance;

        float widthOffset = 0.5F;
        float depthOffset = 0.0F;
        float heightRatio = ent.height / scale;
        float yOffset = (float)(ent.y - ent.boundingBox.MinY);

        Scene.Rotate(-Dispatcher.PlayerViewY, 0.0F, 1.0F, 0.0F);
        Scene.Translate(0.0F, 0.0F, -0.3F + (int)heightRatio * 0.02F);
        Scene.SetColor(1.0F, 1.0F, 1.0F, 1.0F);

        float zOffset = 0.0F;
        int pass = 0;

        tess.startDrawingQuads();

        while (heightRatio > 0.0F)
        {
            if (pass % 2 == 0)
            {
                minU = texX / 256.0F;
                maxU = (texX + 15.99F) / 256.0F;
                minV = texY / 256.0F;
                maxV = (texY + 15.99F) / 256.0F;
            }
            else
            {
                minU = texX / 256.0F;
                maxU = (texX + 15.99F) / 256.0F;
                minV = (texY + 16) / 256.0F;
                maxV = (texY + 16 + 15.99F) / 256.0F;
            }

            if (pass / 2 % 2 == 0)
            {
                (maxU, minU) = (minU, maxU);
            }

            tess.addVertexWithUV(widthOffset - depthOffset, 0.0F - yOffset, zOffset, maxU, maxV);
            tess.addVertexWithUV(-widthOffset - depthOffset, 0.0F - yOffset, zOffset, minU, maxV);
            tess.addVertexWithUV(-widthOffset - depthOffset, 1.4F - yOffset, zOffset, minU, minV);
            tess.addVertexWithUV(widthOffset - depthOffset, 1.4F - yOffset, zOffset, maxU, minV);

            heightRatio -= 0.45F;
            yOffset -= 0.45F;
            widthOffset *= 0.9F;
            zOffset += 0.03F;
            ++pass;
        }

        tess.draw();
        Scene.PopMatrix();
        Scene.Enable(SceneRenderCapability.Lighting);
    }

    private void RenderShadow(Entity target, Vec3D pos, float shadowiness, float tickDelta)
    {
        Scene.Enable(SceneRenderCapability.Blend);
        Scene.SetBlendFunction(SceneBlendFactor.SrcAlpha, SceneBlendFactor.OneMinusSrcAlpha);

        ITextureManager textureManager = Dispatcher.TextureManager;
        textureManager.BindTexture(textureManager.GetTextureId("%clamp%/misc/shadow.png"));

        Scene.SetDepthMask(false);
        float radius = ShadowRadius;

        double targetX = target.lastTickX + (target.x - target.lastTickX) * tickDelta;
        double targetY = target.lastTickY + (target.y - target.lastTickY) * tickDelta + target.getShadowRadius();
        double targetZ = target.lastTickZ + (target.z - target.lastTickZ) * tickDelta;

        int minX = MathHelper.Floor(targetX - radius);
        int maxX = MathHelper.Floor(targetX + radius);
        int minY = MathHelper.Floor(targetY - radius);
        int maxY = MathHelper.Floor(targetY);
        int minZ = MathHelper.Floor(targetZ - radius);
        int maxZ = MathHelper.Floor(targetZ + radius);

        double dx = pos.x - targetX;
        double dy = pos.y - targetY;
        double dz = pos.z - targetZ;

        Tessellator tess = Tessellator.instance;
        tess.startDrawingQuads();

        for (int blockX = minX; blockX <= maxX; ++blockX)
        {
            for (int blockY = minY; blockY <= maxY; ++blockY)
            {
                for (int blockZ = minZ; blockZ <= maxZ; ++blockZ)
                {
                    int blockId = World.Reader.GetBlockId(blockX, blockY - 1, blockZ);
                    if (blockId > 0 && World.Lighting.GetLightLevel(blockX, blockY, blockZ) > 3)
                    {
                        renderShadowOnBlock(
                            Block.Blocks[blockId],
                            new Vec3D(pos.x, pos.y + target.getShadowRadius(), pos.z),
                            blockX, blockY, blockZ,
                            shadowiness,
                            radius,
                            new Vec3D(dx, dy + target.getShadowRadius(), dz)
                        );
                    }
                }
            }
        }

        tess.draw();
        Scene.SetColor(1.0F, 1.0F, 1.0F, 1.0F);
        Scene.Disable(SceneRenderCapability.Blend);
        Scene.SetDepthMask(true);
    }

    private void renderShadowOnBlock(Block block, Vec3D pos, int blockX, int blockY, int blockZ, float shadowiness, float radius, Vec3D offset)
    {
        if (!block.isFullCube()) return;

        double shadowDarkness = (shadowiness - (pos.y - (blockY + offset.y)) / 2.0D) * 0.5D * World.GetLuminance(blockX, blockY, blockZ);

        if (shadowDarkness < 0.0D) return;

        if (shadowDarkness > 1.0D)
            shadowDarkness = 1.0D;

        Tessellator tess = Tessellator.instance;
        tess.setColorRGBA_F(1.0F, 1.0F, 1.0F, (float)shadowDarkness);

        double minX = blockX + block.BoundingBox.MinX + offset.x;
        double maxX = blockX + block.BoundingBox.MaxX + offset.x;
        double minY = blockY + block.BoundingBox.MinY + offset.y + 1.0D / 64.0D;
        double minZ = blockZ + block.BoundingBox.MinZ + offset.z;
        double maxZ = blockZ + block.BoundingBox.MaxZ + offset.z;

        float minU = (float)((pos.x - minX) / 2.0D / (double)radius + 0.5D);
        float maxU = (float)((pos.x - maxX) / 2.0D / (double)radius + 0.5D);
        float minV = (float)((pos.z - minZ) / 2.0D / (double)radius + 0.5D);
        float maxV = (float)((pos.z - maxZ) / 2.0D / (double)radius + 0.5D);

        tess.addVertexWithUV(minX, minY, minZ, (double)minU, (double)minV);
        tess.addVertexWithUV(minX, minY, maxZ, (double)minU, (double)maxV);
        tess.addVertexWithUV(maxX, minY, maxZ, (double)maxU, (double)maxV);
        tess.addVertexWithUV(maxX, minY, minZ, (double)maxU, (double)minV);
    }

    protected void renderShape(Box aabb, Vec3D pos)
    {
        Scene.Disable(SceneRenderCapability.Texture2D);
        Tessellator tess = Tessellator.instance;
        Scene.SetColor(1.0F, 1.0F, 1.0F, 1.0F);

        tess.startDrawingQuads();
        tess.setTranslationD(pos.x, pos.y, pos.z);

        tess.setNormal(0.0F, 0.0F, -1.0F);

        tess.addVertex(aabb.MinX, aabb.MaxY, aabb.MinZ);
        tess.addVertex(aabb.MaxX, aabb.MaxY, aabb.MinZ);
        tess.addVertex(aabb.MaxX, aabb.MinY, aabb.MinZ);
        tess.addVertex(aabb.MinX, aabb.MinY, aabb.MinZ);

        tess.setNormal(0.0F, 0.0F, 1.0F);
        tess.addVertex(aabb.MinX, aabb.MinY, aabb.MaxZ);
        tess.addVertex(aabb.MaxX, aabb.MinY, aabb.MaxZ);
        tess.addVertex(aabb.MaxX, aabb.MaxY, aabb.MaxZ);
        tess.addVertex(aabb.MinX, aabb.MaxY, aabb.MaxZ);

        tess.setNormal(0.0F, -1.0F, 0.0F);
        tess.addVertex(aabb.MinX, aabb.MinY, aabb.MinZ);
        tess.addVertex(aabb.MaxX, aabb.MinY, aabb.MinZ);
        tess.addVertex(aabb.MaxX, aabb.MinY, aabb.MaxZ);
        tess.addVertex(aabb.MinX, aabb.MinY, aabb.MaxZ);

        tess.setNormal(0.0F, 1.0F, 0.0F);
        tess.addVertex(aabb.MinX, aabb.MaxY, aabb.MaxZ);
        tess.addVertex(aabb.MaxX, aabb.MaxY, aabb.MaxZ);
        tess.addVertex(aabb.MaxX, aabb.MaxY, aabb.MinZ);
        tess.addVertex(aabb.MinX, aabb.MaxY, aabb.MinZ);

        tess.setNormal(-1.0F, 0.0F, 0.0F);
        tess.addVertex(aabb.MinX, aabb.MinY, aabb.MaxZ);
        tess.addVertex(aabb.MinX, aabb.MaxY, aabb.MaxZ);
        tess.addVertex(aabb.MinX, aabb.MaxY, aabb.MinZ);
        tess.addVertex(aabb.MinX, aabb.MinY, aabb.MinZ);

        tess.setNormal(1.0F, 0.0F, 0.0F);
        tess.addVertex(aabb.MaxX, aabb.MinY, aabb.MinZ);
        tess.addVertex(aabb.MaxX, aabb.MaxY, aabb.MinZ);
        tess.addVertex(aabb.MaxX, aabb.MaxY, aabb.MaxZ);
        tess.addVertex(aabb.MaxX, aabb.MinY, aabb.MaxZ);

        tess.setTranslationD(0.0D, 0.0D, 0.0D);
        tess.draw();
        Scene.Enable(SceneRenderCapability.Texture2D);
    }

    public static void renderShapeFlat(Box aabb)
    {
        Tessellator tess = Tessellator.instance;
        tess.startDrawingQuads();

        tess.addVertex(aabb.MinX, aabb.MaxY, aabb.MinZ);
        tess.addVertex(aabb.MaxX, aabb.MaxY, aabb.MinZ);
        tess.addVertex(aabb.MaxX, aabb.MinY, aabb.MinZ);
        tess.addVertex(aabb.MinX, aabb.MinY, aabb.MinZ);

        tess.addVertex(aabb.MinX, aabb.MinY, aabb.MaxZ);
        tess.addVertex(aabb.MaxX, aabb.MinY, aabb.MaxZ);
        tess.addVertex(aabb.MaxX, aabb.MaxY, aabb.MaxZ);
        tess.addVertex(aabb.MinX, aabb.MaxY, aabb.MaxZ);

        tess.addVertex(aabb.MinX, aabb.MinY, aabb.MinZ);
        tess.addVertex(aabb.MaxX, aabb.MinY, aabb.MinZ);
        tess.addVertex(aabb.MaxX, aabb.MinY, aabb.MaxZ);
        tess.addVertex(aabb.MinX, aabb.MinY, aabb.MaxZ);

        tess.addVertex(aabb.MinX, aabb.MaxY, aabb.MaxZ);
        tess.addVertex(aabb.MaxX, aabb.MaxY, aabb.MaxZ);
        tess.addVertex(aabb.MaxX, aabb.MaxY, aabb.MinZ);
        tess.addVertex(aabb.MinX, aabb.MaxY, aabb.MinZ);

        tess.addVertex(aabb.MinX, aabb.MinY, aabb.MaxZ);
        tess.addVertex(aabb.MinX, aabb.MaxY, aabb.MaxZ);
        tess.addVertex(aabb.MinX, aabb.MaxY, aabb.MinZ);
        tess.addVertex(aabb.MinX, aabb.MinY, aabb.MinZ);

        tess.addVertex(aabb.MaxX, aabb.MinY, aabb.MinZ);
        tess.addVertex(aabb.MaxX, aabb.MaxY, aabb.MinZ);
        tess.addVertex(aabb.MaxX, aabb.MaxY, aabb.MaxZ);
        tess.addVertex(aabb.MaxX, aabb.MinY, aabb.MaxZ);

        tess.draw();
    }

    public void PostRender(Entity target, Vec3D pos, float yaw, float tickDelta)
    {
        if (ShadowRadius > 0.0F)
        {
            double distance = Dispatcher.GetSquareDistanceTo(target.x, target.y, target.z);
            float shadowiness = (float)((1.0D - distance / 256.0D) * ShadowStrength);
            if (shadowiness > 0.0F)
            {
                RenderShadow(target, pos, shadowiness, tickDelta);
            }
        }

        if (target.isOnFire())
        {
            RenderOnFire(target, pos, tickDelta);
        }

    }

    public void RenderBoundingBox(Entity target, Vec3D pos, float yaw, float tickDelta)
    {
        if (!Dispatcher.Options.ShowDebugInfo) return;

        Scene.Disable(SceneRenderCapability.Lighting);
        Scene.Disable(SceneRenderCapability.Texture2D);
        Scene.PushMatrix();
        Scene.Translate((float)pos.x, (float)pos.y, (float)pos.z);
        Scene.SetColor(1.0F, 1.0F, 1.0F, 1.0F);

        Box bb = target.boundingBox;
        double minX = bb.MinX - target.x;
        double maxX = bb.MaxX - target.x;
        double minY = bb.MinY - target.y;
        double maxY = bb.MaxY - target.y;
        double minZ = bb.MinZ - target.z;
        double maxZ = bb.MaxZ - target.z;

        Tessellator tess = Tessellator.instance;
        tess.startDrawing(1);

        tess.addVertex(minX, minY, minZ);
        tess.addVertex(maxX, minY, minZ);
        tess.addVertex(maxX, minY, minZ);
        tess.addVertex(maxX, minY, maxZ);
        tess.addVertex(maxX, minY, maxZ);
        tess.addVertex(minX, minY, maxZ);
        tess.addVertex(minX, minY, maxZ);
        tess.addVertex(minX, minY, minZ);

        tess.addVertex(minX, maxY, minZ);
        tess.addVertex(maxX, maxY, minZ);
        tess.addVertex(maxX, maxY, minZ);
        tess.addVertex(maxX, maxY, maxZ);
        tess.addVertex(maxX, maxY, maxZ);
        tess.addVertex(minX, maxY, maxZ);
        tess.addVertex(minX, maxY, maxZ);
        tess.addVertex(minX, maxY, minZ);

        tess.addVertex(minX, minY, minZ);
        tess.addVertex(minX, maxY, minZ);
        tess.addVertex(maxX, minY, minZ);
        tess.addVertex(maxX, maxY, minZ);
        tess.addVertex(maxX, minY, maxZ);
        tess.addVertex(maxX, maxY, maxZ);
        tess.addVertex(minX, minY, maxZ);
        tess.addVertex(minX, maxY, maxZ);

        tess.draw();
        Scene.PopMatrix();
        Scene.Enable(SceneRenderCapability.Texture2D);
        Scene.Enable(SceneRenderCapability.Lighting);
    }
}
