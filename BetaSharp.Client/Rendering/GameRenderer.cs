using System.Diagnostics;
using BetaSharp.Blocks;
using BetaSharp.Blocks.Materials;
using BetaSharp.Client.Input;
using BetaSharp.Client.Options;
using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Core.Textures;
using BetaSharp.Client.Rendering.Items;
using BetaSharp.Entities;
using BetaSharp.Profiling;
using BetaSharp.Util.Hit;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;
using BetaSharp.Worlds.Generation.Biomes;
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

    private long _prevFrameTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    private readonly JavaRandom _random = new();
    private int _rainSoundCounter;
    private readonly float[] _fogColorBuffer = new float[16];
    private float _fogColorRed;
    private float _fogColorGreen;
    private float _fogColorBlue;
    private bool? _appliedVSyncState;

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
        if (_client.WorldRenderer != null)
        {
            _client.WorldRenderer.Tick(_client.Camera, var1);
        }
    }

    public void UpdateTargetedEntity(float tickDelta)
    {
        if (_client.Camera == null)
        {
            return;
        }

        if (_client.World == null)
        {
            return;
        }

        double reachDistance = (double)_client.PlayerController.getBlockReachDistance();
        _client.ObjectMouseOver = _client.Camera.rayTrace(reachDistance, tickDelta);
        Vec3D cameraPosition = _client.Camera.GetPosition(tickDelta);

        if (_client.ObjectMouseOver.Type != HitResultType.MISS)
        {
            reachDistance = _client.ObjectMouseOver.Pos.distanceTo(cameraPosition);
        }

        if (reachDistance > 3.0D)
        {
            reachDistance = 3.0D;
        }

        Vec3D lookVec = _client.Camera.getLook(tickDelta);
        Vec3D targetVec = cameraPosition + reachDistance * lookVec;
        _targetedEntity = null;

        float searchMargin = 1.0F;
        List<Entity> entities = _client.World.Entities.GetEntities(_client.Camera, _client.Camera.boundingBox.Stretch(lookVec.x * reachDistance, lookVec.y * reachDistance, lookVec.z * reachDistance).Expand((double)searchMargin, (double)searchMargin, (double)searchMargin));

        double closestDistance = 0.0D;
        for (int i = 0; i < entities.Count; ++i)
        {
            Entity ent = entities[i];
            if (ent.isCollidable())
            {
                float targetingMargin = ent.getTargetingMargin();
                Box box = ent.boundingBox.Expand((double)targetingMargin, (double)targetingMargin, (double)targetingMargin);
                HitResult hit = box.Raycast(cameraPosition, targetVec);

                if (box.Contains(cameraPosition))
                {
                    if (0.0D < closestDistance || closestDistance == 0.0D)
                    {
                        _targetedEntity = ent;
                        closestDistance = 0.0D;
                    }
                }
                else if (hit.Type != HitResultType.MISS)
                {
                    double var18 = cameraPosition.distanceTo(hit.Pos);
                    if (var18 < closestDistance || closestDistance == 0.0D)
                    {
                        _targetedEntity = ent;
                        closestDistance = var18;
                    }
                }
            }
        }

        if (_targetedEntity != null)
        {
            _client.ObjectMouseOver = new HitResult(_targetedEntity);
        }
    }


    private void renderWorld(float tickDelta)
    {
        _viewDistance = _client.Options.renderDistance * 16.0f;
        GLManager.GL.MatrixMode(GLEnum.Projection);
        GLManager.GL.LoadIdentity();

        if (cameraController.CameraZoom != 1.0D)
        {
            GLManager.GL.Translate((float)cameraController.CameraYaw, (float)-cameraController.CameraPitch, 0.0F);
            GLManager.GL.Scale(cameraController.CameraZoom, cameraController.CameraZoom, 1.0D);
            GLU.gluPerspective(cameraController.GetFov(tickDelta), _client.DisplayWidth / (float)_client.DisplayHeight, 0.05F, _viewDistance * 2.0F);
        }
        else
        {
            GLU.gluPerspective(cameraController.GetFov(tickDelta), _client.DisplayWidth / (float)_client.DisplayHeight, 0.05F, _viewDistance * 2.0F);
        }

        GLManager.GL.MatrixMode(GLEnum.Modelview);
        GLManager.GL.LoadIdentity();

        cameraController.ApplyDamageTiltEffect(tickDelta);
        if (_client.Options.ViewBobbing)
        {
            cameraController.ApplyViewBobbing(tickDelta);
        }

        float var4 = _client.Player.lastScreenDistortion + (_client.Player.changeDimensionCooldown - _client.Player.lastScreenDistortion) * tickDelta;
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

        GLU.gluPerspective(cameraController.GetFov(tickDelta, true), _client.DisplayWidth / (float)_client.DisplayHeight, 0.05F, _viewDistance * 2.0F);
        GLManager.GL.MatrixMode(GLEnum.Modelview);
        GLManager.GL.LoadIdentity();

        GLManager.GL.PushMatrix();
        cameraController.ApplyDamageTiltEffect(tickDelta);
        if (_client.Options.ViewBobbing)
        {
            cameraController.ApplyViewBobbing(tickDelta);
        }

        if (_client.Options.CameraMode == EnumCameraMode.FirstPerson && !_client.Camera.isSleeping() && !_client.Options.HideGUI)
        {
            itemRenderer.renderItemInFirstPerson(tickDelta);
        }

        GLManager.GL.PopMatrix();
        if (_client.Options.CameraMode == EnumCameraMode.FirstPerson && !_client.Camera.isSleeping())
        {
            itemRenderer.renderOverlays(tickDelta);
            cameraController.ApplyDamageTiltEffect(tickDelta);
        }

        if (_client.Options.ViewBobbing)
        {
            cameraController.ApplyViewBobbing(tickDelta);
        }
    }

    public void onFrameUpdate(float tickDelta)
    {
        if (!Display.isActive())
        {
            if (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - _prevFrameTime > 500L)
            {
                _client.DisplayInGameMenu();
            }
        }
        else
        {
            _prevFrameTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        if (_client.InGameHasFocus)
        {
            _client.MouseHelper.MouseXYChange();
            float var2 = _client.Options.MouseSensitivity * 0.6F + 0.2F;
            float var3 = var2 * var2 * var2 * 8.0F;
            float var4 = _client.MouseHelper.DeltaX * var3;
            float var5 = _client.MouseHelper.DeltaY * var3;

            bool zoomHeldForSensitivity = _client.CurrentScreen == null && _client.InGameHasFocus && Keyboard.isKeyDown(_client.Options.KeyBindZoom.keyCode);
            if (zoomHeldForSensitivity)
            {
                float zoomProgress = 1.0F / System.Math.Clamp(_client.Options.ZoomScale, 1.25F, 20.0F);
                float sensitivityFloor = 0.4F;
                float zoomSensitivityMultiplier = sensitivityFloor + (1.0F - sensitivityFloor) * zoomProgress;
                var4 *= zoomSensitivityMultiplier;
                var5 *= zoomSensitivityMultiplier;
            }

            ControllerManager.HandleLook(ref var4, ref var5, var3, _client.Timer.DeltaTime);
            int var6 = -1;
            if (_client.Options.InvertMouse)
            {
                var6 = 1;
            }

            if (_client.Options.SmoothCamera)
            {
                var4 = _mouseFilterXAxis.Smooth(var4, 0.05F * var3);
                var5 = _mouseFilterYAxis.Smooth(var5, 0.05F * var3);
            }
            _client.Player.changeLookDirection(var4, var5 * var6);
        }

        bool zoomHeld = (_client.CurrentScreen == null && _client.InGameHasFocus && Keyboard.isKeyDown(_client.Options.KeyBindZoom.keyCode)) || ControllerManager.IsZoomHeld();
        cameraController.SetZoomState(zoomHeld, _client.Options.ZoomScale);

        if (!_client.SkipRenderWorld)
        {
            ScaledResolution var13 = new(_client.Options, _client.DisplayWidth, _client.DisplayHeight);
            int scaledWidth = var13.ScaledWidth;
            int scaledHeight = var13.ScaledHeight;
            int scaledMouseX;
            int scaledMouseY;
            int vpOffsetX = (int)_client.DebugViewportOffset.X;
            int vpOffsetY = (int)_client.DebugViewportOffset.Y;
            if (_client.IsControllerMode)
            {
                scaledMouseX = (int)(_client.VirtualCursor.X * scaledWidth / _client.DisplayWidth);
                scaledMouseY = (int)(_client.VirtualCursor.Y * scaledHeight / _client.DisplayHeight);
            }
            else
            {
                scaledMouseX = (Mouse.getX() - vpOffsetX) * scaledWidth / _client.DisplayWidth;
                scaledMouseY = scaledHeight - (Mouse.getY() - vpOffsetY) * scaledHeight / _client.DisplayHeight - 1;
            }
            int var7 = 30 + (int)(_client.Options.LimitFramerate * 210.0f);
            bool desiredVSync = _client.Options.VSync && var7 >= 240;

            if (_appliedVSyncState != desiredVSync)
            {
                Display.setVSyncEnabled(desiredVSync);
                _appliedVSyncState = desiredVSync;
            }

            _client.FramebufferManager.Begin();

            if (_client.World != null)
            {
                using (Profiler.Begin("RenderWorld"))
                {
                    renderFrame(tickDelta, 0L);
                }

                using (Profiler.Begin("RenderGameOverlay"))
                {
                    if (!_client.Options.HideGUI || _client.CurrentScreen != null)
                    {
                        setupHudRender();
                        _client.HUD.Render(scaledMouseX, scaledMouseY, tickDelta);
                    }
                }
            }
            else
            {
                GLManager.GL.Viewport(0, 0, (uint)_client.FramebufferManager.FramebufferWidth, (uint)_client.FramebufferManager.FramebufferHeight);
                GLManager.GL.MatrixMode(GLEnum.Projection);
                GLManager.GL.LoadIdentity();
                GLManager.GL.MatrixMode(GLEnum.Modelview);
                GLManager.GL.LoadIdentity();
                setupHudRender();
            }

            if (_client.CurrentScreen != null)
            {
                GLManager.GL.Clear(ClearBufferMask.DepthBufferBit);
                setupHudRender();
                _client.CurrentScreen.Render(scaledMouseX, scaledMouseY, tickDelta);

                if (_client.IsControllerMode)
                {
                    DrawVirtualCursor(scaledMouseX, scaledMouseY);
                }
            }


            _client.FramebufferManager.End();


            if (var7 < 240)
            {
                //frametime in milliseconds
                double targetMs = 1000.0 / var7;

                double elapsedMs = _fpsTimer.Elapsed.TotalMilliseconds;
                double waitTime = targetMs - elapsedMs;

                if (waitTime > 0)
                {
                    while (true)
                    {
                        double remainingMs = targetMs - _fpsTimer.Elapsed.TotalMilliseconds;
                        if (remainingMs <= 0)
                        {
                            break;
                        }

                        if (remainingMs > 2.0)
                        {
                            Thread.Sleep(1);
                        }
                        else
                        {
                            Thread.Yield();
                        }
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

        using (Profiler.Begin("GetMouseOver"))
        {
            UpdateTargetedEntity(tickDelta);
        }

        EntityLiving entity = _client.Camera;
        WorldRenderer worldRenderer = _client.WorldRenderer;
        ParticleManager particleManager = _client.ParticleManager;
        double entX = entity.lastTickX + (entity.x - entity.lastTickX) * (double)tickDelta;
        double entY = entity.lastTickY + (entity.y - entity.lastTickY) * (double)tickDelta;
        double entZ = entity.lastTickZ + (entity.z - entity.lastTickZ) * (double)tickDelta;

        using (Profiler.Begin("UpdateFog"))
        {
            GLManager.GL.Viewport(0, 0, (uint)_client.FramebufferManager.FramebufferWidth, (uint)_client.FramebufferManager.FramebufferHeight);
            updateSkyAndFogColors(tickDelta);
        }
        GLManager.GL.Clear(ClearBufferMask.DepthBufferBit | ClearBufferMask.ColorBufferBit);
        GLManager.GL.Enable(GLEnum.CullFace);
        renderWorld(tickDelta);
        Frustum.Instance();
        if (_client.Options.renderDistance >= 8)
        {
            applyFog(-1);
            worldRenderer.RenderSky(tickDelta);
        }

        GLManager.GL.Enable(GLEnum.Fog);
        applyFog(1);

        FrustrumCuller frustrumCuller = new();
        frustrumCuller.SetPosition(entX, entY, entZ);

        applyFog(0);
        GLManager.GL.Enable(GLEnum.Fog);
        _client.TextureManager.BindTexture(_client.TextureManager.GetTextureId("/terrain.png"));
        Lighting.turnOff();

        using (Profiler.Begin("SortAndRender"))
        {
            worldRenderer.SortAndRender(entity, 0, (double)tickDelta, frustrumCuller);
        }

        GLManager.GL.ShadeModel(GLEnum.Flat);
        Lighting.turnOn();

        using (Profiler.Begin("RenderEntities"))
        {
            worldRenderer.RenderEntities(entity.GetPosition(tickDelta), frustrumCuller, tickDelta);
        }

        particleManager.renderSpecialParticles(entity, tickDelta);

        Lighting.turnOff();
        applyFog(0);

        using (Profiler.Begin("RenderParticles"))
        {
            particleManager.renderParticles(entity, tickDelta);
        }

        EntityPlayer entityPlayer = default;
        if (_client.ObjectMouseOver.Type != HitResultType.MISS && entity.isInFluid(Material.Water) && entity is EntityPlayer)
        {
            entityPlayer = (EntityPlayer)entity;
            GLManager.GL.Disable(GLEnum.AlphaTest);
            worldRenderer.DrawBlockBreaking(entityPlayer, _client.ObjectMouseOver, entityPlayer.inventory.GetItemInHand(), tickDelta);
            worldRenderer.DrawSelectionBox(entityPlayer, _client.ObjectMouseOver, 0, entityPlayer.inventory.GetItemInHand(), tickDelta);
            GLManager.GL.Enable(GLEnum.AlphaTest);
        }

        GLManager.GL.BlendFunc(GLEnum.SrcAlpha, GLEnum.OneMinusSrcAlpha);
        applyFog(0);
        GLManager.GL.Enable(GLEnum.Blend);
        GLManager.GL.Disable(GLEnum.CullFace);
        _client.TextureManager.BindTexture(_client.TextureManager.GetTextureId("/terrain.png"));

        using (Profiler.Begin("SortAndRenderTranslucent"))
        {
            worldRenderer.SortAndRender(entity, 1, tickDelta, frustrumCuller);

            GLManager.GL.ShadeModel(GLEnum.Flat);
        }

        //TODO: SELCTION BOX/BLOCK BREAKING VISUALIZATON DON'T APPEAR PROPERLY MOST OF THE TIME, SAME WITH ENTITY SHADOWS. VIEW BOBBING MAKES ENTITES BOB UP AND DOWN

        GLManager.GL.DepthMask(true);
        GLManager.GL.Enable(GLEnum.CullFace);
        GLManager.GL.Disable(GLEnum.Blend);
        if (!cameraController.IsZoomActive && entity is EntityPlayer && _client.ObjectMouseOver.Type != HitResultType.MISS && !entity.isInFluid(Material.Water))
        {
            entityPlayer = (EntityPlayer)entity;
            GLManager.GL.Disable(GLEnum.AlphaTest);
            worldRenderer.DrawBlockBreaking(entityPlayer, _client.ObjectMouseOver, entityPlayer.inventory.GetItemInHand(), tickDelta);
            worldRenderer.DrawSelectionBox(entityPlayer, _client.ObjectMouseOver, 0, entityPlayer.inventory.GetItemInHand(), tickDelta);
            GLManager.GL.Enable(GLEnum.AlphaTest);
        }

        renderSnow(tickDelta);
        GLManager.GL.Disable(GLEnum.Fog);
        if (_targetedEntity != null)
        {
        }

        applyFog(0);
        GLManager.GL.Enable(GLEnum.Fog);

        if (_client.ShowChunkBorders)
        {
            renderChunkBorders(tickDelta);
        }

        worldRenderer.RenderClouds(tickDelta);
        GLManager.GL.Disable(GLEnum.Fog);
        applyFog(1);

        if (!cameraController.IsZoomActive)
        {
            GLManager.GL.Clear(ClearBufferMask.DepthBufferBit);
            renderFirstPersonHand(tickDelta);
        }
    }

    private void renderChunkBorders(float tickDelta)
    {
        EntityLiving camera = _client.Camera;
        double camX = camera.lastTickX + (camera.x - camera.lastTickX) * tickDelta;
        double camY = camera.lastTickY + (camera.y - camera.lastTickY) * tickDelta;
        double camZ = camera.lastTickZ + (camera.z - camera.lastTickZ) * tickDelta;

        int playerChunkX = _client.Player.chunkX;
        int playerChunkZ = _client.Player.chunkZ;

        GLManager.GL.MatrixMode(GLEnum.Modelview);
        GLManager.GL.PushMatrix();
        GLManager.GL.Translate((float)-camX, (float)-camY, (float)-camZ);

        GLManager.GL.Disable(GLEnum.Texture2D);
        GLManager.GL.Disable(GLEnum.Lighting);
        GLManager.GL.Disable(GLEnum.Fog);
        GLManager.GL.Enable(GLEnum.DepthTest);
        GLManager.GL.DepthMask(true);

        double minX = playerChunkX * 16.0;
        double maxX = (playerChunkX + 1) * 16.0;
        double minZ = playerChunkZ * 16.0;
        double maxZ = (playerChunkZ + 1) * 16.0;

        Tessellator tess = Tessellator.instance;
        tess.startDrawing(1);

        tess.setColorRGBA_F(1.0F, 1.0F, 0.0F, 1.0F);

        for (int i = 0; i <= 16; i += 4)
        {
            double x = minX + i;
            double z = minZ + i;

            tess.addVertex(x, 0.0, minZ);
            tess.addVertex(x, 128.0, minZ);

            tess.addVertex(x, 0.0, maxZ);
            tess.addVertex(x, 128.0, maxZ);

            tess.addVertex(minX, 0.0, z);
            tess.addVertex(minX, 128.0, z);

            tess.addVertex(maxX, 0.0, z);
            tess.addVertex(maxX, 128.0, z);
        }

        for (int y = 0; y <= 128; y += 4)
        {
            if (y % 16 == 0) tess.setColorRGBA_F(0.0F, 0.0F, 1.0F, 1.0F);
            tess.addVertex(minX, y, minZ);
            tess.addVertex(minX, y, maxZ);

            tess.addVertex(maxX, y, minZ);
            tess.addVertex(maxX, y, maxZ);

            tess.addVertex(minX, y, minZ);
            tess.addVertex(maxX, y, minZ);

            tess.addVertex(minX, y, maxZ);
            tess.addVertex(maxX, y, maxZ);
            if (y % 16 == 0) tess.setColorRGBA_F(1.0F, 1.0F, 0.0F, 1.0F);
        }

        minX = (playerChunkX - 1) * 16.0;
        maxX = (playerChunkX + 2) * 16.0;
        minZ = (playerChunkZ - 1) * 16.0;
        maxZ = (playerChunkZ + 2) * 16.0;

        tess.setColorRGBA_F(1.0F, 0.0F, 0.0F, 1.0F);

        for (int i = 0; i < 4; i++)
        {
            double x = minX + (i * 16);
            double z = minZ + (i * 16);

            tess.addVertex(x, 0.0, minZ);
            tess.addVertex(x, 128.0, minZ);

            tess.addVertex(x, 0.0, maxZ);
            tess.addVertex(x, 128.0, maxZ);

            tess.addVertex(minX, 0.0, z);
            tess.addVertex(minX, 128.0, z);

            tess.addVertex(maxX, 0.0, z);
            tess.addVertex(maxX, 128.0, z);
        }

        tess.draw();
        GLManager.GL.PopMatrix();
        GLManager.GL.Enable(GLEnum.Texture2D);
    }

    private void renderRain()
    {
        float var1 = _client.World.Environment.GetRainGradient(1.0F);

        if (var1 != 0.0F)
        {
            _random.SetSeed(_ticks * 312987231L);
            EntityLiving var2 = _client.Camera;
            World var3 = _client.World;
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
                int var18 = var3.Reader.GetTopSolidBlockY(var16, var17);
                int var19 = var3.Reader.GetBlockId(var16, var18 - 1, var17);
                if (var18 <= var5 + var7 && var18 >= var5 - var7 && var3.GetBiomeSource().GetBiome(var16, var17).CanSpawnLightningBolt())
                {
                    float var20 = _random.NextFloat();
                    float var21 = _random.NextFloat();
                    if (var19 > 0)
                    {
                        if (Block.Blocks[var19].material == Material.Lava)
                        {
                            _client.ParticleManager.AddSmoke(var16 + var20, var18 + 0.1F - Block.Blocks[var19].BoundingBox.MinY, var17 + var21, 0.0, 0.0, 0.0);
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

                            _client.ParticleManager.AddRain(var16 + var20, var18 + 0.1F - Block.Blocks[var19].BoundingBox.MinY, var17 + var21);
                        }
                    }
                }
            }

            if (var14 > 0 && _random.NextInt(3) < _rainSoundCounter++)
            {
                _rainSoundCounter = 0;
                if (var10 > var2.y + 1.0D && var3.Reader.GetTopSolidBlockY(MathHelper.Floor(var2.x), MathHelper.Floor(var2.z)) > MathHelper.Floor(var2.y))
                {
                    _client.World.Broadcaster.PlaySoundAtPos(var8, var10, var12, "ambient.weather.rain", 0.1F, 0.5F);
                }
                else
                {
                    _client.World.Broadcaster.PlaySoundAtPos(var8, var10, var12, "ambient.weather.rain", 0.2F, 1.0F);
                }
            }
        }
    }

    protected void renderSnow(float tickDelta)
    {
        float var2 = _client.World.Environment.GetRainGradient(tickDelta);
        if (var2 > 0.0F)
        {
            EntityLiving var3 = _client.Camera;
            World var4 = _client.World;
            int var5 = MathHelper.Floor(var3.x);
            int var6 = MathHelper.Floor(var3.y);
            int var7 = MathHelper.Floor(var3.z);
            Tessellator var8 = Tessellator.instance;
            GLManager.GL.Disable(GLEnum.CullFace);
            GLManager.GL.Normal3(0.0F, 1.0F, 0.0F);
            GLManager.GL.Enable(GLEnum.Blend);
            GLManager.GL.BlendFunc(GLEnum.SrcAlpha, GLEnum.OneMinusSrcAlpha);
            GLManager.GL.AlphaFunc(GLEnum.Greater, 0.01F);
            _client.TextureManager.BindTexture(_client.TextureManager.GetTextureId("/environment/snow.png"));
            double var9 = var3.lastTickX + (var3.x - var3.lastTickX) * (double)tickDelta;
            double var11 = var3.lastTickY + (var3.y - var3.lastTickY) * (double)tickDelta;
            double var13 = var3.lastTickZ + (var3.z - var3.lastTickZ) * (double)tickDelta;
            int var15 = MathHelper.Floor(var11);
            byte var16 = 10;

            Biome[] var17 = var4.GetBiomeSource().GetBiomesInArea(var5 - var16, var7 - var16, var16 * 2 + 1, var16 * 2 + 1);
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
                        var22 = var4.Reader.GetTopSolidBlockY(var19, var20);
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
                            float var36 = var4.GetLuminance(var19, var23, var20);
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

            _client.TextureManager.BindTexture(_client.TextureManager.GetTextureId("/environment/rain.png"));
            var16 = 10;

            var18 = 0;

            for (var19 = var5 - var16; var19 <= var5 + var16; ++var19)
            {
                for (var20 = var7 - var16; var20 <= var7 + var16; ++var20)
                {
                    var21 = var17[var18++];
                    if (var21.CanSpawnLightningBolt())
                    {
                        var22 = var4.Reader.GetTopSolidBlockY(var19, var20);
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
                            float var32 = var4.GetLuminance(var19, 128, var20) * 0.85F + 0.15F;
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
        ScaledResolution sr = new(_client.Options, _client.DisplayWidth, _client.DisplayHeight);
        GLManager.GL.Clear(ClearBufferMask.DepthBufferBit);
        GLManager.GL.MatrixMode(GLEnum.Projection);
        GLManager.GL.LoadIdentity();
        GLManager.GL.Ortho(0.0D, sr.ScaledWidthDouble, sr.ScaledHeightDouble, 0.0D, 1000.0D, 3000.0D);
        GLManager.GL.MatrixMode(GLEnum.Modelview);
        GLManager.GL.LoadIdentity();
        GLManager.GL.Translate(0.0F, 0.0F, -2000.0F);
    }

    public void DrawVirtualCursor(int x, int y)
    {
        if (_client.IsControllerMode && _client.CurrentScreen?.IsEditingSlider != true)
        {
            GLManager.GL.Disable(GLEnum.Lighting);
            GLManager.GL.Disable(GLEnum.DepthTest);
            GLManager.GL.Enable(GLEnum.Blend);
            GLManager.GL.BlendFunc(GLEnum.SrcAlpha, GLEnum.OneMinusSrcAlpha);
            GLManager.GL.Color4(1.0f, 1.0f, 1.0f, 1.0f);

            TextureHandle textureId = _client.TextureManager.GetTextureId("/gui/Pointer.png");
            _client.TextureManager.BindTexture(textureId);

            const int width = 32;
            const int height = 32;

            x -= width / 2;
            y -= height / 2;

            const float zLevel = 10.0f;
            Tessellator tess = Tessellator.instance;
            tess.startDrawingQuads();
            tess.addVertexWithUV(x, y + height, zLevel, 0.0, 1.0);
            tess.addVertexWithUV(x + width, y + height, zLevel, 1.0, 1.0);
            tess.addVertexWithUV(x + width, y, zLevel, 1.0, 0.0);
            tess.addVertexWithUV(x, y, zLevel, 0.0, 0.0);
            tess.draw();

            GLManager.GL.Disable(GLEnum.Blend);
            GLManager.GL.Enable(GLEnum.DepthTest);
        }
    }

    private void updateSkyAndFogColors(float tickDelta)
    {
        World var2 = _client.World;
        EntityLiving var3 = _client.Camera;
        float var4 = 4.0F / _client.Options.renderDistance;
        var4 = System.Math.Clamp(var4, 0.25f, 1.0f);
        var4 = 1.0F - (float)Math.Pow(var4, 0.25D);
        Vector3D<double> var5 = var2.Environment.GetSkyColor(_client.Camera, tickDelta);
        float var6 = (float)var5.X;
        float var7 = (float)var5.Y;
        float var8 = (float)var5.Z;
        Vector3D<double> var9 = var2.GetFogColor(tickDelta);
        _fogColorRed = (float)var9.X;
        _fogColorGreen = (float)var9.Y;
        _fogColorBlue = (float)var9.Z;
        _fogColorRed += (var6 - _fogColorRed) * var4;
        _fogColorGreen += (var7 - _fogColorGreen) * var4;
        _fogColorBlue += (var8 - _fogColorBlue) * var4;
        float var10 = var2.Environment.GetRainGradient(tickDelta);
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

        var11 = var2.Environment.GetThunderGradient(tickDelta);
        if (var11 > 0.0F)
        {
            var12 = 1.0F - var11 * 0.5F;
            _fogColorRed *= var12;
            _fogColorGreen *= var12;
            _fogColorBlue *= var12;
        }

        if (_cloudFog)
        {
            Vector3D<double> var16 = var2.Environment.GetCloudColor(tickDelta);
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
        EntityLiving var3 = _client.Camera;
        GLManager.GL.Fog(GLEnum.FogColor, updateFogColorBuffer(_fogColorRed, _fogColorGreen, _fogColorBlue, 1.0F));
        _client.WorldRenderer.ChunkRenderer.SetFogColor(_fogColorRed, _fogColorGreen, _fogColorBlue, 1.0f);
        GLManager.GL.Normal3(0.0F, -1.0F, 0.0F);
        GLManager.GL.Color4(1.0F, 1.0F, 1.0F, 1.0F);
        if (_cloudFog)
        {
            GLManager.GL.Fog(GLEnum.FogMode, (int)GLEnum.Exp);
            GLManager.GL.Fog(GLEnum.FogDensity, 0.1F);
            _client.WorldRenderer.ChunkRenderer.SetFogMode(1);
            _client.WorldRenderer.ChunkRenderer.SetFogDensity(0.1f);
        }
        else if (var3.isInFluid(Material.Water))
        {
            GLManager.GL.Fog(GLEnum.FogMode, (int)GLEnum.Exp);
            GLManager.GL.Fog(GLEnum.FogDensity, 0.1F);
            _client.WorldRenderer.ChunkRenderer.SetFogMode(1);
            _client.WorldRenderer.ChunkRenderer.SetFogDensity(0.1f);
        }
        else if (var3.isInFluid(Material.Lava))
        {
            GLManager.GL.Fog(GLEnum.FogMode, (int)GLEnum.Exp);
            GLManager.GL.Fog(GLEnum.FogDensity, 2.0F);
            _client.WorldRenderer.ChunkRenderer.SetFogMode(1);
            _client.WorldRenderer.ChunkRenderer.SetFogDensity(2.0f);
        }
        else
        {
            GLManager.GL.Fog(GLEnum.FogMode, (int)GLEnum.Linear);
            GLManager.GL.Fog(GLEnum.FogStart, _viewDistance * 0.25F);
            GLManager.GL.Fog(GLEnum.FogEnd, _viewDistance);
            _client.WorldRenderer.ChunkRenderer.SetFogMode(0);
            _client.WorldRenderer.ChunkRenderer.SetFogStart(_viewDistance * 0.25f);
            _client.WorldRenderer.ChunkRenderer.SetFogEnd(_viewDistance);
            if (mode < 0)
            {
                GLManager.GL.Fog(GLEnum.FogStart, 0.0F);
                GLManager.GL.Fog(GLEnum.FogEnd, _viewDistance * 0.8F);
                _client.WorldRenderer.ChunkRenderer.SetFogStart(0.0f);
                _client.WorldRenderer.ChunkRenderer.SetFogEnd(_viewDistance * 0.8f);
            }

            if (_client.World.Dimension.IsNether)
            {
                GLManager.GL.Fog(GLEnum.FogStart, 0.0F);
                _client.WorldRenderer.ChunkRenderer.SetFogStart(0.0f);
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
