using System.Diagnostics;
using BetaSharp.Blocks;
using BetaSharp.Blocks.Materials;
using BetaSharp.Client.Entities.FX;
using BetaSharp.Client.Input;
using BetaSharp.Client.Options;
using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Items;
using BetaSharp.Entities;
using BetaSharp.Profiling;
using BetaSharp.Util.Hit;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds;
using BetaSharp.Worlds.Biomes;
using BetaSharp.Worlds.Chunks;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using GLEnum = BetaSharp.Client.Rendering.Core.OpenGL.GLEnum;

namespace BetaSharp.Client.Rendering;

public class GameRenderer
{
    private readonly bool _cloudFog = false;
    private readonly BetaSharp _client;
    private float _viewDistance;
    public HeldItemRenderer itemRenderer;
    public CameraController cameraController;
    private int _ticks;
    private Entity _targetedEntity;
    private readonly MouseFilter _mouseFilterXAxis = new();
    private readonly MouseFilter _mouseFilterYAxis = new();

    private long _prevFrameTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
;
    private readonly JavaRandom _random = new();
    private int _rainSoundCounter;
    private readonly float[] _fogColorBuffer = new float[16];
    private float _fogColorRed;
    private float _fogColorGreen;
    private float _fogColorBlue;

    private readonly Stopwatch _fpsTimer = Stopwatch.StartNew();

    public GameRenderer(BetaSharp game)
    {
        _client = game;
        itemRenderer = new HeldItemRenderer(game);
        cameraController = new CameraController(game);
    }

    public void updateCamera()
    {
        cameraController.UpdateCamera();
        ++_ticks;
        itemRenderer.updateEquippedItem();
        renderRain();
    }

    public void tick(float var1)
    {
        if (_client.terrainRenderer != null)
        {
            _client.terrainRenderer.tick(_client.camera, var1);
        }
    }

    public void updateTargetedEntity(float tickDelta)
    {
        if (_client.camera != null)
        {
            if (_client.world != null)
            {
                double var2 = (double)_client.playerController.getBlockReachDistance();
                _client.objectMouseOver = _client.camera.rayTrace(var2, tickDelta);
                double var4 = var2;
                Vec3D var6 = _client.camera.getPosition(tickDelta);
                if (_client.objectMouseOver.Type != HitResultType.MISS)
                {
                    var4 = _client.objectMouseOver.Pos.distanceTo(var6);
                }

                if (var4 > 3.0D)
                {
                    var4 = 3.0D;
                }

                var2 = var4;

                Vec3D var7 = _client.camera.getLook(tickDelta);
                Vec3D var8 = var6 + var2 * var7;
                _targetedEntity = null;
                float var9 = 1.0F;
                var var10 = _client.world.getEntities(_client.camera, _client.camera.boundingBox.Stretch(var7.x * var2, var7.y * var2, var7.z * var2).Expand((double)var9, (double)var9, (double)var9));
                double var11 = 0.0D;

                for (int var13 = 0; var13 < var10.Count; ++var13)
                {
                    Entity var14 = var10[var13];
                    if (var14.isCollidable())
                    {
                        float var15 = var14.getTargetingMargin();
                        Box var16 = var14.boundingBox.Expand((double)var15, (double)var15, (double)var15);
                        HitResult var17 = var16.Raycast(var6, var8);
                        if (var16.Contains(var6))
                        {
                            if (0.0D < var11 || var11 == 0.0D)
                            {
                                _targetedEntity = var14;
                                var11 = 0.0D;
                            }
                        }
                        else if (var17.Type != HitResultType.MISS)
                        {
                            double var18 = var6.distanceTo(var17.Pos);
                            if (var18 < var11 || var11 == 0.0D)
                            {
                                _targetedEntity = var14;
                                var11 = var18;
                            }
                        }
                    }
                }

                if (_targetedEntity != null)
                {
                    _client.objectMouseOver = new HitResult(_targetedEntity);
                }

            }
        }
    }



    private void renderWorld(float tickDelta)
    {
        _viewDistance = _client.options.renderDistance * 16.0f;
        GLManager.GL.MatrixMode(GLEnum.Projection);
        GLManager.GL.LoadIdentity();

        if (cameraController.CameraZoom != 1.0D)
        {
            GLManager.GL.Translate((float)cameraController.CameraYaw, (float)-cameraController.CameraPitch, 0.0F);
            GLManager.GL.Scale(cameraController.CameraZoom, cameraController.CameraZoom, 1.0D);
            GLU.gluPerspective(cameraController.GetFov(tickDelta), _client.displayWidth / (float)_client.displayHeight, 0.05F, _viewDistance * 2.0F);
        }
        else
        {
            GLU.gluPerspective(cameraController.GetFov(tickDelta), _client.displayWidth / (float)_client.displayHeight, 0.05F, _viewDistance * 2.0F);
        }

        GLManager.GL.MatrixMode(GLEnum.Modelview);
        GLManager.GL.LoadIdentity();

        cameraController.ApplyDamageTiltEffect(tickDelta);
        if (_client.options.ViewBobbing)
        {
            cameraController.ApplyViewBobbing(tickDelta);
        }

        float var4 = _client.player.lastScreenDistortion + (_client.player.changeDimensionCooldown - _client.player.lastScreenDistortion) * tickDelta;
        if (var4 > 0.0F)
        {
            float var5 = 5.0F / (var4 * var4 + 5.0F) - var4 * 0.04F;
            var5 *= var5;
            GLManager.GL.Rotate((_ticks + tickDelta) * 20.0F, 0.0F, 1.0F, 1.0F);
            GLManager.GL.Scale(1.0F / var5, 1.0F, 1.0F);
            GLManager.GL.Rotate(-(_ticks + tickDelta) * 20.0F, 0.0F, 1.0F, 1.0F);
        }

        cameraController.ApplyCameraTransform(tickDelta);
    }

    private void renderFirstPersonHand(float tickDelta)
    {
        GLManager.GL.MatrixMode(GLEnum.Projection);
        GLManager.GL.LoadIdentity();
        if (cameraController.CameraZoom != 1.0D)
        {
            GLManager.GL.Translate((float)cameraController.CameraYaw, (float)-cameraController.CameraPitch, 0.0F);
            GLManager.GL.Scale(cameraController.CameraZoom, cameraController.CameraZoom, 1.0D);
        }
        GLU.gluPerspective(cameraController.GetFov(tickDelta, true), _client.displayWidth / (float)_client.displayHeight, 0.05F, _viewDistance * 2.0F);
        GLManager.GL.MatrixMode(GLEnum.Modelview);
        GLManager.GL.LoadIdentity();

        GLManager.GL.PushMatrix();
        cameraController.ApplyDamageTiltEffect(tickDelta);
        if (_client.options.ViewBobbing)
        {
            cameraController.ApplyViewBobbing(tickDelta);
        }

        if (_client.options.CameraMode == EnumCameraMode.FirstPerson && !_client.camera.isSleeping() && !_client.options.HideGUI)
        {
            itemRenderer.renderItemInFirstPerson(tickDelta);
        }

        GLManager.GL.PopMatrix();
        if (_client.options.CameraMode == EnumCameraMode.FirstPerson && !_client.camera.isSleeping())
        {
            itemRenderer.renderOverlays(tickDelta);
            cameraController.ApplyDamageTiltEffect(tickDelta);
        }

        if (_client.options.ViewBobbing)
        {
            cameraController.ApplyViewBobbing(tickDelta);
        }

    }

    public void onFrameUpdate(float tickDelta)
    {
        if (!Display.isActive())
        {
            if (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
 - _prevFrameTime > 500L)
            {
                _client.displayInGameMenu();
            }
        }
        else
        {
            _prevFrameTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
;
        }

        if (_client.inGameHasFocus)
        {
            _client.mouseHelper.mouseXYChange();
            float var2 = _client.options.MouseSensitivity * 0.6F + 0.2F;
            float var3 = var2 * var2 * var2 * 8.0F;
            float var4 = _client.mouseHelper.DeltaX * var3;
            float var5 = _client.mouseHelper.DeltaY * var3;
            int var6 = -1;
            if (_client.options.InvertMouse)
            {
                var6 = 1;
            }
            if (_client.options.SmoothCamera)
            {
                var4 = _mouseFilterXAxis.Smooth(var4, 0.05F * var3);
                var5 = _mouseFilterYAxis.Smooth(var5, 0.05F * var3);
            }
            _client.player.changeLookDirection(var4, var5 * var6);
        }

        if (!_client.skipRenderWorld)
        {
            ScaledResolution var13 = new(_client.options, _client.displayWidth, _client.displayHeight);
            int scaledWidth = var13.ScaledWidth;
            int scaledHeight = var13.ScaledHeight;
            int scaledMouseX = Mouse.getX() * scaledWidth / _client.displayWidth;
            int scaledMouseY = scaledHeight - Mouse.getY() * scaledHeight / _client.displayHeight - 1;
            int var7 = 30 + (int)(_client.options.LimitFramerate * 210.0f);

            if (var7 < 240)
            {
                Display.setVSyncEnabled(false);
            }

            if (_client.world != null)
            {
                Profiler.PushGroup("renderWorld");
                renderFrame(tickDelta, 0L);
                Profiler.PopGroup();
                Profiler.Start("renderGameOverlay");
                if (!_client.options.HideGUI || _client.currentScreen != null)
                {
                    _client.ingameGUI.renderGameOverlay(tickDelta, _client.currentScreen != null, scaledMouseX, scaledMouseY);
                }
                Profiler.Stop("renderGameOverlay");
            }
            else
            {
                GLManager.GL.Viewport(0, 0, (uint)_client.displayWidth, (uint)_client.displayHeight);
                GLManager.GL.MatrixMode(GLEnum.Projection);
                GLManager.GL.LoadIdentity();
                GLManager.GL.MatrixMode(GLEnum.Modelview);
                GLManager.GL.LoadIdentity();
                setupHudRender();
            }

            if (_client.currentScreen != null)
            {
                GLManager.GL.Clear(ClearBufferMask.DepthBufferBit);
                _client.currentScreen.Render(scaledMouseX, scaledMouseY, tickDelta);
                if (_client.currentScreen != null && _client.currentScreen.ParticlesGui != null)
                {
                    _client.currentScreen.ParticlesGui.render(tickDelta);
                }
            }

            if (var7 < 240)
            {
                //frametime in milliseconds
                double targetMs = 1000.0 / var7;

                double elapsedMs = _fpsTimer.Elapsed.TotalMilliseconds;
                double waitTime = targetMs - elapsedMs;

                if (waitTime > 0)
                {
                    if (waitTime > 2.0)
                    {
                        Thread.Sleep((int)(waitTime - 1.0));
                    }

                    while (_fpsTimer.Elapsed.TotalMilliseconds < targetMs)
                    {
                        Thread.SpinWait(10);
                    }
                }

                _fpsTimer.Restart();
            }
        }
    }

    public void renderFrame(float tickDelta, long time)
    {
        GLManager.GL.Enable(GLEnum.CullFace);
        GLManager.GL.Enable(GLEnum.DepthTest);
        _client.camera ??= _client.player;

        Profiler.Start("getMouseOver");
        updateTargetedEntity(tickDelta);
        Profiler.Stop("getMouseOver");

        EntityLiving var4 = _client.camera;
        WorldRenderer var5 = _client.terrainRenderer;
        ParticleManager var6 = _client.particleManager;
        double var7 = var4.lastTickX + (var4.x - var4.lastTickX) * (double)tickDelta;
        double var9 = var4.lastTickY + (var4.y - var4.lastTickY) * (double)tickDelta;
        double var11 = var4.lastTickZ + (var4.z - var4.lastTickZ) * (double)tickDelta;
        ChunkSource var13 = _client.world.GetChunkSource();

        Profiler.Start("updateFog");
        GLManager.GL.Viewport(0, 0, (uint)_client.displayWidth, (uint)_client.displayHeight);
        updateSkyAndFogColors(tickDelta);
        Profiler.Stop("updateFog");
        GLManager.GL.Clear(ClearBufferMask.DepthBufferBit | ClearBufferMask.ColorBufferBit);
        GLManager.GL.Enable(GLEnum.CullFace);
        renderWorld(tickDelta);
        Frustum.Instance();
        if (_client.options.renderDistance >= 8)
        {
            applyFog(-1);
            var5.renderSky(tickDelta);
        }

        GLManager.GL.Enable(GLEnum.Fog);
        applyFog(1);

        FrustrumCuller var19 = new();
        var19.setPosition(var7, var9, var11);

        applyFog(0);
        GLManager.GL.Enable(GLEnum.Fog);
        _client.textureManager.BindTexture(_client.textureManager.GetTextureId("/terrain.png"));
        Lighting.turnOff();

        Profiler.Start("sortAndRender");
        var5.sortAndRender(var4, 0, (double)tickDelta, var19);
        Profiler.Stop("sortAndRender");

        GLManager.GL.ShadeModel(GLEnum.Flat);
        Lighting.turnOn();

        Profiler.Start("renderEntities");
        var5.renderEntities(var4.getPosition(tickDelta), var19, tickDelta);
        Profiler.Stop("renderEntities");

        var6.func_1187_b(var4, tickDelta);

        Lighting.turnOff();
        applyFog(0);

        Profiler.Start("renderParticles");
        var6.renderParticles(var4, tickDelta);
        Profiler.Stop("renderParticles");

        EntityPlayer var21;
        if (_client.objectMouseOver.Type != HitResultType.MISS && var4.isInFluid(Material.Water) && var4 is EntityPlayer)
        {
            var21 = (EntityPlayer)var4;
            GLManager.GL.Disable(GLEnum.AlphaTest);
            var5.drawBlockBreaking(var21, _client.objectMouseOver, 0, var21.inventory.getSelectedItem(), tickDelta);
            var5.drawSelectionBox(var21, _client.objectMouseOver, 0, var21.inventory.getSelectedItem(), tickDelta);
            GLManager.GL.Enable(GLEnum.AlphaTest);
        }

        GLManager.GL.BlendFunc(GLEnum.SrcAlpha, GLEnum.OneMinusSrcAlpha);
        applyFog(0);
        GLManager.GL.Enable(GLEnum.Blend);
        GLManager.GL.Disable(GLEnum.CullFace);
        _client.textureManager.BindTexture(_client.textureManager.GetTextureId("/terrain.png"));

        Profiler.Start("sortAndRender2");

        var5.sortAndRender(var4, 1, tickDelta, var19);

        GLManager.GL.ShadeModel(GLEnum.Flat);

        Profiler.Stop("sortAndRender2");

        //TODO: SELCTION BOX/BLOCK BREAKING VISUALIZATON DON'T APPEAR PROPERLY MOST OF THE TIME, SAME WITH ENTITY SHADOWS. VIEW BOBBING MAKES ENTITES BOB UP AND DOWN

        GLManager.GL.DepthMask(true);
        GLManager.GL.Enable(GLEnum.CullFace);
        GLManager.GL.Disable(GLEnum.Blend);
        if (cameraController.CameraZoom == 1.0D && var4 is EntityPlayer && _client.objectMouseOver.Type != HitResultType.MISS && !var4.isInFluid(Material.Water))
        {
            var21 = (EntityPlayer)var4;
            GLManager.GL.Disable(GLEnum.AlphaTest);
            var5.drawBlockBreaking(var21, _client.objectMouseOver, 0, var21.inventory.getSelectedItem(), tickDelta);
            var5.drawSelectionBox(var21, _client.objectMouseOver, 0, var21.inventory.getSelectedItem(), tickDelta);
            GLManager.GL.Enable(GLEnum.AlphaTest);
        }

        renderSnow(tickDelta);
        GLManager.GL.Disable(GLEnum.Fog);
        if (_targetedEntity != null)
        {
        }

        applyFog(0);
        GLManager.GL.Enable(GLEnum.Fog);
        var5.renderClouds(tickDelta);
        GLManager.GL.Disable(GLEnum.Fog);
        applyFog(1);
        if (cameraController.CameraZoom == 1.0D)
        {
            GLManager.GL.Clear(ClearBufferMask.DepthBufferBit);
            renderFirstPersonHand(tickDelta);
        }
    }

    private void renderRain()
    {
        float var1 = _client.world.getRainGradient(1.0F);

        if (var1 != 0.0F)
        {
            _random.SetSeed(_ticks * 312987231L);
            EntityLiving var2 = _client.camera;
            World var3 = _client.world;
            int var4 = MathHelper.Floor(var2.x);
            int var5 = MathHelper.Floor(var2.y);
            int var6 = MathHelper.Floor(var2.z);
            byte var7 = 10;
            double var8 = 0.0D;
            double var10 = 0.0D;
            double var12 = 0.0D;
            int var14 = 0;

            for (int var15 = 0; var15 < (int)(100.0F * var1 * var1); ++var15)
            {
                int var16 = var4 + _random.NextInt(var7) - _random.NextInt(var7);
                int var17 = var6 + _random.NextInt(var7) - _random.NextInt(var7);
                int var18 = var3.getTopSolidBlockY(var16, var17);
                int var19 = var3.getBlockId(var16, var18 - 1, var17);
                if (var18 <= var5 + var7 && var18 >= var5 - var7 && var3.getBiomeSource().GetBiome(var16, var17).CanSpawnLightningBolt())
                {
                    float var20 = _random.NextFloat();
                    float var21 = _random.NextFloat();
                    if (var19 > 0)
                    {
                        if (Block.Blocks[var19].material == Material.Lava)
                        {
                            _client.particleManager.addEffect(new EntitySmokeFX(var3, (double)(var16 + var20), (double)(var18 + 0.1F) - Block.Blocks[var19].BoundingBox.MinY, (double)(var17 + var21), 0.0D, 0.0D, 0.0D));
                        }
                        else
                        {
                            ++var14;
                            if (_random.NextInt(var14) == 0)
                            {
                                var8 = (double)(var16 + var20);
                                var10 = (double)(var18 + 0.1F) - Block.Blocks[var19].BoundingBox.MinY;
                                var12 = (double)(var17 + var21);
                            }

                            _client.particleManager.addEffect(new EntityRainFX(var3, (double)(var16 + var20), (double)(var18 + 0.1F) - Block.Blocks[var19].BoundingBox.MinY, (double)(var17 + var21)));
                        }
                    }
                }
            }

            if (var14 > 0 && _random.NextInt(3) < _rainSoundCounter++)
            {
                _rainSoundCounter = 0;
                if (var10 > var2.y + 1.0D && var3.getTopSolidBlockY(MathHelper.Floor(var2.x), MathHelper.Floor(var2.z)) > MathHelper.Floor(var2.y))
                {
                    _client.world.playSound(var8, var10, var12, "ambient.weather.rain", 0.1F, 0.5F);
                }
                else
                {
                    _client.world.playSound(var8, var10, var12, "ambient.weather.rain", 0.2F, 1.0F);
                }
            }

        }
    }

    protected void renderSnow(float tickDelta)
    {
        float var2 = _client.world.getRainGradient(tickDelta);
        if (var2 > 0.0F)
        {
            EntityLiving var3 = _client.camera;
            World var4 = _client.world;
            int var5 = MathHelper.Floor(var3.x);
            int var6 = MathHelper.Floor(var3.y);
            int var7 = MathHelper.Floor(var3.z);
            Tessellator var8 = Tessellator.instance;
            GLManager.GL.Disable(GLEnum.CullFace);
            GLManager.GL.Normal3(0.0F, 1.0F, 0.0F);
            GLManager.GL.Enable(GLEnum.Blend);
            GLManager.GL.BlendFunc(GLEnum.SrcAlpha, GLEnum.OneMinusSrcAlpha);
            GLManager.GL.AlphaFunc(GLEnum.Greater, 0.01F);
            _client.textureManager.BindTexture(_client.textureManager.GetTextureId("/environment/snow.png"));
            double var9 = var3.lastTickX + (var3.x - var3.lastTickX) * (double)tickDelta;
            double var11 = var3.lastTickY + (var3.y - var3.lastTickY) * (double)tickDelta;
            double var13 = var3.lastTickZ + (var3.z - var3.lastTickZ) * (double)tickDelta;
            int var15 = MathHelper.Floor(var11);
            byte var16 = 10;

            Biome[] var17 = var4.getBiomeSource().GetBiomesInArea(var5 - var16, var7 - var16, var16 * 2 + 1, var16 * 2 + 1);
            int var18 = 0;

            int var19;
            int var20;
            Biome var21;
            int var22;
            int var23;
            int var24;
            float var26;
            for (var19 = var5 - var16; var19 <= var5 + var16; ++var19)
            {
                for (var20 = var7 - var16; var20 <= var7 + var16; ++var20)
                {
                    var21 = var17[var18++];
                    if (var21.GetEnableSnow())
                    {
                        var22 = var4.getTopSolidBlockY(var19, var20);
                        if (var22 < 0)
                        {
                            var22 = 0;
                        }

                        var23 = var22;
                        if (var22 < var15)
                        {
                            var23 = var15;
                        }

                        var24 = var6 - var16;
                        int var25 = var6 + var16;
                        if (var24 < var22)
                        {
                            var24 = var22;
                        }

                        if (var25 < var22)
                        {
                            var25 = var22;
                        }

                        var26 = 1.0F;
                        if (var24 != var25)
                        {
                            _random.SetSeed(var19 * var19 * 3121 + var19 * 45238971 + var20 * var20 * 418711 + var20 * 13761);
                            float var27 = _ticks + tickDelta;
                            float var28 = ((_ticks & 511) + tickDelta) / 512.0F;
                            float var29 = _random.NextFloat() + var27 * 0.01F * (float)_random.NextGaussian();
                            float var30 = _random.NextFloat() + var27 * (float)_random.NextGaussian() * 0.001F;
                            double var31 = (double)(var19 + 0.5F) - var3.x;
                            double var33 = (double)(var20 + 0.5F) - var3.z;
                            float var35 = MathHelper.Sqrt(var31 * var31 + var33 * var33) / var16;
                            var8.startDrawingQuads();
                            float var36 = var4.getLuminance(var19, var23, var20);
                            GLManager.GL.Color4(var36, var36, var36, ((1.0F - var35 * var35) * 0.3F + 0.5F) * var2);
                            var8.setTranslationD(-var9 * 1.0D, -var11 * 1.0D, -var13 * 1.0D);
                            var8.addVertexWithUV(var19 + 0, var24, var20 + 0.5D, (double)(0.0F * var26 + var29), (double)(var24 * var26 / 4.0F + var28 * var26 + var30));
                            var8.addVertexWithUV(var19 + 1, var24, var20 + 0.5D, (double)(1.0F * var26 + var29), (double)(var24 * var26 / 4.0F + var28 * var26 + var30));
                            var8.addVertexWithUV(var19 + 1, var25, var20 + 0.5D, (double)(1.0F * var26 + var29), (double)(var25 * var26 / 4.0F + var28 * var26 + var30));
                            var8.addVertexWithUV(var19 + 0, var25, var20 + 0.5D, (double)(0.0F * var26 + var29), (double)(var25 * var26 / 4.0F + var28 * var26 + var30));
                            var8.addVertexWithUV(var19 + 0.5D, var24, var20 + 0, (double)(0.0F * var26 + var29), (double)(var24 * var26 / 4.0F + var28 * var26 + var30));
                            var8.addVertexWithUV(var19 + 0.5D, var24, var20 + 1, (double)(1.0F * var26 + var29), (double)(var24 * var26 / 4.0F + var28 * var26 + var30));
                            var8.addVertexWithUV(var19 + 0.5D, var25, var20 + 1, (double)(1.0F * var26 + var29), (double)(var25 * var26 / 4.0F + var28 * var26 + var30));
                            var8.addVertexWithUV(var19 + 0.5D, var25, var20 + 0, (double)(0.0F * var26 + var29), (double)(var25 * var26 / 4.0F + var28 * var26 + var30));
                            var8.setTranslationD(0.0D, 0.0D, 0.0D);
                            var8.draw();
                        }
                    }
                }
            }

            _client.textureManager.BindTexture(_client.textureManager.GetTextureId("/environment/rain.png"));
            var16 = 10;

            var18 = 0;

            for (var19 = var5 - var16; var19 <= var5 + var16; ++var19)
            {
                for (var20 = var7 - var16; var20 <= var7 + var16; ++var20)
                {
                    var21 = var17[var18++];
                    if (var21.CanSpawnLightningBolt())
                    {
                        var22 = var4.getTopSolidBlockY(var19, var20);
                        var23 = var6 - var16;
                        var24 = var6 + var16;
                        if (var23 < var22)
                        {
                            var23 = var22;
                        }

                        if (var24 < var22)
                        {
                            var24 = var22;
                        }

                        float var37 = 1.0F;
                        if (var23 != var24)
                        {
                            _random.SetSeed(var19 * var19 * 3121 + var19 * 45238971 + var20 * var20 * 418711 + var20 * 13761);
                            var26 = ((_ticks + var19 * var19 * 3121 + var19 * 45238971 + var20 * var20 * 418711 + var20 * 13761 & 31) + tickDelta) / 32.0F * (3.0F + _random.NextFloat());
                            double var38 = (double)(var19 + 0.5F) - var3.x;
                            double var39 = (double)(var20 + 0.5F) - var3.z;
                            float var40 = MathHelper.Sqrt(var38 * var38 + var39 * var39) / var16;
                            var8.startDrawingQuads();
                            float var32 = var4.getLuminance(var19, 128, var20) * 0.85F + 0.15F;
                            GLManager.GL.Color4(var32, var32, var32, ((1.0F - var40 * var40) * 0.5F + 0.5F) * var2);
                            var8.setTranslationD(-var9 * 1.0D, -var11 * 1.0D, -var13 * 1.0D);
                            var8.addVertexWithUV(var19 + 0, var23, var20 + 0.5D, (double)(0.0F * var37), (double)(var23 * var37 / 4.0F + var26 * var37));
                            var8.addVertexWithUV(var19 + 1, var23, var20 + 0.5D, (double)(1.0F * var37), (double)(var23 * var37 / 4.0F + var26 * var37));
                            var8.addVertexWithUV(var19 + 1, var24, var20 + 0.5D, (double)(1.0F * var37), (double)(var24 * var37 / 4.0F + var26 * var37));
                            var8.addVertexWithUV(var19 + 0, var24, var20 + 0.5D, (double)(0.0F * var37), (double)(var24 * var37 / 4.0F + var26 * var37));
                            var8.addVertexWithUV(var19 + 0.5D, var23, var20 + 0, (double)(0.0F * var37), (double)(var23 * var37 / 4.0F + var26 * var37));
                            var8.addVertexWithUV(var19 + 0.5D, var23, var20 + 1, (double)(1.0F * var37), (double)(var23 * var37 / 4.0F + var26 * var37));
                            var8.addVertexWithUV(var19 + 0.5D, var24, var20 + 1, (double)(1.0F * var37), (double)(var24 * var37 / 4.0F + var26 * var37));
                            var8.addVertexWithUV(var19 + 0.5D, var24, var20 + 0, (double)(0.0F * var37), (double)(var24 * var37 / 4.0F + var26 * var37));
                            var8.setTranslationD(0.0D, 0.0D, 0.0D);
                            var8.draw();
                        }
                    }
                }
            }

            GLManager.GL.Enable(GLEnum.CullFace);
            GLManager.GL.Disable(GLEnum.Blend);
            GLManager.GL.AlphaFunc(GLEnum.Greater, 0.1F);
        }
    }

    public void setupHudRender()
    {
        ScaledResolution var1 = new(_client.options, _client.displayWidth, _client.displayHeight);
        GLManager.GL.Clear(ClearBufferMask.DepthBufferBit);
        GLManager.GL.MatrixMode(GLEnum.Projection);
        GLManager.GL.LoadIdentity();
        GLManager.GL.Ortho(0.0D, var1.ScaledWidthDouble, var1.ScaledHeightDouble, 0.0D, 1000.0D, 3000.0D);
        GLManager.GL.MatrixMode(GLEnum.Modelview);
        GLManager.GL.LoadIdentity();
        GLManager.GL.Translate(0.0F, 0.0F, -2000.0F);
    }

    private void updateSkyAndFogColors(float tickDelta)
    {
        World var2 = _client.world;
        EntityLiving var3 = _client.camera;
        float var4 = 4.0F / _client.options.renderDistance;
        var4 = System.Math.Clamp(var4, 0.25f, 1.0f);
        var4 = 1.0F - (float)Math.Pow(var4, 0.25D);
        Vector3D<double> var5 = var2.getSkyColor(_client.camera, tickDelta);
        float var6 = (float)var5.X;
        float var7 = (float)var5.Y;
        float var8 = (float)var5.Z;
        Vector3D<double> var9 = var2.getFogColor(tickDelta);
        _fogColorRed = (float)var9.X;
        _fogColorGreen = (float)var9.Y;
        _fogColorBlue = (float)var9.Z;
        _fogColorRed += (var6 - _fogColorRed) * var4;
        _fogColorGreen += (var7 - _fogColorGreen) * var4;
        _fogColorBlue += (var8 - _fogColorBlue) * var4;
        float var10 = var2.getRainGradient(tickDelta);
        float var11;
        float var12;
        if (var10 > 0.0F)
        {
            var11 = 1.0F - var10 * 0.5F;
            var12 = 1.0F - var10 * 0.4F;
            _fogColorRed *= var11;
            _fogColorGreen *= var11;
            _fogColorBlue *= var12;
        }

        var11 = var2.getThunderGradient(tickDelta);
        if (var11 > 0.0F)
        {
            var12 = 1.0F - var11 * 0.5F;
            _fogColorRed *= var12;
            _fogColorGreen *= var12;
            _fogColorBlue *= var12;
        }

        if (_cloudFog)
        {
            Vector3D<double> var16 = var2.getCloudColor(tickDelta);
            _fogColorRed = (float)var16.X;
            _fogColorGreen = (float)var16.Y;
            _fogColorBlue = (float)var16.Z;
        }
        else if (var3.isInFluid(Material.Water))
        {
            _fogColorRed = 0.02F;
            _fogColorGreen = 0.02F;
            _fogColorBlue = 0.2F;
        }
        else if (var3.isInFluid(Material.Lava))
        {
            _fogColorRed = 0.6F;
            _fogColorGreen = 0.1F;
            _fogColorBlue = 0.0F;
        }

        var12 = cameraController.LastViewBob + (cameraController.ViewBob - cameraController.LastViewBob) * tickDelta;
        _fogColorRed *= var12;
        _fogColorGreen *= var12;
        _fogColorBlue *= var12;

        GLManager.GL.ClearColor(_fogColorRed, _fogColorGreen, _fogColorBlue, 0.0F);
    }

    private void applyFog(int mode)
    {
        EntityLiving var3 = _client.camera;
        GLManager.GL.Fog(GLEnum.FogColor, updateFogColorBuffer(_fogColorRed, _fogColorGreen, _fogColorBlue, 1.0F));
        _client.terrainRenderer.chunkRenderer.SetFogColor(_fogColorRed, _fogColorGreen, _fogColorBlue, 1.0f);
        GLManager.GL.Normal3(0.0F, -1.0F, 0.0F);
        GLManager.GL.Color4(1.0F, 1.0F, 1.0F, 1.0F);
        if (_cloudFog)
        {
            GLManager.GL.Fog(GLEnum.FogMode, (int)GLEnum.Exp);
            GLManager.GL.Fog(GLEnum.FogDensity, 0.1F);
            _client.terrainRenderer.chunkRenderer.SetFogMode(1);
            _client.terrainRenderer.chunkRenderer.SetFogDensity(0.1f);
        }
        else if (var3.isInFluid(Material.Water))
        {
            GLManager.GL.Fog(GLEnum.FogMode, (int)GLEnum.Exp);
            GLManager.GL.Fog(GLEnum.FogDensity, 0.1F);
            _client.terrainRenderer.chunkRenderer.SetFogMode(1);
            _client.terrainRenderer.chunkRenderer.SetFogDensity(0.1f);
        }
        else if (var3.isInFluid(Material.Lava))
        {
            GLManager.GL.Fog(GLEnum.FogMode, (int)GLEnum.Exp);
            GLManager.GL.Fog(GLEnum.FogDensity, 2.0F);
            _client.terrainRenderer.chunkRenderer.SetFogMode(1);
            _client.terrainRenderer.chunkRenderer.SetFogDensity(2.0f);
        }
        else
        {
            GLManager.GL.Fog(GLEnum.FogMode, (int)GLEnum.Linear);
            GLManager.GL.Fog(GLEnum.FogStart, _viewDistance * 0.25F);
            GLManager.GL.Fog(GLEnum.FogEnd, _viewDistance);
            _client.terrainRenderer.chunkRenderer.SetFogMode(0);
            _client.terrainRenderer.chunkRenderer.SetFogStart(_viewDistance * 0.25f);
            _client.terrainRenderer.chunkRenderer.SetFogEnd(_viewDistance);
            if (mode < 0)
            {
                GLManager.GL.Fog(GLEnum.FogStart, 0.0F);
                GLManager.GL.Fog(GLEnum.FogEnd, _viewDistance * 0.8F);
                _client.terrainRenderer.chunkRenderer.SetFogStart(0.0f);
                _client.terrainRenderer.chunkRenderer.SetFogEnd(_viewDistance * 0.8f);
            }

            if (_client.world.dimension.IsNether)
            {
                GLManager.GL.Fog(GLEnum.FogStart, 0.0F);
                _client.terrainRenderer.chunkRenderer.SetFogStart(0.0f);
            }
        }

        GLManager.GL.Enable(GLEnum.ColorMaterial);
        GLManager.GL.ColorMaterial(GLEnum.Front, GLEnum.Ambient);
    }

    private float[] updateFogColorBuffer(float var1, float var2, float var3, float var4)
    {
        _fogColorBuffer[0] = var1;
        _fogColorBuffer[1] = var2;
        _fogColorBuffer[2] = var3;
        _fogColorBuffer[3] = var4;
        return _fogColorBuffer;
    }
}
