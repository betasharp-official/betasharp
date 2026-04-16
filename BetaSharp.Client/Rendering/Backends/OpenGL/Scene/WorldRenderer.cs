using BetaSharp.Blocks;
using BetaSharp.Blocks.Entities;
using BetaSharp.Client.Entities.FX;
using BetaSharp.Client.Options;
using BetaSharp.Client.Rendering.Blocks;
using BetaSharp.Client.Rendering.Blocks.Entities;
using BetaSharp.Client.Rendering.Chunks;
using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Core.Textures;
using BetaSharp.Client.Rendering.Entities;
using BetaSharp.Client.Rendering.Legacy;
using BetaSharp.Client.Rendering.Particles;
using BetaSharp.Entities;
using BetaSharp.Items;
using BetaSharp.Profiling;
using BetaSharp.Util;
using BetaSharp.Util.Hit;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Chunks;
using BetaSharp.Worlds.Core;
using Silk.NET.Maths;

namespace BetaSharp.Client.Rendering;

public class WorldRenderer : IWorldEventListener, IWorldRenderer
{
    public int CountEntitiesTotal { get; private set; }
    public int CountEntitiesRendered { get; private set; }
    public int CountEntitiesHidden { get; private set; }
    public float DamagePartialTime { get; set; }

    private IChunkRenderer? _chunkRenderer;
    private World? _world;
    private readonly IChunkRendererFactory _chunkRendererFactory;
    private readonly ITextureManager _textureManager;
    private readonly BetaSharp _game;
    private readonly ILegacyFixedFunctionApi _sceneRenderBackend;
    private int _cloudOffsetX;
    private readonly int _starGLCallList;
    private readonly int _glSkyList;
    private readonly int _glSkyList2;
    private int _glCloudsList = -1;
    private int _renderDistance = -1;
    private int _renderEntitiesStartupCounter = 2;

    public WorldRenderer(BetaSharp gameInstance, ITextureManager textureManager,
        IChunkRendererFactory chunkRendererFactory)
    {
        _game = gameInstance;
        _sceneRenderBackend = gameInstance.LegacyFixedFunctionApi;
        _textureManager = textureManager;
        _chunkRendererFactory = chunkRendererFactory;

        _starGLCallList = _sceneRenderBackend.GenerateDisplayLists(3);
        _sceneRenderBackend.PushMatrix();
        _sceneRenderBackend.BeginDisplayList(_starGLCallList);
        RenderStars();
        _sceneRenderBackend.EndDisplayList();
        _sceneRenderBackend.PopMatrix();
        Tessellator var4 = Tessellator.instance;
        _glSkyList = _starGLCallList + 1;
        _sceneRenderBackend.BeginDisplayList(_glSkyList);
        byte var6 = 64;
        int var7 = 256 / var6 + 2;
        float var5 = 16.0F;

        int var8;
        int var9;
        for (var8 = -var6 * var7; var8 <= var6 * var7; var8 += var6)
        {
            for (var9 = -var6 * var7; var9 <= var6 * var7; var9 += var6)
            {
                var4.startDrawingQuads();
                var4.addVertex(var8 + 0, (double)var5, var9 + 0);
                var4.addVertex(var8 + var6, (double)var5, var9 + 0);
                var4.addVertex(var8 + var6, (double)var5, var9 + var6);
                var4.addVertex(var8 + 0, (double)var5, var9 + var6);
                var4.draw();
            }
        }

        _sceneRenderBackend.EndDisplayList();
        _glSkyList2 = _starGLCallList + 2;
        _sceneRenderBackend.BeginDisplayList(_glSkyList2);
        var5 = -16.0F;
        var4.startDrawingQuads();

        for (var8 = -var6 * var7; var8 <= var6 * var7; var8 += var6)
        {
            for (var9 = -var6 * var7; var9 <= var6 * var7; var9 += var6)
            {
                var4.addVertex(var8 + var6, (double)var5, var9 + 0);
                var4.addVertex(var8 + 0, (double)var5, var9 + 0);
                var4.addVertex(var8 + 0, (double)var5, var9 + var6);
                var4.addVertex(var8 + var6, (double)var5, var9 + var6);
            }
        }

        var4.draw();
        _sceneRenderBackend.EndDisplayList();
        BuildCloudDisplayLists();
    }

    private static void RenderStars()
    {
        Random random = new(10842);
        Tessellator tessellator = Tessellator.instance;
        tessellator.startDrawingQuads();

        for (int var3 = 0; var3 < 1500; ++var3)
        {
            double var4 = (double)(random.NextDouble() * 2.0 - 1.0);
            double var6 = (double)(random.NextDouble() * 2.0 - 1.0);
            double var8 = (double)(random.NextDouble() * 2.0 - 1.0);
            double var10 = (double)(0.25 + random.NextDouble() * 0.25);
            double var12 = var4 * var4 + var6 * var6 + var8 * var8;
            if (var12 < 1.0 && var12 > 0.01)
            {
                var12 = 1.0 / Math.Sqrt(var12);
                var4 *= var12;
                var6 *= var12;
                var8 *= var12;
                double var14 = var4 * 100.0;
                double var16 = var6 * 100.0;
                double var18 = var8 * 100.0;
                double var20 = Math.Atan2(var4, var8);
                double var22 = Math.Sin(var20);
                double var24 = Math.Cos(var20);
                double var26 = Math.Atan2(Math.Sqrt(var4 * var4 + var8 * var8), var6);
                double var28 = Math.Sin(var26);
                double var30 = Math.Cos(var26);
                double var32 = random.NextDouble() * Math.PI * 2.0;
                double var34 = Math.Sin(var32);
                double var36 = Math.Cos(var32);

                for (int var38 = 0; var38 < 4; ++var38)
                {
                    double var39 = 0.0D;
                    double var41 = ((var38 & 2) - 1) * var10;
                    double var43 = ((var38 + 1 & 2) - 1) * var10;
                    double var47 = var41 * var36 - var43 * var34;
                    double var49 = var43 * var36 + var41 * var34;
                    double var53 = var47 * var28 + var39 * var30;
                    double var55 = var39 * var28 - var47 * var30;
                    double var57 = var55 * var22 - var49 * var24;
                    double var61 = var49 * var22 + var55 * var24;
                    tessellator.addVertex(var14 + var57, var16 + var53, var18 + var61);
                }
            }
        }

        tessellator.draw();
    }

    public void ChangeWorld(World world)
    {
        _world?.EventListeners.Remove(this);

        _game.EntityRenderDispatcher.World = world;
        _world = world;
        if (world != null)
        {
            world.EventListeners.Add(this);
            LoadRenderers();
        }
        else
        {
            _chunkRenderer?.Dispose();
            _chunkRenderer = null;
        }
    }

    public void Tick(Entity view, float partialTicks)
    {
        if (view == null)
        {
            return;
        }

        double viewX = view.lastTickX + (view.x - view.lastTickX) * partialTicks;
        double viewY = view.lastTickY + (view.y - view.lastTickY) * partialTicks;
        double viewZ = view.lastTickZ + (view.z - view.lastTickZ) * partialTicks;
        _chunkRenderer?.Tick(new(viewX, viewY, viewZ));
    }

    public void LoadRenderers()
    {
        Block.Leaves.setGraphicsLevel(true);
        _renderDistance = _game.Options.renderDistance;

        _chunkRenderer?.Dispose();
        _chunkRenderer = _chunkRendererFactory.Create(_world!, () => _game.Options.AlternateBlocksEnabled);

        _renderEntitiesStartupCounter = 2;
    }

    public bool TryGetChunkStats(out ChunkRendererStats stats)
    {
        if (_chunkRenderer == null)
        {
            stats = default;
            return false;
        }

        stats = new ChunkRendererStats(
            _chunkRenderer.TotalChunks,
            _chunkRenderer.ChunksInFrustum,
            _chunkRenderer.ChunksOccluded,
            _chunkRenderer.ChunksRendered,
            _chunkRenderer.TranslucentMeshes);
        return true;
    }

    public void MarkAllVisibleChunksDirty()
    {
        _chunkRenderer?.MarkAllVisibleChunksDirty();
    }

    public void SetChunkFogColor(float red, float green, float blue, float alpha)
    {
        _chunkRenderer?.SetFogColor(red, green, blue, alpha);
    }

    public void SetChunkFogMode(int mode)
    {
        _chunkRenderer?.SetFogMode(mode);
    }

    public void SetChunkFogDensity(float density)
    {
        _chunkRenderer?.SetFogDensity(density);
    }

    public void SetChunkFogStart(float start)
    {
        _chunkRenderer?.SetFogStart(start);
    }

    public void SetChunkFogEnd(float end)
    {
        _chunkRenderer?.SetFogEnd(end);
    }

    public void RenderEntities(Vec3D var1, ICuller culler, float var3)
    {
        if (_renderEntitiesStartupCounter > 0)
        {
            --_renderEntitiesStartupCounter;
        }
        else
        {
            _game.BlockEntityRenderDispatcher.CacheActiveRenderInfo(_world, _textureManager, _game.TextRenderer,
                _game.Camera, var3);
            _game.EntityRenderDispatcher.CacheRenderInfo(_world, _textureManager, _game.TextRenderer, _game.Camera,
                _game.Options, _sceneRenderBackend, var3);
            CountEntitiesTotal = 0;
            CountEntitiesRendered = 0;
            CountEntitiesHidden = 0;
            EntityLiving var4 = _game.Camera;
            _game.EntityRenderDispatcher.OffsetX = var4.lastTickX + (var4.x - var4.lastTickX) * (double)var3;
            _game.EntityRenderDispatcher.OffsetY = var4.lastTickY + (var4.y - var4.lastTickY) * (double)var3;
            _game.EntityRenderDispatcher.OffsetZ = var4.lastTickZ + (var4.z - var4.lastTickZ) * (double)var3;
            _game.BlockEntityRenderDispatcher.StaticPlayerX = var4.lastTickX + (var4.x - var4.lastTickX) * (double)var3;
            _game.BlockEntityRenderDispatcher.StaticPlayerY = var4.lastTickY + (var4.y - var4.lastTickY) * (double)var3;
            _game.BlockEntityRenderDispatcher.StaticPlayerZ = var4.lastTickZ + (var4.z - var4.lastTickZ) * (double)var3;
            List<Entity> var5 = _world.Entities.Entities;
            CountEntitiesTotal = var5.Count;

            int var6;
            Entity var7;
            for (var6 = 0; var6 < _world.Entities.GlobalEntities.Count; ++var6)
            {
                var7 = _world.Entities.GlobalEntities[var6];
                ++CountEntitiesRendered;
                if (var7.shouldRender(var1))
                {
                    _game.EntityRenderDispatcher.RenderEntity(var7, var3);
                }
            }

            for (var6 = 0; var6 < var5.Count; ++var6)
            {
                var7 = var5[var6];
                if (var5[var6].dead)
                {
                    if (var5[var6] is EntityLiving living)
                    {
                        if (living.deathTime >= 20)
                        {
                            var5.RemoveAt(var6--);
                            continue;
                        }
                    }
                    else
                    {
                        var5.RemoveAt(var6--);
                        continue;
                    }
                }

                if (var7.shouldRender(var1) &&
                    (var7.ignoreFrustumCheck || culler.IsBoundingBoxInFrustum(var7.boundingBox)) &&
                    (var7 != _game.Camera || _game.Options.CameraMode != EnumCameraMode.FirstPerson ||
                     _game.Camera.isSleeping()))
                {
                    int yFloor = MathHelper.Floor(var7.y);
                    if (yFloor < 0)
                    {
                        yFloor = 0;
                    }
                    else if (yFloor >= ChuckFormat.WorldHeight)
                    {
                        yFloor = ChuckFormat.WorldHeight - 1;
                    }

                    if (_world.Reader.IsPosLoaded(MathHelper.Floor(var7.x), yFloor, MathHelper.Floor(var7.z)))
                    {
                        ++CountEntitiesRendered;
                        _game.EntityRenderDispatcher.RenderEntity(var7, var3);
                    }
                }
            }

            for (var6 = 0; var6 < _world.Entities.BlockEntities.Count; ++var6)
            {
                BlockEntity entity = _world.Entities.BlockEntities[var6];
                if (!entity.isRemoved() && culler.IsBoundingBoxInFrustum(new Box(entity.X, entity.Y, entity.Z,
                        entity.X + 1, entity.Y + 1, entity.Z + 1)))
                {
                    _game.BlockEntityRenderDispatcher.RenderTileEntity(entity, var3);
                }
            }
        }
    }

    public int SortAndRender(EntityLiving entity, int pass, double partialTicks, ICuller culler)
    {
        if (_world == null || _chunkRenderer == null)
        {
            return 0;
        }

        if (_game.Options.renderDistance != _renderDistance)
        {
            LoadRenderers();
        }

        double viewX = entity.lastTickX + (entity.x - entity.lastTickX) * partialTicks;
        double viewY = entity.lastTickY + (entity.y - entity.lastTickY) * partialTicks;
        double viewZ = entity.lastTickZ + (entity.z - entity.lastTickZ) * partialTicks;

        Lighting.turnOff();

        ChunkRenderParams renderParams = new()
        {
            Camera = culler,
            ViewPos = new Vector3D<double>(viewX, viewY, viewZ),
            RenderDistance = _renderDistance,
            Ticks = _world.GetTime(),
            PartialTicks = (float)partialTicks,
            DeltaTime = _game.Timer.DeltaTime,
            EnvironmentAnimation = _game.Options.EnvironmentAnimation,
            ChunkFade = _game.Options.ChunkFade,
            RenderOccluded = false
        };

        if (pass == 0)
        {
            _chunkRenderer.Render(renderParams);
        }
        else
        {
            _chunkRenderer.RenderTransparent(renderParams);
        }

        return 0;
    }

    public void UpdateClouds()
    {
        ++_cloudOffsetX;
    }

    public void RenderSky(float var1)
    {
        if (!_game.World.Dimension.IsNether)
        {
            _sceneRenderBackend.Disable(SceneRenderCapability.Texture2D);
            Vector3D<double> var2 = _world.Environment.GetSkyColor(_game.Camera, var1);
            float var3 = (float)var2.X;
            float var4 = (float)var2.Y;
            float var5 = (float)var2.Z;
            float var7;
            float var8;

            _sceneRenderBackend.SetColorRgb(var3, var4, var5);
            Tessellator var17 = Tessellator.instance;
            _sceneRenderBackend.SetDepthMask(false);
            _sceneRenderBackend.Enable(SceneRenderCapability.Fog);
            _sceneRenderBackend.SetColorRgb(var3, var4, var5);
            _sceneRenderBackend.CallDisplayList(_glSkyList);
            _sceneRenderBackend.Disable(SceneRenderCapability.Fog);
            _sceneRenderBackend.Disable(SceneRenderCapability.AlphaTest);
            _sceneRenderBackend.Enable(SceneRenderCapability.Blend);
            _sceneRenderBackend.SetBlendFunction(SceneBlendFactor.SrcAlpha, SceneBlendFactor.OneMinusSrcAlpha);
            Lighting.turnOff();
            float[] var18 = _world.Dimension.GetBackgroundColor(_world.GetTime(var1), var1);
            float var9;
            float var10;
            float var11;
            float var12;
            if (var18 != null)
            {
                _sceneRenderBackend.Disable(SceneRenderCapability.Texture2D);
                _sceneRenderBackend.SetShadeModel(SceneShadeModel.Smooth);
                _sceneRenderBackend.PushMatrix();
                _sceneRenderBackend.Rotate(90.0F, 1.0F, 0.0F, 0.0F);
                var8 = _world.GetTime(var1);
                _sceneRenderBackend.Rotate(var8 > 0.5F ? 180.0F : 0.0F, 0.0F, 0.0F, 1.0F);
                var9 = var18[0];
                var10 = var18[1];
                var11 = var18[2];
                float var14;

                var17.startDrawing(6);
                var17.setColorRGBA_F(var9, var10, var11, var18[3]);
                var17.addVertex(0.0D, 100.0D, 0.0D);
                byte var19 = 16;
                var17.setColorRGBA_F(var18[0], var18[1], var18[2], 0.0F);

                for (int var20 = 0; var20 <= var19; ++var20)
                {
                    var14 = var20 * (float)Math.PI * 2.0F / var19;
                    float var15 = MathHelper.Sin(var14);
                    float var16 = MathHelper.Cos(var14);
                    var17.addVertex((double)(var15 * 120.0F), (double)(var16 * 120.0F),
                        (double)(-var16 * 40.0F * var18[3]));
                }

                var17.draw();
                _sceneRenderBackend.PopMatrix();
                _sceneRenderBackend.SetShadeModel(SceneShadeModel.Flat);
            }

            _sceneRenderBackend.Enable(SceneRenderCapability.Texture2D);
            _sceneRenderBackend.SetBlendFunction(SceneBlendFactor.SrcAlpha, SceneBlendFactor.One);
            _sceneRenderBackend.PushMatrix();
            var7 = 1.0F - _world.Environment.GetRainGradient(var1);
            var8 = 0.0F;
            var9 = 0.0F;
            var10 = 0.0F;
            _sceneRenderBackend.SetColor(1.0F, 1.0F, 1.0F, var7);
            _sceneRenderBackend.Translate(var8, var9, var10);
            _sceneRenderBackend.Rotate(0.0F, 0.0F, 0.0F, 1.0F);
            _sceneRenderBackend.Rotate(_world.GetTime(var1) * 360.0F, 1.0F, 0.0F, 0.0F);
            var11 = 30.0F;
            _textureManager.BindTexture(_textureManager.GetTextureId("/terrain/sun.png"));
            var17.startDrawingQuads();
            var17.addVertexWithUV((double)-var11, 100.0D, (double)-var11, 0.0D, 0.0D);
            var17.addVertexWithUV((double)var11, 100.0D, (double)-var11, 1.0D, 0.0D);
            var17.addVertexWithUV((double)var11, 100.0D, (double)var11, 1.0D, 1.0D);
            var17.addVertexWithUV((double)-var11, 100.0D, (double)var11, 0.0D, 1.0D);
            var17.draw();
            var11 = 20.0F;
            _textureManager.BindTexture(_textureManager.GetTextureId("/terrain/moon.png"));
            var17.startDrawingQuads();
            var17.addVertexWithUV((double)-var11, -100.0D, (double)var11, 1.0D, 1.0D);
            var17.addVertexWithUV((double)var11, -100.0D, (double)var11, 0.0D, 1.0D);
            var17.addVertexWithUV((double)var11, -100.0D, (double)-var11, 0.0D, 0.0D);
            var17.addVertexWithUV((double)-var11, -100.0D, (double)-var11, 1.0D, 0.0D);
            var17.draw();
            _sceneRenderBackend.Disable(SceneRenderCapability.Texture2D);
            var12 = _world.CalculateSkyLightIntensity(var1) * var7;
            if (var12 > 0.0F)
            {
                _sceneRenderBackend.SetColor(var12, var12, var12, var12);
                _sceneRenderBackend.CallDisplayList(_starGLCallList);
            }

            _sceneRenderBackend.SetColor(1.0F, 1.0F, 1.0F, 1.0F);
            _sceneRenderBackend.Disable(SceneRenderCapability.Blend);
            _sceneRenderBackend.Enable(SceneRenderCapability.AlphaTest);
            _sceneRenderBackend.Enable(SceneRenderCapability.Fog);
            _sceneRenderBackend.PopMatrix();
            if (_world.Dimension.HasGround)
            {
                _sceneRenderBackend.SetColorRgb(var3 * 0.2F + 0.04F, var4 * 0.2F + 0.04F, var5 * 0.6F + 0.1F);
            }
            else
            {
                _sceneRenderBackend.SetColorRgb(var3, var4, var5);
            }

            _sceneRenderBackend.Disable(SceneRenderCapability.Texture2D);
            _sceneRenderBackend.CallDisplayList(_glSkyList2);
            _sceneRenderBackend.Enable(SceneRenderCapability.Texture2D);
            _sceneRenderBackend.SetDepthMask(true);
        }
    }

    public void RenderClouds(float var1)
    {
        using (Profiler.Begin("RenderClouds"))
        {
            if (!_game.World.Dimension.IsNether)
            {
                RenderCloudsFancy(var1);
            }
        }
    }

    private void BuildCloudDisplayLists()
    {
        _glCloudsList = _sceneRenderBackend.GenerateDisplayLists(4);
        Tessellator tessellator = Tessellator.instance;

        for (int i = 0; i < 4; ++i)
        {
            _sceneRenderBackend.BeginDisplayList(_glCloudsList + i);
            tessellator.startDrawingQuads();
            float cloudHeight = 4.0F;
            float uvScale = 1.0F / 256.0F;
            float var24 = 1.0F / 1024.0F;
            byte var22 = 8;
            byte var23 = 3;

            for (int var26 = -var23 + 1; var26 <= var23; ++var26)
            {
                for (int var27 = -var23 + 1; var27 <= var23; ++var27)
                {
                    float var28 = var26 * var22;
                    float var29 = var27 * var22;
                    float var30 = var28;
                    float var31 = var29;

                    if (i == 0)
                    {
                        tessellator.setNormal(0.0F, -1.0F, 0.0F);
                        tessellator.addVertexWithUV((double)(var30 + 0.0F), (double)(0.0F), (double)(var31 + var22),
                            (double)((var28 + 0.0F) * uvScale), (double)((var29 + var22) * uvScale));
                        tessellator.addVertexWithUV((double)(var30 + var22), (double)(0.0F), (double)(var31 + var22),
                            (double)((var28 + var22) * uvScale), (double)((var29 + var22) * uvScale));
                        tessellator.addVertexWithUV((double)(var30 + var22), (double)(0.0F), (double)(var31 + 0.0F),
                            (double)((var28 + var22) * uvScale), (double)((var29 + 0.0F) * uvScale));
                        tessellator.addVertexWithUV((double)(var30 + 0.0F), (double)(0.0F), (double)(var31 + 0.0F),
                            (double)((var28 + 0.0F) * uvScale), (double)((var29 + 0.0F) * uvScale));
                    }

                    else if (i == 1)
                    {
                        tessellator.setNormal(0.0F, 1.0F, 0.0F);
                        tessellator.addVertexWithUV((double)(var30 + 0.0F), (double)(cloudHeight - var24),
                            (double)(var31 + var22), (double)((var28 + 0.0F) * uvScale),
                            (double)((var29 + var22) * uvScale));
                        tessellator.addVertexWithUV((double)(var30 + var22), (double)(cloudHeight - var24),
                            (double)(var31 + var22), (double)((var28 + var22) * uvScale),
                            (double)((var29 + var22) * uvScale));
                        tessellator.addVertexWithUV((double)(var30 + var22), (double)(cloudHeight - var24),
                            (double)(var31 + 0.0F), (double)((var28 + var22) * uvScale),
                            (double)((var29 + 0.0F) * uvScale));
                        tessellator.addVertexWithUV((double)(var30 + 0.0F), (double)(cloudHeight - var24),
                            (double)(var31 + 0.0F), (double)((var28 + 0.0F) * uvScale),
                            (double)((var29 + 0.0F) * uvScale));
                    }

                    else if (i == 2)
                    {
                        if (var26 > -1)
                        {
                            tessellator.setNormal(-1.0F, 0.0F, 0.0F);
                            for (int var32 = 0; var32 < var22; ++var32)
                            {
                                tessellator.addVertexWithUV((double)(var30 + var32 + 0.0F), (double)(0.0F),
                                    (double)(var31 + var22), (double)((var28 + var32 + 0.5F) * uvScale),
                                    (double)((var29 + var22) * uvScale));
                                tessellator.addVertexWithUV((double)(var30 + var32 + 0.0F), (double)(cloudHeight),
                                    (double)(var31 + var22), (double)((var28 + var32 + 0.5F) * uvScale),
                                    (double)((var29 + var22) * uvScale));
                                tessellator.addVertexWithUV((double)(var30 + var32 + 0.0F), (double)(cloudHeight),
                                    (double)(var31 + 0.0F), (double)((var28 + var32 + 0.5F) * uvScale),
                                    (double)((var29 + 0.0F) * uvScale));
                                tessellator.addVertexWithUV((double)(var30 + var32 + 0.0F), (double)(0.0F),
                                    (double)(var31 + 0.0F), (double)((var28 + var32 + 0.5F) * uvScale),
                                    (double)((var29 + 0.0F) * uvScale));
                            }
                        }

                        if (var26 <= 1)
                        {
                            tessellator.setNormal(1.0F, 0.0F, 0.0F);
                            for (int var32 = 0; var32 < var22; ++var32)
                            {
                                tessellator.addVertexWithUV((double)(var30 + var32 + 1.0F - var24), (double)(0.0F),
                                    (double)(var31 + var22), (double)((var28 + var32 + 0.5F) * uvScale),
                                    (double)((var29 + var22) * uvScale));
                                tessellator.addVertexWithUV((double)(var30 + var32 + 1.0F - var24),
                                    (double)(cloudHeight), (double)(var31 + var22),
                                    (double)((var28 + var32 + 0.5F) * uvScale), (double)((var29 + var22) * uvScale));
                                tessellator.addVertexWithUV((double)(var30 + var32 + 1.0F - var24),
                                    (double)(cloudHeight), (double)(var31 + 0.0F),
                                    (double)((var28 + var32 + 0.5F) * uvScale), (double)((var29 + 0.0F) * uvScale));
                                tessellator.addVertexWithUV((double)(var30 + var32 + 1.0F - var24), (double)(0.0F),
                                    (double)(var31 + 0.0F), (double)((var28 + var32 + 0.5F) * uvScale),
                                    (double)((var29 + 0.0F) * uvScale));
                            }
                        }
                    }

                    else if (i == 3)
                    {
                        if (var27 > -1)
                        {
                            tessellator.setNormal(0.0F, 0.0F, -1.0F);
                            for (int var32 = 0; var32 < var22; ++var32)
                            {
                                tessellator.addVertexWithUV((double)(var30 + 0.0F), (double)(cloudHeight),
                                    (double)(var31 + var32 + 0.0F), (double)((var28 + 0.0F) * uvScale),
                                    (double)((var29 + var32 + 0.5F) * uvScale));
                                tessellator.addVertexWithUV((double)(var30 + var22), (double)(cloudHeight),
                                    (double)(var31 + var32 + 0.0F), (double)((var28 + var22) * uvScale),
                                    (double)((var29 + var32 + 0.5F) * uvScale));
                                tessellator.addVertexWithUV((double)(var30 + var22), (double)(0.0F),
                                    (double)(var31 + var32 + 0.0F), (double)((var28 + var22) * uvScale),
                                    (double)((var29 + var32 + 0.5F) * uvScale));
                                tessellator.addVertexWithUV((double)(var30 + 0.0F), (double)(0.0F),
                                    (double)(var31 + var32 + 0.0F), (double)((var28 + 0.0F) * uvScale),
                                    (double)((var29 + var32 + 0.5F) * uvScale));
                            }
                        }

                        if (var27 <= 1)
                        {
                            tessellator.setNormal(0.0F, 0.0F, 1.0F);
                            for (int var32 = 0; var32 < var22; ++var32)
                            {
                                tessellator.addVertexWithUV((double)(var30 + 0.0F), (double)(cloudHeight),
                                    (double)(var31 + var32 + 1.0F - var24), (double)((var28 + 0.0F) * uvScale),
                                    (double)((var29 + var32 + 0.5F) * uvScale));
                                tessellator.addVertexWithUV((double)(var30 + var22), (double)(cloudHeight),
                                    (double)(var31 + var32 + 1.0F - var24), (double)((var28 + var22) * uvScale),
                                    (double)((var29 + var32 + 0.5F) * uvScale));
                                tessellator.addVertexWithUV((double)(var30 + var22), (double)(0.0F),
                                    (double)(var31 + var32 + 1.0F - var24), (double)((var28 + var22) * uvScale),
                                    (double)((var29 + var32 + 0.5F) * uvScale));
                                tessellator.addVertexWithUV((double)(var30 + 0.0F), (double)(0.0F),
                                    (double)(var31 + var32 + 1.0F - var24), (double)((var28 + 0.0F) * uvScale),
                                    (double)((var29 + var32 + 0.5F) * uvScale));
                            }
                        }
                    }
                }
            }

            tessellator.draw();
            _sceneRenderBackend.EndDisplayList();
        }
    }

    private void RenderCloudsFancy(float var1)
    {
        _sceneRenderBackend.Disable(SceneRenderCapability.CullFace);
        float var2 = (float)(_game.Camera.lastTickY + (_game.Camera.y - _game.Camera.lastTickY) * (double)var1);
        float var4 = 12.0F;
        float var5 = 4.0F;
        double var6 =
            (_game.Camera.prevX + (_game.Camera.x - _game.Camera.prevX) * (double)var1 +
             (double)((_cloudOffsetX + var1) * 0.03F)) / (double)var4;
        double var8 = (_game.Camera.prevZ + (_game.Camera.z - _game.Camera.prevZ) * (double)var1) / (double)var4 +
                      (double)0.33F;
        float var10 = _world.Dimension.CloudHeight - var2 + 0.33F;
        int var11 = MathHelper.Floor(var6 / 2048.0D);
        int var12 = MathHelper.Floor(var8 / 2048.0D);
        var6 -= var11 * 2048;
        var8 -= var12 * 2048;
        _textureManager.BindTexture(_textureManager.GetTextureId("/environment/clouds.png"));
        _sceneRenderBackend.Enable(SceneRenderCapability.Blend);
        _sceneRenderBackend.SetBlendFunction(SceneBlendFactor.SrcAlpha, SceneBlendFactor.OneMinusSrcAlpha);
        Vector3D<double> var13 = _world.Environment.GetCloudColor(var1);
        float var14 = (float)var13.X;
        float var15 = (float)var13.Y;
        float var16 = (float)var13.Z;

        float var19 = 1 / 256f;
        float var17 = MathHelper.Floor(var6) * var19;
        float var18 = MathHelper.Floor(var8) * var19;
        float var20 = (float)(var6 - MathHelper.Floor(var6));
        float var21 = (float)(var8 - MathHelper.Floor(var8));

        _sceneRenderBackend.Scale(var4, 1.0F, var4);

        for (int var25 = 0; var25 < 2; ++var25)
        {
            if (var25 == 0)
            {
                _sceneRenderBackend.SetColorMask(false, false, false, false);
            }
            else
            {
                _sceneRenderBackend.SetColorMask(true, true, true, true);
            }

            _sceneRenderBackend.PushMatrix();
            _sceneRenderBackend.Translate(-var20, var10, -var21);

            _sceneRenderBackend.SetMatrixMode(SceneMatrixMode.Texture);
            _sceneRenderBackend.PushMatrix();
            _sceneRenderBackend.Translate(var17, var18, 0.0F);
            _sceneRenderBackend.SetMatrixMode(SceneMatrixMode.Modelview);

            if (var10 > -var5 - 1.0F)
            {
                _sceneRenderBackend.SetColor(var14 * 0.7F, var15 * 0.7F, var16 * 0.7F, 0.8F);
                _sceneRenderBackend.CallDisplayList(_glCloudsList + 0); // Bottom
            }

            if (var10 <= var5 + 1.0F)
            {
                _sceneRenderBackend.SetColor(var14, var15, var16, 0.8F);
                _sceneRenderBackend.CallDisplayList(_glCloudsList + 1); // Top
            }

            _sceneRenderBackend.SetColor(var14 * 0.9F, var15 * 0.9F, var16 * 0.9F, 0.8F);
            _sceneRenderBackend.CallDisplayList(_glCloudsList + 2); // Side X

            _sceneRenderBackend.SetColor(var14 * 0.8F, var15 * 0.8F, var16 * 0.8F, 0.8F);
            _sceneRenderBackend.CallDisplayList(_glCloudsList + 3); // Side Z

            _sceneRenderBackend.SetMatrixMode(SceneMatrixMode.Texture);
            _sceneRenderBackend.PopMatrix();
            _sceneRenderBackend.SetMatrixMode(SceneMatrixMode.Modelview);

            _sceneRenderBackend.PopMatrix();
        }

        _sceneRenderBackend.SetColor(1.0F, 1.0F, 1.0F, 1.0F);
        _sceneRenderBackend.Disable(SceneRenderCapability.Blend);
        _sceneRenderBackend.Enable(SceneRenderCapability.CullFace);
    }

    public void DrawBlockBreaking(EntityPlayer entityPlayer, HitResult hit, ItemStack? itemStack, float tickDelta)
    {
        if (DamagePartialTime <= 0.0F) return;

        Tessellator tessellator = Tessellator.instance;

        _sceneRenderBackend.PushMatrix();
        _sceneRenderBackend.Enable(SceneRenderCapability.Blend);
        _sceneRenderBackend.Enable(SceneRenderCapability.AlphaTest);
        _sceneRenderBackend.Enable(SceneRenderCapability.PolygonOffsetFill);

        _sceneRenderBackend.SetBlendFunction(SceneBlendFactor.DstColor, SceneBlendFactor.SrcColor);
        _sceneRenderBackend.SetColor(1.0F, 1.0F, 1.0F, 0.5F);
        _sceneRenderBackend.SetPolygonOffset(-3.0F, -50.0F);

        _textureManager.BindTexture(_textureManager.GetTextureId("/terrain.png"));

        int targetBlockId = _world.Reader.GetBlockId(hit.BlockX, hit.BlockY, hit.BlockZ);
        Block targetBlock = targetBlockId > 0 ? Block.Blocks[targetBlockId] : Block.Stone;

        double renderX = entityPlayer.lastTickX + (entityPlayer.x - entityPlayer.lastTickX) * (double)tickDelta;
        double renderY = entityPlayer.lastTickY + (entityPlayer.y - entityPlayer.lastTickY) * (double)tickDelta;
        double renderZ = entityPlayer.lastTickZ + (entityPlayer.z - entityPlayer.lastTickZ) * (double)tickDelta;

        tessellator.startDrawingQuads();
        tessellator.setTranslationD(-renderX, -renderY, -renderZ);
        tessellator.disableColor();

        BlockRenderer.RenderBlockByRenderType(_world.Reader, _world.Lighting, targetBlock,
            new BlockPos(hit.BlockX, hit.BlockY, hit.BlockZ), tessellator, 240 + (int)(DamagePartialTime * 10.0F), true,
            _game.Options.AlternateBlocksEnabled);
        tessellator.draw();

        tessellator.setTranslationD(0.0D, 0.0D, 0.0D);
        _sceneRenderBackend.SetPolygonOffset(0.0F, 0.0F);
        _sceneRenderBackend.SetColor(1.0F, 1.0F, 1.0F, 1.0F);
        _sceneRenderBackend.SetBlendFunction(SceneBlendFactor.SrcAlpha, SceneBlendFactor.OneMinusSrcAlpha);

        _sceneRenderBackend.Disable(SceneRenderCapability.PolygonOffsetFill);
        _sceneRenderBackend.Disable(SceneRenderCapability.AlphaTest);
        _sceneRenderBackend.Disable(SceneRenderCapability.Blend);
        _sceneRenderBackend.PopMatrix();
    }

    public void DrawSelectionBox(EntityPlayer var1, HitResult var2, int var3, ItemStack? var4, float var5)
    {
        if (var3 == 0 && var2.Type == HitResultType.TILE)
        {
            _sceneRenderBackend.Enable(SceneRenderCapability.Blend);
            _sceneRenderBackend.SetBlendFunction(SceneBlendFactor.SrcAlpha, SceneBlendFactor.OneMinusSrcAlpha);
            _sceneRenderBackend.SetColor(0.0F, 0.0F, 0.0F, 0.4F);
            _sceneRenderBackend.SetLineWidth(2.0F);
            _sceneRenderBackend.Disable(SceneRenderCapability.Texture2D);
            _sceneRenderBackend.SetDepthMask(false);
            float var6 = 0.002F;
            int var7 = _world.Reader.GetBlockId(var2.BlockX, var2.BlockY, var2.BlockZ);
            if (var7 > 0)
            {
                Block.Blocks[var7].updateBoundingBox(_world.Reader, var2.BlockX, var2.BlockY, var2.BlockZ);
                double var8 = var1.lastTickX + (var1.x - var1.lastTickX) * (double)var5;
                double var10 = var1.lastTickY + (var1.y - var1.lastTickY) * (double)var5;
                double var12 = var1.lastTickZ + (var1.z - var1.lastTickZ) * (double)var5;
                DrawOutlinedBoundingBox(Block.Blocks[var7]
                    .getBoundingBox(_world.Reader, _world.Entities, var2.BlockX, var2.BlockY, var2.BlockZ)
                    .Expand((double)var6, (double)var6, (double)var6).Offset(-var8, -var10, -var12));
            }

            _sceneRenderBackend.SetDepthMask(true);
            _sceneRenderBackend.Enable(SceneRenderCapability.Texture2D);
            _sceneRenderBackend.Disable(SceneRenderCapability.Blend);
        }
    }

    private static void DrawOutlinedBoundingBox(Box box)
    {
        Tessellator var2 = Tessellator.instance;
        var2.startDrawing(3);
        var2.addVertex(box.MinX, box.MinY, box.MinZ);
        var2.addVertex(box.MaxX, box.MinY, box.MinZ);
        var2.addVertex(box.MaxX, box.MinY, box.MaxZ);
        var2.addVertex(box.MinX, box.MinY, box.MaxZ);
        var2.addVertex(box.MinX, box.MinY, box.MinZ);
        var2.draw();
        var2.startDrawing(3);
        var2.addVertex(box.MinX, box.MaxY, box.MinZ);
        var2.addVertex(box.MaxX, box.MaxY, box.MinZ);
        var2.addVertex(box.MaxX, box.MaxY, box.MaxZ);
        var2.addVertex(box.MinX, box.MaxY, box.MaxZ);
        var2.addVertex(box.MinX, box.MaxY, box.MinZ);
        var2.draw();
        var2.startDrawing(1);
        var2.addVertex(box.MinX, box.MinY, box.MinZ);
        var2.addVertex(box.MinX, box.MaxY, box.MinZ);
        var2.addVertex(box.MaxX, box.MinY, box.MinZ);
        var2.addVertex(box.MaxX, box.MaxY, box.MinZ);
        var2.addVertex(box.MaxX, box.MinY, box.MaxZ);
        var2.addVertex(box.MaxX, box.MaxY, box.MaxZ);
        var2.addVertex(box.MinX, box.MinY, box.MaxZ);
        var2.addVertex(box.MinX, box.MaxY, box.MaxZ);
        var2.draw();
    }

    public void MarkBlocksDirty(int minX, int minY, int minZ, int maxX, int maxY, int maxZ)
    {
        if (_chunkRenderer == null)
        {
            return;
        }

        int sectionSize = _chunkRenderer.SectionSize;
        int xStart = (int)Math.Floor((double)minX / sectionSize);
        int yStart = (int)Math.Floor((double)minY / sectionSize);
        int zStart = (int)Math.Floor((double)minZ / sectionSize);
        int xEnd = (int)Math.Ceiling((double)maxX / sectionSize);
        int yEnd = (int)Math.Ceiling((double)maxY / sectionSize);
        int zEnd = (int)Math.Ceiling((double)maxZ / sectionSize);

        for (int x = xStart; x <= xEnd; x++)
        {
            for (int y = yStart; y <= yEnd; y++)
            {
                for (int z = zStart; z <= zEnd; z++)
                {
                    _chunkRenderer.MarkDirty(new Vector3D<int>(x, y, z) * sectionSize, true);
                }
            }
        }
    }

    public void BlockUpdate(int x, int y, int z)
    {
        MarkBlocksDirty(x - 1, y - 1, z - 1, x + 1, y + 1, z + 1);
    }

    public void SetBlocksDirty(int minX, int minY, int minZ, int maxX, int maxY, int maxZ)
    {
        if (!_world.BlockHost.IsRegionLoaded(minX, minY, minZ, maxX, maxY, maxZ))
        {
            return;
        }

        MarkBlocksDirty(minX - 1, minY - 1, minZ - 1, maxX + 1, maxY + 1, maxZ + 1);
    }

    public void PlayStreaming(string var1, int var2, int var3, int var4)
    {
        if (var1 != null)
        {
            _game.HUD.Chat.SetRecordPlaying(var1);
        }

        _game.SoundManager.PlayStreaming(var1, var2, var3, var4, 1.0F, 1.0F);
    }

    public void PlaySound(string var1, double var2, double var4, double var6, float var8, float var9)
    {
        float var10 = 16.0F;
        if (var8 > 1.0F)
        {
            var10 *= var8;
        }

        if (_game.Camera.getSquaredDistance(var2, var4, var6) < (double)(var10 * var10))
        {
            _game.SoundManager.PlaySound(var1, (float)var2, (float)var4, (float)var6, var8, var9);
        }
    }

    public void SpawnParticle(string var1, double var2, double var4, double var6, double var8, double var10,
        double var12)
    {
        if (_game != null && _game.Camera != null && _game.ParticleManager != null)
        {
            double var14 = _game.Camera.x - var2;
            double var16 = _game.Camera.y - var4;
            double var18 = _game.Camera.z - var6;
            double var20 = 16.0D;
            if (var14 * var14 + var16 * var16 + var18 * var18 <= var20 * var20)
            {
                IParticleManager pm = _game.ParticleManager;
                switch (var1)
                {
                    case "bubble": pm.AddBubble(var2, var4, var6, var8, var10, var12); break;
                    case "smoke": pm.AddSmoke(var2, var4, var6, var8, var10, var12); break;
                    case "note": pm.AddNote(var2, var4, var6, var8, var10, var12); break;
                    case "portal": pm.AddPortal(var2, var4, var6, var8, var10, var12); break;
                    case "explode": pm.AddExplode(var2, var4, var6, var8, var10, var12); break;
                    case "flame": pm.AddFlame(var2, var4, var6, var8, var10, var12); break;
                    case "lava": pm.AddLava(var2, var4, var6); break;
                    case "footstep": pm.AddFootstep(var2, var4, var6); break;
                    case "splash": pm.AddSplash(var2, var4, var6, var8, var10, var12); break;
                    case "largesmoke": pm.AddSmoke(var2, var4, var6, var8, var10, var12, 2.5f); break;
                    case "reddust": pm.AddReddust(var2, var4, var6, (float)var8, (float)var10, (float)var12); break;
                    case "snowballpoof": pm.AddSlime(var2, var4, var6, Item.Snowball); break;
                    case "snowshovel": pm.AddSnowShovel(var2, var4, var6, var8, var10, var12); break;
                    case "slime": pm.AddSlime(var2, var4, var6, Item.Slimeball); break;
                    case "heart": pm.AddHeart(var2, var4, var6, var8, var10, var12); break;
                }
            }
        }
    }

    public void NotifyEntityAdded(Entity var1)
    {
        var1.updateCloak();
        _game.EntityRenderDispatcher.SkinManager.RequestDownload((var1 as EntityPlayer)?.name);
    }

    public void NotifyEntityRemoved(Entity var1)
    {
    }

    public void NotifyAmbientDarknessChanged()
    {
        _chunkRenderer?.UpdateAllRenderers();
    }

    public void UpdateBlockEntity(int var1, int var2, int var3, BlockEntity var4)
    {
    }

    public void WorldEvent(EntityPlayer var1, int var2, int var3, int var4, int var5, int var6)
    {
        JavaRandom var7 = _world.Random;
        int var16;
        switch (var2)
        {
            case 1000:
                _game.SoundManager.PlaySound("random.click", var3, var4, var5, 1.0F, 1.0F);
                break;
            case 1001:
                _game.SoundManager.PlaySound("random.click", var3, var4, var5, 1.0F, 1.2F);
                break;
            case 1002:
                _game.SoundManager.PlaySound("random.bow", var3, var4, var5, 1.0F, 1.2F);
                break;
            case 1003:
                if (Random.Shared.NextDouble() < 0.5D)
                {
                    _game.SoundManager.PlaySound("random.door_open", var3 + 0.5F, var4 + 0.5F, var5 + 0.5F, 1.0F,
                        _world.Random.NextFloat() * 0.1F + 0.9F);
                }
                else
                {
                    _game.SoundManager.PlaySound("random.door_close", var3 + 0.5F, var4 + 0.5F, var5 + 0.5F, 1.0F,
                        _world.Random.NextFloat() * 0.1F + 0.9F);
                }

                break;
            case 1004:
                _game.SoundManager.PlaySound("random.fizz", var3 + 0.5F, var4 + 0.5F, var5 + 0.5F, 0.5F,
                    2.6F + (var7.NextFloat() - var7.NextFloat()) * 0.8F);
                break;
            case 1005:
                if (Item.ITEMS[var6] is ItemRecord)
                {
                    _game.SoundManager.PlayStreaming(((ItemRecord)Item.ITEMS[var6]).recordName, var3, var4, var5, 1.0F,
                        1.0F);
                }
                else
                {
                    _game.SoundManager.PlayStreaming(null, var3, var4, var5, 1.0F, 1.0F);
                }

                break;
            case 2000:
                int var8 = var6 % 3 - 1;
                int var9 = var6 / 3 % 3 - 1;
                double var10 = var3 + var8 * 0.6D + 0.5D;
                double var12 = var4 + 0.5D;
                double var14 = var5 + var9 * 0.6D + 0.5D;

                for (var16 = 0; var16 < 10; ++var16)
                {
                    double var31 = var7.NextDouble() * 0.2D + 0.01D;
                    double var19 = var10 + var8 * 0.01D + (var7.NextDouble() - 0.5D) * var9 * 0.5D;
                    double var21 = var12 + (var7.NextDouble() - 0.5D) * 0.5D;
                    double var23 = var14 + var9 * 0.01D + (var7.NextDouble() - 0.5D) * var8 * 0.5D;
                    double var25 = var8 * var31 + var7.NextGaussian() * 0.01D;
                    double var27 = -0.03D + var7.NextGaussian() * 0.01D;
                    double var29 = var9 * var31 + var7.NextGaussian() * 0.01D;
                    SpawnParticle("smoke", var19, var21, var23, var25, var27, var29);
                }

                return;
            case 2001: // This is for breaking a block
                var16 = var6 & 255;
                if (var16 > 0)
                {
                    Block blockId = Block.Blocks[var16];
                    _game.SoundManager.PlaySound(blockId.SoundGroup.BreakSound, var3 + 0.5F, var4 + 0.5F, var5 + 0.5F,
                        (blockId.SoundGroup.Volume + 1.0F) / 2.0F, blockId.SoundGroup.Pitch * 0.8F);
                }

                _game.ParticleManager.addBlockDestroyEffects(var3, var4, var5, var6 & 255, var6 >> 8 & 255);
                break;
        }
    }

    public void PlayNote(int x, int y, int z, int soundType, int pitch)
    {
    }

    public void BroadcastEntityEvent(Entity entity, byte @event)
    {
    }
}
