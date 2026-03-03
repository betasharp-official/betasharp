using BetaSharp.Blocks;
using BetaSharp.Blocks.Entities;
using BetaSharp.Client.Entities.FX;
using BetaSharp.Client.Rendering.Blocks;
using BetaSharp.Client.Rendering.Blocks.Entities;
using BetaSharp.Client.Rendering.Chunks;
using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Entities;
using BetaSharp.Client.Options;
using BetaSharp.Entities;
using BetaSharp.Items;
using BetaSharp.Profiling;
using BetaSharp.Util.Hit;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds;
using Silk.NET.Maths;
using Silk.NET.OpenGL.Legacy;
using BetaSharp.Util;
using BetaSharp.Client.Rendering.Core.Textures;

namespace BetaSharp.Client.Rendering;

public class WorldRenderer : IWorldAccess
{
    private World world;
    private readonly TextureManager renderEngine;
    private readonly Minecraft mc;
    private BlockRenderer globalRenderBlocks;
    private int cloudOffsetX;
    private readonly int starGLCallList;
    private readonly int glSkyList;
    private readonly int glSkyList2;
    private int glCloudsList = -1;
    private int renderDistance = -1;
    private int renderEntitiesStartupCounter = 2;
    private int countEntitiesTotal;
    private int countEntitiesRendered;
    private int countEntitiesHidden;
    public ChunkRenderer chunkRenderer;
    public float damagePartialTime;

    public WorldRenderer(Minecraft gameInstance, TextureManager textureManager)
    {
        mc = gameInstance;
        renderEngine = textureManager;
        byte var3 = 64;

        starGLCallList = GLAllocation.generateDisplayLists(3);
        GLManager.GL.PushMatrix();
        GLManager.GL.NewList((uint)starGLCallList, GLEnum.Compile);
        renderStars();
        GLManager.GL.EndList();
        GLManager.GL.PopMatrix();
        Tessellator var4 = Tessellator.instance;
        glSkyList = starGLCallList + 1;
        GLManager.GL.NewList((uint)glSkyList, GLEnum.Compile);
        byte var6 = 64;
        int var7 = 256 / var6 + 2;
        float var5 = 16.0F;

        chunkRenderer = new(gameInstance.world);

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

        GLManager.GL.EndList();
        glSkyList2 = starGLCallList + 2;
        GLManager.GL.NewList((uint)glSkyList2, GLEnum.Compile);
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
        GLManager.GL.EndList();
        buildCloudDisplayLists();
    }

    private void renderStars()
    {
        Random random = new(10842);
        var tessellator = Tessellator.instance;
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

    public void changeWorld(World world)
    {
        this.world?.removeWorldAccess(this);

        EntityRenderDispatcher.instance.func_852_a(world);
        this.world = world;
        globalRenderBlocks = new BlockRenderer(world);
        if (world != null)
        {
            world.addWorldAccess(this);
            loadRenderers();
        }

    }

    public void tick(Entity view, float var3)
    {
        if (view == null)
        {
            return;
        }

        double var33 = view.lastTickX + (view.x - view.lastTickX) * var3;
        double var7 = view.lastTickY + (view.y - view.lastTickY) * var3;
        double var9 = view.lastTickZ + (view.z - view.lastTickZ) * var3;
        chunkRenderer.Tick(new(var33, var7, var9));
    }

    public void loadRenderers()
    {
        Block.Leaves.setGraphicsLevel(true);
        renderDistance = mc.options.renderDistance;

        chunkRenderer?.Dispose();
        chunkRenderer = new(world);
        ChunkMeshVersion.ClearPool();

        renderEntitiesStartupCounter = 2;
    }

    public void renderEntities(Vec3D var1, Culler culler, float var3)
    {
        if (renderEntitiesStartupCounter > 0)
        {
            --renderEntitiesStartupCounter;
        }
        else
        {
            BlockEntityRenderer.Instance.CacheActiveRenderInfo(world, renderEngine, mc.fontRenderer, mc.camera, var3);
            EntityRenderDispatcher.instance.cacheActiveRenderInfo(world, renderEngine, mc.fontRenderer, mc.camera, mc.options, var3);
            countEntitiesTotal = 0;
            countEntitiesRendered = 0;
            countEntitiesHidden = 0;
            EntityLiving var4 = mc.camera;
            EntityRenderDispatcher.offsetX = var4.lastTickX + (var4.x - var4.lastTickX) * (double)var3;
            EntityRenderDispatcher.offsetY = var4.lastTickY + (var4.y - var4.lastTickY) * (double)var3;
            EntityRenderDispatcher.offsetZ = var4.lastTickZ + (var4.z - var4.lastTickZ) * (double)var3;
            BlockEntityRenderer.StaticPlayerX = var4.lastTickX + (var4.x - var4.lastTickX) * (double)var3;
            BlockEntityRenderer.StaticPlayerY = var4.lastTickY + (var4.y - var4.lastTickY) * (double)var3;
            BlockEntityRenderer.StaticPlayerZ = var4.lastTickZ + (var4.z - var4.lastTickZ) * (double)var3;
            List<Entity> var5 = world.getEntities();
            countEntitiesTotal = var5.Count;

            int var6;
            Entity var7;
            for (var6 = 0; var6 < world.globalEntities.Count; ++var6)
            {
                var7 = world.globalEntities[var6];
                ++countEntitiesRendered;
                if (var7.shouldRender(var1))
                {
                    EntityRenderDispatcher.instance.renderEntity(var7, var3);
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
                if (var7.shouldRender(var1) && (var7.ignoreFrustumCheck || culler.isBoundingBoxInFrustum(var7.boundingBox)) && (var7 != mc.camera || mc.options.CameraMode != EnumCameraMode.FirstPerson || mc.camera.isSleeping()))
                {
                    int var8 = MathHelper.Floor(var7.y);
                    if (var8 < 0)
                    {
                        var8 = 0;
                    }

                    if (var8 >= 128)
                    {
                        var8 = 127;
                    }

                    if (world.isPosLoaded(MathHelper.Floor(var7.x), var8, MathHelper.Floor(var7.z)))
                    {
                        ++countEntitiesRendered;
                        EntityRenderDispatcher.instance.renderEntity(var7, var3);
                    }
                }
            }

            for (var6 = 0; var6 < world.blockEntities.Count; ++var6)
            {
                BlockEntity entity = world.blockEntities[var6];
                if (!entity.isRemoved() && culler.isBoundingBoxInFrustum(new Box(entity.X, entity.Y, entity.Z, entity.X + 1, entity.Y + 1, entity.Z + 1)))
                {
                    BlockEntityRenderer.Instance.RenderTileEntity(entity, var3);
                }
            }
        }
    }

    public string getDebugInfoEntities()
    {
        return "E: " + countEntitiesRendered + "/" + countEntitiesTotal + ". B: " + countEntitiesHidden + ", I: " + (countEntitiesTotal - countEntitiesHidden - countEntitiesRendered);
    }

    public int sortAndRender(EntityLiving var1, int pass, double var3, Culler cam)
    {
        if (mc.options.renderDistance != renderDistance)
        {
            loadRenderers();
        }

        double var33 = var1.lastTickX + (var1.x - var1.lastTickX) * var3;
        double var7 = var1.lastTickY + (var1.y - var1.lastTickY) * var3;
        double var9 = var1.lastTickZ + (var1.z - var1.lastTickZ) * var3;

        Lighting.turnOff();

        var renderParams = new ChunkRenderParams
        {
            Camera = cam,
            ViewPos = new Vector3D<double>(var33, var7, var9),
            RenderDistance = renderDistance,
            Ticks = world.getTime(),
            PartialTicks = (float)var3,
            DeltaTime = mc.Timer.DeltaTime,
            EnvironmentAnimation = mc.options.EnvironmentAnimation,
            ChunkFade = mc.options.ChunkFade,
            RenderOccluded = mc.options.RenderOccluded
        };

        if (pass == 0)
        {
            chunkRenderer.Render(renderParams);
        }
        else
        {
            chunkRenderer.RenderTransparent(renderParams);
        }

        return 0;
    }

    public void updateClouds()
    {
        ++cloudOffsetX;
    }

    public void renderSky(float var1)
    {
        if (!mc.world.dimension.IsNether)
        {
            GLManager.GL.Disable(GLEnum.Texture2D);
            Vector3D<double> var2 = world.getSkyColor(mc.camera, var1);
            float var3 = (float)var2.X;
            float var4 = (float)var2.Y;
            float var5 = (float)var2.Z;
            float var7;
            float var8;

            GLManager.GL.Color3(var3, var4, var5);
            Tessellator var17 = Tessellator.instance;
            GLManager.GL.DepthMask(false);
            GLManager.GL.Enable(GLEnum.Fog);
            GLManager.GL.Color3(var3, var4, var5);
            GLManager.GL.CallList((uint)glSkyList);
            GLManager.GL.Disable(GLEnum.Fog);
            GLManager.GL.Disable(GLEnum.AlphaTest);
            GLManager.GL.Enable(GLEnum.Blend);
            GLManager.GL.BlendFunc(GLEnum.SrcAlpha, GLEnum.OneMinusSrcAlpha);
            Lighting.turnOff();
            float[] var18 = world.dimension.GetBackgroundColor(world.getTime(var1), var1);
            float var9;
            float var10;
            float var11;
            float var12;
            if (var18 != null)
            {
                GLManager.GL.Disable(GLEnum.Texture2D);
                GLManager.GL.ShadeModel(GLEnum.Smooth);
                GLManager.GL.PushMatrix();
                GLManager.GL.Rotate(90.0F, 1.0F, 0.0F, 0.0F);
                var8 = world.getTime(var1);
                GLManager.GL.Rotate(var8 > 0.5F ? 180.0F : 0.0F, 0.0F, 0.0F, 1.0F);
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
                    var17.addVertex((double)(var15 * 120.0F), (double)(var16 * 120.0F), (double)(-var16 * 40.0F * var18[3]));
                }

                var17.draw();
                GLManager.GL.PopMatrix();
                GLManager.GL.ShadeModel(GLEnum.Flat);
            }

            GLManager.GL.Enable(GLEnum.Texture2D);
            GLManager.GL.BlendFunc(GLEnum.SrcAlpha, GLEnum.One);
            GLManager.GL.PushMatrix();
            var7 = 1.0F - world.getRainGradient(var1);
            var8 = 0.0F;
            var9 = 0.0F;
            var10 = 0.0F;
            GLManager.GL.Color4(1.0F, 1.0F, 1.0F, var7);
            GLManager.GL.Translate(var8, var9, var10);
            GLManager.GL.Rotate(0.0F, 0.0F, 0.0F, 1.0F);
            GLManager.GL.Rotate(world.getTime(var1) * 360.0F, 1.0F, 0.0F, 0.0F);
            var11 = 30.0F;
            renderEngine.BindTexture(renderEngine.GetTextureId("/terrain/sun.png"));
            var17.startDrawingQuads();
            var17.addVertexWithUV((double)-var11, 100.0D, (double)-var11, 0.0D, 0.0D);
            var17.addVertexWithUV((double)var11, 100.0D, (double)-var11, 1.0D, 0.0D);
            var17.addVertexWithUV((double)var11, 100.0D, (double)var11, 1.0D, 1.0D);
            var17.addVertexWithUV((double)-var11, 100.0D, (double)var11, 0.0D, 1.0D);
            var17.draw();
            var11 = 20.0F;
            renderEngine.BindTexture(renderEngine.GetTextureId("/terrain/moon.png"));
            var17.startDrawingQuads();
            var17.addVertexWithUV((double)-var11, -100.0D, (double)var11, 1.0D, 1.0D);
            var17.addVertexWithUV((double)var11, -100.0D, (double)var11, 0.0D, 1.0D);
            var17.addVertexWithUV((double)var11, -100.0D, (double)-var11, 0.0D, 0.0D);
            var17.addVertexWithUV((double)-var11, -100.0D, (double)-var11, 1.0D, 0.0D);
            var17.draw();
            GLManager.GL.Disable(GLEnum.Texture2D);
            var12 = world.calcualteSkyLightIntensity(var1) * var7;
            if (var12 > 0.0F)
            {
                GLManager.GL.Color4(var12, var12, var12, var12);
                GLManager.GL.CallList((uint)starGLCallList);
            }

            GLManager.GL.Color4(1.0F, 1.0F, 1.0F, 1.0F);
            GLManager.GL.Disable(GLEnum.Blend);
            GLManager.GL.Enable(GLEnum.AlphaTest);
            GLManager.GL.Enable(GLEnum.Fog);
            GLManager.GL.PopMatrix();
            if (world.dimension.HasGround)
            {
                GLManager.GL.Color3(var3 * 0.2F + 0.04F, var4 * 0.2F + 0.04F, var5 * 0.6F + 0.1F);
            }
            else
            {
                GLManager.GL.Color3(var3, var4, var5);
            }

            GLManager.GL.Disable(GLEnum.Texture2D);
            GLManager.GL.CallList((uint)glSkyList2);
            GLManager.GL.Enable(GLEnum.Texture2D);
            GLManager.GL.DepthMask(true);
        }
    }

    public void renderClouds(float var1)
    {
        Profiler.Start("renderClouds");
        if (!mc.world.dimension.IsNether)
        {
            renderCloudsFancy(var1);
        }
        Profiler.Stop("renderClouds");
    }

    private void buildCloudDisplayLists()
    {
        glCloudsList = GLAllocation.generateDisplayLists(4);
        Tessellator tessellator = Tessellator.instance;

        for (int i = 0; i < 4; ++i)
        {
            GLManager.GL.NewList((uint)(glCloudsList + i), GLEnum.Compile);
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
                        tessellator.addVertexWithUV((double)(var30 + 0.0F), (double)(0.0F), (double)(var31 + var22), (double)((var28 + 0.0F) * uvScale), (double)((var29 + var22) * uvScale));
                        tessellator.addVertexWithUV((double)(var30 + var22), (double)(0.0F), (double)(var31 + var22), (double)((var28 + var22) * uvScale), (double)((var29 + var22) * uvScale));
                        tessellator.addVertexWithUV((double)(var30 + var22), (double)(0.0F), (double)(var31 + 0.0F), (double)((var28 + var22) * uvScale), (double)((var29 + 0.0F) * uvScale));
                        tessellator.addVertexWithUV((double)(var30 + 0.0F), (double)(0.0F), (double)(var31 + 0.0F), (double)((var28 + 0.0F) * uvScale), (double)((var29 + 0.0F) * uvScale));
                    }

                    else if (i == 1)
                    {
                        tessellator.setNormal(0.0F, 1.0F, 0.0F);
                        tessellator.addVertexWithUV((double)(var30 + 0.0F), (double)(cloudHeight - var24), (double)(var31 + var22), (double)((var28 + 0.0F) * uvScale), (double)((var29 + var22) * uvScale));
                        tessellator.addVertexWithUV((double)(var30 + var22), (double)(cloudHeight - var24), (double)(var31 + var22), (double)((var28 + var22) * uvScale), (double)((var29 + var22) * uvScale));
                        tessellator.addVertexWithUV((double)(var30 + var22), (double)(cloudHeight - var24), (double)(var31 + 0.0F), (double)((var28 + var22) * uvScale), (double)((var29 + 0.0F) * uvScale));
                        tessellator.addVertexWithUV((double)(var30 + 0.0F), (double)(cloudHeight - var24), (double)(var31 + 0.0F), (double)((var28 + 0.0F) * uvScale), (double)((var29 + 0.0F) * uvScale));
                    }

                    else if (i == 2)
                    {
                        if (var26 > -1)
                        {
                            tessellator.setNormal(-1.0F, 0.0F, 0.0F);
                            for (int var32 = 0; var32 < var22; ++var32)
                            {
                                tessellator.addVertexWithUV((double)(var30 + var32 + 0.0F), (double)(0.0F), (double)(var31 + var22), (double)((var28 + var32 + 0.5F) * uvScale), (double)((var29 + var22) * uvScale));
                                tessellator.addVertexWithUV((double)(var30 + var32 + 0.0F), (double)(cloudHeight), (double)(var31 + var22), (double)((var28 + var32 + 0.5F) * uvScale), (double)((var29 + var22) * uvScale));
                                tessellator.addVertexWithUV((double)(var30 + var32 + 0.0F), (double)(cloudHeight), (double)(var31 + 0.0F), (double)((var28 + var32 + 0.5F) * uvScale), (double)((var29 + 0.0F) * uvScale));
                                tessellator.addVertexWithUV((double)(var30 + var32 + 0.0F), (double)(0.0F), (double)(var31 + 0.0F), (double)((var28 + var32 + 0.5F) * uvScale), (double)((var29 + 0.0F) * uvScale));
                            }
                        }
                        if (var26 <= 1)
                        {
                            tessellator.setNormal(1.0F, 0.0F, 0.0F);
                            for (int var32 = 0; var32 < var22; ++var32)
                            {
                                tessellator.addVertexWithUV((double)(var30 + var32 + 1.0F - var24), (double)(0.0F), (double)(var31 + var22), (double)((var28 + var32 + 0.5F) * uvScale), (double)((var29 + var22) * uvScale));
                                tessellator.addVertexWithUV((double)(var30 + var32 + 1.0F - var24), (double)(cloudHeight), (double)(var31 + var22), (double)((var28 + var32 + 0.5F) * uvScale), (double)((var29 + var22) * uvScale));
                                tessellator.addVertexWithUV((double)(var30 + var32 + 1.0F - var24), (double)(cloudHeight), (double)(var31 + 0.0F), (double)((var28 + var32 + 0.5F) * uvScale), (double)((var29 + 0.0F) * uvScale));
                                tessellator.addVertexWithUV((double)(var30 + var32 + 1.0F - var24), (double)(0.0F), (double)(var31 + 0.0F), (double)((var28 + var32 + 0.5F) * uvScale), (double)((var29 + 0.0F) * uvScale));
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
                                tessellator.addVertexWithUV((double)(var30 + 0.0F), (double)(cloudHeight), (double)(var31 + var32 + 0.0F), (double)((var28 + 0.0F) * uvScale), (double)((var29 + var32 + 0.5F) * uvScale));
                                tessellator.addVertexWithUV((double)(var30 + var22), (double)(cloudHeight), (double)(var31 + var32 + 0.0F), (double)((var28 + var22) * uvScale), (double)((var29 + var32 + 0.5F) * uvScale));
                                tessellator.addVertexWithUV((double)(var30 + var22), (double)(0.0F), (double)(var31 + var32 + 0.0F), (double)((var28 + var22) * uvScale), (double)((var29 + var32 + 0.5F) * uvScale));
                                tessellator.addVertexWithUV((double)(var30 + 0.0F), (double)(0.0F), (double)(var31 + var32 + 0.0F), (double)((var28 + 0.0F) * uvScale), (double)((var29 + var32 + 0.5F) * uvScale));
                            }
                        }
                        if (var27 <= 1)
                        {
                            tessellator.setNormal(0.0F, 0.0F, 1.0F);
                            for (int var32 = 0; var32 < var22; ++var32)
                            {
                                tessellator.addVertexWithUV((double)(var30 + 0.0F), (double)(cloudHeight), (double)(var31 + var32 + 1.0F - var24), (double)((var28 + 0.0F) * uvScale), (double)((var29 + var32 + 0.5F) * uvScale));
                                tessellator.addVertexWithUV((double)(var30 + var22), (double)(cloudHeight), (double)(var31 + var32 + 1.0F - var24), (double)((var28 + var22) * uvScale), (double)((var29 + var32 + 0.5F) * uvScale));
                                tessellator.addVertexWithUV((double)(var30 + var22), (double)(0.0F), (double)(var31 + var32 + 1.0F - var24), (double)((var28 + var22) * uvScale), (double)((var29 + var32 + 0.5F) * uvScale));
                                tessellator.addVertexWithUV((double)(var30 + 0.0F), (double)(0.0F), (double)(var31 + var32 + 1.0F - var24), (double)((var28 + 0.0F) * uvScale), (double)((var29 + var32 + 0.5F) * uvScale));
                            }
                        }
                    }
                }
            }
            tessellator.draw();
            GLManager.GL.EndList();
        }
    }

    private void renderCloudsFancy(float var1)
    {
        GLManager.GL.Disable(GLEnum.CullFace);
        float var2 = (float)(mc.camera.lastTickY + (mc.camera.y - mc.camera.lastTickY) * (double)var1);
        float var4 = 12.0F;
        float var5 = 4.0F;
        double var6 = (mc.camera.prevX + (mc.camera.x - mc.camera.prevX) * (double)var1 + (double)((cloudOffsetX + var1) * 0.03F)) / (double)var4;
        double var8 = (mc.camera.prevZ + (mc.camera.z - mc.camera.prevZ) * (double)var1) / (double)var4 + (double)0.33F;
        float var10 = world.dimension.CloudHeight - var2 + 0.33F;
        int var11 = MathHelper.Floor(var6 / 2048.0D);
        int var12 = MathHelper.Floor(var8 / 2048.0D);
        var6 -= var11 * 2048;
        var8 -= var12 * 2048;
        renderEngine.BindTexture(renderEngine.GetTextureId("/environment/clouds.png"));
        GLManager.GL.Enable(GLEnum.Blend);
        GLManager.GL.BlendFunc(GLEnum.SrcAlpha, GLEnum.OneMinusSrcAlpha);
        Vector3D<double> var13 = world.getCloudColor(var1);
        float var14 = (float)var13.X;
        float var15 = (float)var13.Y;
        float var16 = (float)var13.Z;

        float var19 = 1 / 256f;
        float var17 = MathHelper.Floor(var6) * var19;
        float var18 = MathHelper.Floor(var8) * var19;
        float var20 = (float)(var6 - MathHelper.Floor(var6));
        float var21 = (float)(var8 - MathHelper.Floor(var8));

        GLManager.GL.Scale(var4, 1.0F, var4);

        for (int var25 = 0; var25 < 2; ++var25)
        {
            if (var25 == 0)
            {
                GLManager.GL.ColorMask(false, false, false, false);
            }
            else
            {
                GLManager.GL.ColorMask(true, true, true, true);
            }

            GLManager.GL.PushMatrix();
            GLManager.GL.Translate(-var20, var10, -var21);

            GLManager.GL.MatrixMode(GLEnum.Texture);
            GLManager.GL.PushMatrix();
            GLManager.GL.Translate(var17, var18, 0.0F);
            GLManager.GL.MatrixMode(GLEnum.Modelview);

            if (var10 > -var5 - 1.0F)
            {
                GLManager.GL.Color4(var14 * 0.7F, var15 * 0.7F, var16 * 0.7F, 0.8F);
                GLManager.GL.CallList((uint)(glCloudsList + 0)); // Bottom
            }

            if (var10 <= var5 + 1.0F)
            {
                GLManager.GL.Color4(var14, var15, var16, 0.8F);
                GLManager.GL.CallList((uint)(glCloudsList + 1)); // Top
            }

            GLManager.GL.Color4(var14 * 0.9F, var15 * 0.9F, var16 * 0.9F, 0.8F);
            GLManager.GL.CallList((uint)(glCloudsList + 2)); // Side X

            GLManager.GL.Color4(var14 * 0.8F, var15 * 0.8F, var16 * 0.8F, 0.8F);
            GLManager.GL.CallList((uint)(glCloudsList + 3)); // Side Z

            GLManager.GL.MatrixMode(GLEnum.Texture);
            GLManager.GL.PopMatrix();
            GLManager.GL.MatrixMode(GLEnum.Modelview);

            GLManager.GL.PopMatrix();
        }

        GLManager.GL.Color4(1.0F, 1.0F, 1.0F, 1.0F);
        GLManager.GL.Disable(GLEnum.Blend);
        GLManager.GL.Enable(GLEnum.CullFace);
    }

    public void drawBlockBreaking(EntityPlayer var1, HitResult var2, int var3, ItemStack var4, float var5)
    {
        Tessellator var6 = Tessellator.instance;
        GLManager.GL.Enable(GLEnum.Blend);
        GLManager.GL.Enable(GLEnum.AlphaTest);
        GLManager.GL.BlendFunc(GLEnum.SrcAlpha, GLEnum.One);
        GLManager.GL.Color4(1.0F, 1.0F, 1.0F, (MathHelper.Sin(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
 / 100.0F) * 0.2F + 0.4F) * 0.5F);
        int var8;
        if (var3 == 0)
        {
            if (damagePartialTime > 0.0F)
            {
                GLManager.GL.BlendFunc(GLEnum.DstColor, GLEnum.SrcColor);
                renderEngine.BindTexture(renderEngine.GetTextureId("/terrain.png"));
                GLManager.GL.Color4(1.0F, 1.0F, 1.0F, 0.5F);
                GLManager.GL.PushMatrix();
                var8 = world.getBlockId(var2.BlockX, var2.BlockY, var2.BlockZ);
                Block var9 = var8 > 0 ? Block.Blocks[var8] : null;
                GLManager.GL.Disable(GLEnum.AlphaTest);
                GLManager.GL.PolygonOffset(-3.0F, -3.0F);
                GLManager.GL.Enable(GLEnum.PolygonOffsetFill);
                double var10 = var1.lastTickX + (var1.x - var1.lastTickX) * (double)var5;
                double var12 = var1.lastTickY + (var1.y - var1.lastTickY) * (double)var5;
                double var14 = var1.lastTickZ + (var1.z - var1.lastTickZ) * (double)var5;
                var9 ??= Block.Stone;

                GLManager.GL.Enable(GLEnum.AlphaTest);
                var6.startDrawingQuads();
                var6.setTranslationD(-var10, -var12, -var14);
                var6.disableColor();
                globalRenderBlocks.renderBlockUsingTexture(var9, var2.BlockX, var2.BlockY, var2.BlockZ, 240 + (int)(damagePartialTime * 10.0F));
                var6.draw();
                var6.setTranslationD(0.0D, 0.0D, 0.0D);
                GLManager.GL.Disable(GLEnum.AlphaTest);
                GLManager.GL.PolygonOffset(0.0F, 0.0F);
                GLManager.GL.Disable(GLEnum.PolygonOffsetFill);
                GLManager.GL.Enable(GLEnum.AlphaTest);
                GLManager.GL.DepthMask(true);
                GLManager.GL.PopMatrix();
            }
        }
        else if (var4 != null)
        {
            GLManager.GL.BlendFunc(GLEnum.SrcAlpha, GLEnum.OneMinusSrcAlpha);
            float var16 = MathHelper.Sin(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
 / 100.0F) * 0.2F + 0.8F;
            GLManager.GL.Color4(var16, var16, var16, MathHelper.Sin(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
 / 200.0F) * 0.2F + 0.5F);
            renderEngine.BindTexture(renderEngine.GetTextureId("/terrain.png"));
            int var17 = var2.BlockX;
            int var18 = var2.BlockY;
            int var11 = var2.BlockZ;
            if (var2.Side == 0)
            {
                --var18;
            }

            if (var2.Side == 1)
            {
                ++var18;
            }

            if (var2.Side == 2)
            {
                --var11;
            }

            if (var2.Side == 3)
            {
                ++var11;
            }

            if (var2.Side == 4)
            {
                --var17;
            }

            if (var2.Side == 5)
            {
                ++var17;
            }
        }

        GLManager.GL.Disable(GLEnum.Blend);
        GLManager.GL.Disable(GLEnum.AlphaTest);
    }

    public void drawSelectionBox(EntityPlayer var1, HitResult var2, int var3, ItemStack var4, float var5)
    {
        if (var3 == 0 && var2.Type == HitResultType.TILE)
        {
            GLManager.GL.Enable(GLEnum.Blend);
            GLManager.GL.BlendFunc(GLEnum.SrcAlpha, GLEnum.OneMinusSrcAlpha);
            GLManager.GL.Color4(0.0F, 0.0F, 0.0F, 0.4F);
            GLManager.GL.LineWidth(2.0F);
            GLManager.GL.Disable(GLEnum.Texture2D);
            GLManager.GL.DepthMask(false);
            float var6 = 0.002F;
            int var7 = world.getBlockId(var2.BlockX, var2.BlockY, var2.BlockZ);
            if (var7 > 0)
            {
                Block.Blocks[var7].updateBoundingBox(world, var2.BlockX, var2.BlockY, var2.BlockZ);
                double var8 = var1.lastTickX + (var1.x - var1.lastTickX) * (double)var5;
                double var10 = var1.lastTickY + (var1.y - var1.lastTickY) * (double)var5;
                double var12 = var1.lastTickZ + (var1.z - var1.lastTickZ) * (double)var5;
                drawOutlinedBoundingBox(Block.Blocks[var7].getBoundingBox(world, var2.BlockX, var2.BlockY, var2.BlockZ).Expand((double)var6, (double)var6, (double)var6).Offset(-var8, -var10, -var12));
            }

            GLManager.GL.DepthMask(true);
            GLManager.GL.Enable(GLEnum.Texture2D);
            GLManager.GL.Disable(GLEnum.Blend);
        }

    }

    private void drawOutlinedBoundingBox(Box var1)
    {
        Tessellator var2 = Tessellator.instance;
        var2.startDrawing(3);
        var2.addVertex(var1.MinX, var1.MinY, var1.MinZ);
        var2.addVertex(var1.MaxX, var1.MinY, var1.MinZ);
        var2.addVertex(var1.MaxX, var1.MinY, var1.MaxZ);
        var2.addVertex(var1.MinX, var1.MinY, var1.MaxZ);
        var2.addVertex(var1.MinX, var1.MinY, var1.MinZ);
        var2.draw();
        var2.startDrawing(3);
        var2.addVertex(var1.MinX, var1.MaxY, var1.MinZ);
        var2.addVertex(var1.MaxX, var1.MaxY, var1.MinZ);
        var2.addVertex(var1.MaxX, var1.MaxY, var1.MaxZ);
        var2.addVertex(var1.MinX, var1.MaxY, var1.MaxZ);
        var2.addVertex(var1.MinX, var1.MaxY, var1.MinZ);
        var2.draw();
        var2.startDrawing(1);
        var2.addVertex(var1.MinX, var1.MinY, var1.MinZ);
        var2.addVertex(var1.MinX, var1.MaxY, var1.MinZ);
        var2.addVertex(var1.MaxX, var1.MinY, var1.MinZ);
        var2.addVertex(var1.MaxX, var1.MaxY, var1.MinZ);
        var2.addVertex(var1.MaxX, var1.MinY, var1.MaxZ);
        var2.addVertex(var1.MaxX, var1.MaxY, var1.MaxZ);
        var2.addVertex(var1.MinX, var1.MinY, var1.MaxZ);
        var2.addVertex(var1.MinX, var1.MaxY, var1.MaxZ);
        var2.draw();
    }

    public void MarkBlocksDirty(int var1, int var2, int var3, int var4, int var5, int var6)
    {
        int var7 = MathHelper.FloorDiv(var1, SubChunkRenderer.Size);
        int var8 = MathHelper.FloorDiv(var2, SubChunkRenderer.Size);
        int var9 = MathHelper.FloorDiv(var3, SubChunkRenderer.Size);
        int var10 = MathHelper.FloorDiv(var4, SubChunkRenderer.Size);
        int var11 = MathHelper.FloorDiv(var5, SubChunkRenderer.Size);
        int var12 = MathHelper.FloorDiv(var6, SubChunkRenderer.Size);

        for (int var13 = var7; var13 <= var10; ++var13)
        {
            for (int var15 = var8; var15 <= var11; ++var15)
            {
                for (int var17 = var9; var17 <= var12; ++var17)
                {
                    chunkRenderer.MarkDirty(new Vector3D<int>(var13, var15, var17) * SubChunkRenderer.Size, true);
                }
            }
        }
    }

    public void blockUpdate(int var1, int var2, int var3)
    {
        MarkBlocksDirty(var1 - 1, var2 - 1, var3 - 1, var1 + 1, var2 + 1, var3 + 1);
    }

    public void setBlocksDirty(int var1, int var2, int var3, int var4, int var5, int var6)
    {
        MarkBlocksDirty(var1 - 1, var2 - 1, var3 - 1, var4 + 1, var5 + 1, var6 + 1);
    }

    public void playStreaming(string var1, int var2, int var3, int var4)
    {
        if (var1 != null)
        {
            mc.ingameGUI.setRecordPlayingMessage("C418 - " + var1);
        }

        mc.sndManager.PlayStreaming(var1, var2, var3, var4, 1.0F, 1.0F);
    }

    public void playSound(string var1, double var2, double var4, double var6, float var8, float var9)
    {
        float var10 = 16.0F;
        if (var8 > 1.0F)
        {
            var10 *= var8;
        }

        if (mc.camera.getSquaredDistance(var2, var4, var6) < (double)(var10 * var10))
        {
            mc.sndManager.PlaySound(var1, (float)var2, (float)var4, (float)var6, var8, var9);
        }

    }

    public void spawnParticle(string var1, double var2, double var4, double var6, double var8, double var10, double var12)
    {
        if (mc != null && mc.camera != null && mc.particleManager != null)
        {
            double var14 = mc.camera.x - var2;
            double var16 = mc.camera.y - var4;
            double var18 = mc.camera.z - var6;
            double var20 = 16.0D;
            if (var14 * var14 + var16 * var16 + var18 * var18 <= var20 * var20)
            {
                if (var1.Equals("bubble"))
                {
                    mc.particleManager.addEffect(new EntityBubbleFX(world, var2, var4, var6, var8, var10, var12));
                }
                else if (var1.Equals("smoke"))
                {
                    mc.particleManager.addEffect(new EntitySmokeFX(world, var2, var4, var6, var8, var10, var12));
                }
                else if (var1.Equals("note"))
                {
                    mc.particleManager.addEffect(new EntityNoteFX(world, var2, var4, var6, var8, var10, var12));
                }
                else if (var1.Equals("portal"))
                {
                    mc.particleManager.addEffect(new EntityPortalFX(world, var2, var4, var6, var8, var10, var12));
                }
                else if (var1.Equals("explode"))
                {
                    mc.particleManager.addEffect(new EntityExplodeFX(world, var2, var4, var6, var8, var10, var12));
                }
                else if (var1.Equals("flame"))
                {
                    mc.particleManager.addEffect(new EntityFlameFX(world, var2, var4, var6, var8, var10, var12));
                }
                else if (var1.Equals("lava"))
                {
                    mc.particleManager.addEffect(new EntityLavaFX(world, var2, var4, var6));
                }
                else if (var1.Equals("footstep"))
                {
                    mc.particleManager.addEffect(new EntityFootStepFX(renderEngine, world, var2, var4, var6));
                }
                else if (var1.Equals("splash"))
                {
                    mc.particleManager.addEffect(new EntitySplashFX(world, var2, var4, var6, var8, var10, var12));
                }
                else if (var1.Equals("largesmoke"))
                {
                    mc.particleManager.addEffect(new EntitySmokeFX(world, var2, var4, var6, var8, var10, var12, 2.5F));
                }
                else if (var1.Equals("reddust"))
                {
                    mc.particleManager.addEffect(new EntityReddustFX(world, var2, var4, var6, (float)var8, (float)var10, (float)var12));
                }
                else if (var1.Equals("snowballpoof"))
                {
                    mc.particleManager.addEffect(new EntitySlimeFX(world, var2, var4, var6, Item.Snowball));
                }
                else if (var1.Equals("snowshovel"))
                {
                    mc.particleManager.addEffect(new EntitySnowShovelFX(world, var2, var4, var6, var8, var10, var12));
                }
                else if (var1.Equals("slime"))
                {
                    mc.particleManager.addEffect(new EntitySlimeFX(world, var2, var4, var6, Item.Slimeball));
                }
                else if (var1.Equals("heart"))
                {
                    mc.particleManager.addEffect(new EntityHeartFX(world, var2, var4, var6, var8, var10, var12));
                }

            }
        }
    }

    public void notifyEntityAdded(Entity var1)
    {
        var1.updateCloak();
        if (!string.IsNullOrEmpty(var1.skinUrl) && mc.session.skinUrl != null)
        {
            EntityRenderDispatcher.instance.skinManager?.RequestDownload(var1.skinUrl);
        }
    }

    public void notifyEntityRemoved(Entity var1)
    {
        EntityRenderDispatcher.instance.skinManager?.Release(var1.skinUrl);
    }

    public void notifyAmbientDarknessChanged()
    {
        chunkRenderer.UpdateAllRenderers();
    }

    public void updateBlockEntity(int var1, int var2, int var3, BlockEntity var4)
    {
    }

    public void worldEvent(EntityPlayer var1, int var2, int var3, int var4, int var5, int var6)
    {
        JavaRandom var7 = world.random;
        int var16;
        switch (var2)
        {
            case 1000:
                world.playSound(var3, var4, var5, "random.click", 1.0F, 1.0F);
                break;
            case 1001:
                world.playSound(var3, var4, var5, "random.click", 1.0F, 1.2F);
                break;
            case 1002:
                world.playSound(var3, var4, var5, "random.bow", 1.0F, 1.2F);
                break;
            case 1003:
                if (Random.Shared.NextDouble() < 0.5D)
                {
                    world.playSound(var3 + 0.5D, var4 + 0.5D, var5 + 0.5D, "random.door_open", 1.0F, world.random.NextFloat() * 0.1F + 0.9F);
                }
                else
                {
                    world.playSound(var3 + 0.5D, var4 + 0.5D, var5 + 0.5D, "random.door_close", 1.0F, world.random.NextFloat() * 0.1F + 0.9F);
                }
                break;
            case 1004:
                world.playSound((double)(var3 + 0.5F), (double)(var4 + 0.5F), (double)(var5 + 0.5F), "random.fizz", 0.5F, 2.6F + (var7.NextFloat() - var7.NextFloat()) * 0.8F);
                break;
            case 1005:
                if (Item.ITEMS[var6] is ItemRecord)
                {
                    world.playStreaming(((ItemRecord)Item.ITEMS[var6]).recordName, var3, var4, var5);
                }
                else
                {
                    world.playStreaming(null, var3, var4, var5);
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
                    spawnParticle("smoke", var19, var21, var23, var25, var27, var29);
                }

                return;
            case 2001: // This is for breaking a block
                var16 = var6 & 255;
                if (var16 > 0)
                {
                    Block var17 = Block.Blocks[var16];
                    mc.sndManager.PlaySound(var17.soundGroup.BreakSound, var3 + 0.5F, var4 + 0.5F, var5 + 0.5F, (var17.soundGroup.Volume + 1.0F) / 2.0F, var17.soundGroup.Pitch * 0.8F);
                }

                mc.particleManager.addBlockDestroyEffects(var3, var4, var5, var6 & 255, var6 >> 8 & 255);
                break;
        }

    }
}
