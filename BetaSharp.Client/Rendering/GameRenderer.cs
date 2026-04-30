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

    public void tick(float tickDelta)
    {
        if (_client.WorldRenderer != null)
        {
            _client.WorldRenderer.Tick(_client.Camera, tickDelta);
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
        _client.ObjectMouseOver = _client.Camera.RayTrace(reachDistance, tickDelta);
        Vec3D cameraPosition = _client.Camera.GetPosition(tickDelta);

        if (_client.ObjectMouseOver.Type != HitResultType.MISS)
        {
            reachDistance = _client.ObjectMouseOver.Pos.distanceTo(cameraPosition);
        }

        if (reachDistance > 3.0D)
        {
            reachDistance = 3.0D;
        }

        Vec3D lookVec = _client.Camera.GetLook(tickDelta);
        Vec3D targetVec = cameraPosition + reachDistance * lookVec;
        _targetedEntity = null;

        float searchMargin = 1.0F;
        List<Entity> entities = _client.World.Entities.GetEntities(_client.Camera, _client.Camera.BoundingBox.Stretch(lookVec.x * reachDistance, lookVec.y * reachDistance, lookVec.z * reachDistance).Expand((double)searchMargin, (double)searchMargin, (double)searchMargin));

        double closestDistance = 0.0D;
        for (int i = 0; i < entities.Count; ++i)
        {
            Entity ent = entities[i];
            if (ent.HasCollision)
            {
                float targetingMargin = ent.TargetingMargin;
                Box box = ent.BoundingBox.Expand((double)targetingMargin, (double)targetingMargin, (double)targetingMargin);
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
                    double hitDistance = cameraPosition.distanceTo(hit.Pos);
                    if (hitDistance < closestDistance || closestDistance == 0.0D)
                    {
                        _targetedEntity = ent;
                        closestDistance = hitDistance;
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

        float screenDistortion = _client.Player.LastScreenDistortion + (_client.Player.ChangeDimensionCooldown - _client.Player.LastScreenDistortion) * tickDelta;
        if (screenDistortion > 0.0F)
        {
            float distortionScale = 5.0F / (screenDistortion * screenDistortion + 5.0F) - screenDistortion * 0.04F;
            distortionScale *= distortionScale;
            GLManager.GL.Rotate((_ticks + tickDelta) * 20.0F, 0.0F, 1.0F, 1.0F);
            GLManager.GL.Scale(1.0F / distortionScale, 1.0F, 1.0F);
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

        if (_client.Options.CameraMode == CameraMode.FirstPerson && !_client.Camera.IsSleeping && !_client.Options.HideGUI)
        {
            itemRenderer.renderItemInFirstPerson(tickDelta);
        }

        GLManager.GL.PopMatrix();
        if (_client.Options.CameraMode == CameraMode.FirstPerson && !_client.Camera.IsSleeping)
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
            float baseSensitivity = _client.Options.MouseSensitivity * 0.6F + 0.2F;
            float lookScale = baseSensitivity * baseSensitivity * baseSensitivity * 8.0F;
            float yawDelta = _client.MouseHelper.DeltaX * lookScale;
            float pitchDelta = _client.MouseHelper.DeltaY * lookScale;

            bool zoomHeldForSensitivity = _client.CurrentScreen == null && _client.InGameHasFocus && Keyboard.isKeyDown(_client.Options.KeyBindZoom.scanCode);
            if (zoomHeldForSensitivity)
            {
                float zoomProgress = 1.0F / Math.Clamp(_client.Options.ZoomScale, 1.25F, 20.0F);
                float sensitivityFloor = 0.4F;
                float zoomSensitivityMultiplier = sensitivityFloor + (1.0F - sensitivityFloor) * zoomProgress;
                yawDelta *= zoomSensitivityMultiplier;
                pitchDelta *= zoomSensitivityMultiplier;
            }

            ControllerManager.HandleLook(ref yawDelta, ref pitchDelta, lookScale, _client.Timer.DeltaTime);
            int invertMultiplier = -1;
            if (_client.Options.InvertMouse)
            {
                invertMultiplier = 1;
            }

            if (_client.Options.SmoothCamera)
            {
                yawDelta = _mouseFilterXAxis.Smooth(yawDelta, 0.05F * lookScale);
                pitchDelta = _mouseFilterYAxis.Smooth(pitchDelta, 0.05F * lookScale);
            }
            _client.Player.ChangeLookDirection(yawDelta, pitchDelta * invertMultiplier);
        }

        bool zoomHeld = (_client.CurrentScreen == null && _client.InGameHasFocus && Keyboard.isKeyDown(_client.Options.KeyBindZoom.scanCode)) || ControllerManager.IsZoomHeld();
        cameraController.SetZoomState(zoomHeld, _client.Options.ZoomScale);

        if (!_client.SkipRenderWorld)
        {
            ScaledResolution scaledResolution = new(_client.Options, _client.DisplayWidth, _client.DisplayHeight);
            int scaledWidth = scaledResolution.ScaledWidth;
            int scaledHeight = scaledResolution.ScaledHeight;
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
            int targetFps = 30 + (int)(_client.Options.LimitFramerate * 210.0f);
            bool desiredVSync = _client.Options.VSync && targetFps >= 240;

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


            if (targetFps < 240)
            {
                //frametime in milliseconds
                double targetMs = 1000.0 / targetFps;

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
        double entX = entity.LastTickX + (entity.X - entity.LastTickX) * (double)tickDelta;
        double entY = entity.LastTickY + (entity.Y - entity.LastTickY) * (double)tickDelta;
        double entZ = entity.LastTickZ + (entity.Z - entity.LastTickZ) * (double)tickDelta;

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
        if (_client.ObjectMouseOver.Type != HitResultType.MISS && entity.IsInFluid(Material.Water) && entity is EntityPlayer)
        {
            entityPlayer = (EntityPlayer)entity;
            GLManager.GL.Disable(GLEnum.AlphaTest);
            worldRenderer.DrawBlockBreaking(entityPlayer, _client.ObjectMouseOver, entityPlayer.Inventory.ItemInHand, tickDelta);
            worldRenderer.DrawSelectionBox(entityPlayer, _client.ObjectMouseOver, 0, entityPlayer.Inventory.ItemInHand, tickDelta);
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
        if (!cameraController.IsZoomActive && entity is EntityPlayer && _client.ObjectMouseOver.Type != HitResultType.MISS && !entity.IsInFluid(Material.Water))
        {
            entityPlayer = (EntityPlayer)entity;
            GLManager.GL.Disable(GLEnum.AlphaTest);
            worldRenderer.DrawBlockBreaking(entityPlayer, _client.ObjectMouseOver, entityPlayer.Inventory.ItemInHand, tickDelta);
            worldRenderer.DrawSelectionBox(entityPlayer, _client.ObjectMouseOver, 0, entityPlayer.Inventory.ItemInHand, tickDelta);
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
        double camX = camera.LastTickX + (camera.X - camera.LastTickX) * tickDelta;
        double camY = camera.LastTickY + (camera.Y - camera.LastTickY) * tickDelta;
        double camZ = camera.LastTickZ + (camera.Z - camera.LastTickZ) * tickDelta;

        int playerChunkX = _client.Player.ChunkX;
        int playerChunkZ = _client.Player.ChunkZ;

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
        float rainGradient = _client.World.Environment.GetRainGradient(1.0F);

        if (rainGradient != 0.0F)
        {
            _random.SetSeed(_ticks * 312987231L);
            EntityLiving camera = _client.Camera;
            World world = _client.World;
            int cameraBlockX = MathHelper.Floor(camera.X);
            int cameraBlockY = MathHelper.Floor(camera.Y);
            int cameraBlockZ = MathHelper.Floor(camera.Z);
            byte searchRadius = 10;
            double rainSoundX = 0.0D;
            double rainSoundY = 0.0D;
            double rainSoundZ = 0.0D;
            int validDropCount = 0;

            for (int sampleIndex = 0; sampleIndex < (int)(100.0F * rainGradient * rainGradient); ++sampleIndex)
            {
                int sampleX = cameraBlockX + _random.NextInt(searchRadius) - _random.NextInt(searchRadius);
                int sampleZ = cameraBlockZ + _random.NextInt(searchRadius) - _random.NextInt(searchRadius);
                int topSolidY = world.Reader.GetTopSolidBlockY(sampleX, sampleZ);
                int blockBelowId = world.Reader.GetBlockId(sampleX, topSolidY - 1, sampleZ);
                if (topSolidY <= cameraBlockY + searchRadius && topSolidY >= cameraBlockY - searchRadius && world.GetBiomeSource().GetBiome(sampleX, sampleZ).CanSpawnLightningBolt())
                {
                    float xOffset = _random.NextFloat();
                    float zOffset = _random.NextFloat();
                    if (blockBelowId > 0)
                    {
                        if (Block.Blocks[blockBelowId].material == Material.Lava)
                        {
                            _client.ParticleManager.AddSmoke(sampleX + xOffset, topSolidY + 0.1F - Block.Blocks[blockBelowId].BoundingBox.MinY, sampleZ + zOffset, 0.0, 0.0, 0.0);
                        }
                        else
                        {
                            ++validDropCount;
                            if (_random.NextInt(validDropCount) == 0)
                            {
                                rainSoundX = (double)(sampleX + xOffset);
                                rainSoundY = (double)(topSolidY + 0.1F) - Block.Blocks[blockBelowId].BoundingBox.MinY;
                                rainSoundZ = (double)(sampleZ + zOffset);
                            }

                            _client.ParticleManager.AddRain(sampleX + xOffset, topSolidY + 0.1F - Block.Blocks[blockBelowId].BoundingBox.MinY, sampleZ + zOffset);
                        }
                    }
                }
            }

            if (validDropCount > 0 && _random.NextInt(3) < _rainSoundCounter++)
            {
                _rainSoundCounter = 0;
                if (rainSoundY > camera.Y + 1.0D && world.Reader.GetTopSolidBlockY(MathHelper.Floor(camera.X), MathHelper.Floor(camera.Z)) > MathHelper.Floor(camera.Y))
                {
                    _client.World.Broadcaster.PlaySoundAtPos(rainSoundX, rainSoundY, rainSoundZ, "ambient.weather.rain", 0.1F, 0.5F);
                }
                else
                {
                    _client.World.Broadcaster.PlaySoundAtPos(rainSoundX, rainSoundY, rainSoundZ, "ambient.weather.rain", 0.2F, 1.0F);
                }
            }
        }
    }

    protected void renderSnow(float tickDelta)
    {
        float rainGradient = _client.World.Environment.GetRainGradient(tickDelta);
        if (rainGradient > 0.0F)
        {
            EntityLiving camera = _client.Camera;
            World world = _client.World;
            int cameraBlockX = MathHelper.Floor(camera.X);
            int cameraBlockY = MathHelper.Floor(camera.Y);
            int cameraBlockZ = MathHelper.Floor(camera.Z);
            Tessellator tessellator = Tessellator.instance;
            GLManager.GL.Disable(GLEnum.CullFace);
            GLManager.GL.Normal3(0.0F, 1.0F, 0.0F);
            GLManager.GL.Enable(GLEnum.Blend);
            GLManager.GL.BlendFunc(GLEnum.SrcAlpha, GLEnum.OneMinusSrcAlpha);
            GLManager.GL.AlphaFunc(GLEnum.Greater, 0.01F);
            _client.TextureManager.BindTexture(_client.TextureManager.GetTextureId("/environment/snow.png"));
            double renderX = camera.LastTickX + (camera.X - camera.LastTickX) * (double)tickDelta;
            double renderY = camera.LastTickY + (camera.Y - camera.LastTickY) * (double)tickDelta;
            double renderZ = camera.LastTickZ + (camera.Z - camera.LastTickZ) * (double)tickDelta;
            int cameraYFloor = MathHelper.Floor(renderY);
            byte renderRadius = 10;

            Biome[] biomes = world.GetBiomeSource().GetBiomesInArea(cameraBlockX - renderRadius, cameraBlockZ - renderRadius, renderRadius * 2 + 1, renderRadius * 2 + 1);
            int biomeIndex = 0;

            int sampleX;
            int sampleZ;
            Biome biome;
            int topSolidY;
            int minY;
            int maxY;
            float textureScroll;
            for (sampleX = cameraBlockX - renderRadius; sampleX <= cameraBlockX + renderRadius; ++sampleX)
            {
                for (sampleZ = cameraBlockZ - renderRadius; sampleZ <= cameraBlockZ + renderRadius; ++sampleZ)
                {
                    biome = biomes[biomeIndex++];
                    if (biome.GetEnableSnow())
                    {
                        topSolidY = world.Reader.GetTopSolidBlockY(sampleX, sampleZ);
                        if (topSolidY < 0)
                        {
                            topSolidY = 0;
                        }

                        minY = topSolidY;
                        if (topSolidY < cameraYFloor)
                        {
                            minY = cameraYFloor;
                        }

                        maxY = cameraBlockY - renderRadius;
                        int maxRenderY = cameraBlockY + renderRadius;
                        if (maxY < topSolidY)
                        {
                            maxY = topSolidY;
                        }

                        if (maxRenderY < topSolidY)
                        {
                            maxRenderY = topSolidY;
                        }

                        textureScroll = 1.0F;
                        if (maxY != maxRenderY)
                        {
                            _random.SetSeed(sampleX * sampleX * 3121 + sampleX * 45238971 + sampleZ * sampleZ * 418711 + sampleZ * 13761);
                            float animationTime = _ticks + tickDelta;
                            float textureVOffset = ((_ticks & 511) + tickDelta) / 512.0F;
                            float textureUDrift = _random.NextFloat() + animationTime * 0.01F * (float)_random.NextGaussian();
                            float textureVDrift = _random.NextFloat() + animationTime * (float)_random.NextGaussian() * 0.001F;
                            double dx = (double)(sampleX + 0.5F) - camera.X;
                            double dz = (double)(sampleZ + 0.5F) - camera.Z;
                            float distanceFactor = MathHelper.Sqrt(dx * dx + dz * dz) / renderRadius;
                            tessellator.startDrawingQuads();
                            float brightness = world.GetLuminance(sampleX, minY, sampleZ);
                            GLManager.GL.Color4(brightness, brightness, brightness, ((1.0F - distanceFactor * distanceFactor) * 0.3F + 0.5F) * rainGradient);
                            tessellator.setTranslationD(-renderX * 1.0D, -renderY * 1.0D, -renderZ * 1.0D);
                            tessellator.addVertexWithUV(sampleX + 0, maxY, sampleZ + 0.5D, (double)(0.0F * textureScroll + textureUDrift), (double)(maxY * textureScroll / 4.0F + textureVOffset * textureScroll + textureVDrift));
                            tessellator.addVertexWithUV(sampleX + 1, maxY, sampleZ + 0.5D, (double)(1.0F * textureScroll + textureUDrift), (double)(maxY * textureScroll / 4.0F + textureVOffset * textureScroll + textureVDrift));
                            tessellator.addVertexWithUV(sampleX + 1, maxRenderY, sampleZ + 0.5D, (double)(1.0F * textureScroll + textureUDrift), (double)(maxRenderY * textureScroll / 4.0F + textureVOffset * textureScroll + textureVDrift));
                            tessellator.addVertexWithUV(sampleX + 0, maxRenderY, sampleZ + 0.5D, (double)(0.0F * textureScroll + textureUDrift), (double)(maxRenderY * textureScroll / 4.0F + textureVOffset * textureScroll + textureVDrift));
                            tessellator.addVertexWithUV(sampleX + 0.5D, maxY, sampleZ + 0, (double)(0.0F * textureScroll + textureUDrift), (double)(maxY * textureScroll / 4.0F + textureVOffset * textureScroll + textureVDrift));
                            tessellator.addVertexWithUV(sampleX + 0.5D, maxY, sampleZ + 1, (double)(1.0F * textureScroll + textureUDrift), (double)(maxY * textureScroll / 4.0F + textureVOffset * textureScroll + textureVDrift));
                            tessellator.addVertexWithUV(sampleX + 0.5D, maxRenderY, sampleZ + 1, (double)(1.0F * textureScroll + textureUDrift), (double)(maxRenderY * textureScroll / 4.0F + textureVOffset * textureScroll + textureVDrift));
                            tessellator.addVertexWithUV(sampleX + 0.5D, maxRenderY, sampleZ + 0, (double)(0.0F * textureScroll + textureUDrift), (double)(maxRenderY * textureScroll / 4.0F + textureVOffset * textureScroll + textureVDrift));
                            tessellator.setTranslationD(0.0D, 0.0D, 0.0D);
                            tessellator.draw();
                        }
                    }
                }
            }

            _client.TextureManager.BindTexture(_client.TextureManager.GetTextureId("/environment/rain.png"));
            renderRadius = 10;

            biomeIndex = 0;

            for (sampleX = cameraBlockX - renderRadius; sampleX <= cameraBlockX + renderRadius; ++sampleX)
            {
                for (sampleZ = cameraBlockZ - renderRadius; sampleZ <= cameraBlockZ + renderRadius; ++sampleZ)
                {
                    biome = biomes[biomeIndex++];
                    if (biome.CanSpawnLightningBolt())
                    {
                        topSolidY = world.Reader.GetTopSolidBlockY(sampleX, sampleZ);
                        minY = cameraBlockY - renderRadius;
                        maxY = cameraBlockY + renderRadius;
                        if (minY < topSolidY)
                        {
                            minY = topSolidY;
                        }

                        if (maxY < topSolidY)
                        {
                            maxY = topSolidY;
                        }

                        float rainUvScale = 1.0F;
                        if (minY != maxY)
                        {
                            _random.SetSeed(sampleX * sampleX * 3121 + sampleX * 45238971 + sampleZ * sampleZ * 418711 + sampleZ * 13761);
                            textureScroll = ((_ticks + sampleX * sampleX * 3121 + sampleX * 45238971 + sampleZ * sampleZ * 418711 + sampleZ * 13761 & 31) + tickDelta) / 32.0F * (3.0F + _random.NextFloat());
                            double rainDx = (double)(sampleX + 0.5F) - camera.X;
                            double rainDz = (double)(sampleZ + 0.5F) - camera.Z;
                            float rainDistanceFactor = MathHelper.Sqrt(rainDx * rainDx + rainDz * rainDz) / renderRadius;
                            tessellator.startDrawingQuads();
                            float rainBrightness = world.GetLuminance(sampleX, 128, sampleZ) * 0.85F + 0.15F;
                            GLManager.GL.Color4(rainBrightness, rainBrightness, rainBrightness, ((1.0F - rainDistanceFactor * rainDistanceFactor) * 0.5F + 0.5F) * rainGradient);
                            tessellator.setTranslationD(-renderX * 1.0D, -renderY * 1.0D, -renderZ * 1.0D);
                            tessellator.addVertexWithUV(sampleX + 0, minY, sampleZ + 0.5D, (double)(0.0F * rainUvScale), (double)(minY * rainUvScale / 4.0F + textureScroll * rainUvScale));
                            tessellator.addVertexWithUV(sampleX + 1, minY, sampleZ + 0.5D, (double)(1.0F * rainUvScale), (double)(minY * rainUvScale / 4.0F + textureScroll * rainUvScale));
                            tessellator.addVertexWithUV(sampleX + 1, maxY, sampleZ + 0.5D, (double)(1.0F * rainUvScale), (double)(maxY * rainUvScale / 4.0F + textureScroll * rainUvScale));
                            tessellator.addVertexWithUV(sampleX + 0, maxY, sampleZ + 0.5D, (double)(0.0F * rainUvScale), (double)(maxY * rainUvScale / 4.0F + textureScroll * rainUvScale));
                            tessellator.addVertexWithUV(sampleX + 0.5D, minY, sampleZ + 0, (double)(0.0F * rainUvScale), (double)(minY * rainUvScale / 4.0F + textureScroll * rainUvScale));
                            tessellator.addVertexWithUV(sampleX + 0.5D, minY, sampleZ + 1, (double)(1.0F * rainUvScale), (double)(minY * rainUvScale / 4.0F + textureScroll * rainUvScale));
                            tessellator.addVertexWithUV(sampleX + 0.5D, maxY, sampleZ + 1, (double)(1.0F * rainUvScale), (double)(maxY * rainUvScale / 4.0F + textureScroll * rainUvScale));
                            tessellator.addVertexWithUV(sampleX + 0.5D, maxY, sampleZ + 0, (double)(0.0F * rainUvScale), (double)(maxY * rainUvScale / 4.0F + textureScroll * rainUvScale));
                            tessellator.setTranslationD(0.0D, 0.0D, 0.0D);
                            tessellator.draw();
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
        World world = _client.World;
        EntityLiving camera = _client.Camera;
        float fogBlend = 4.0F / _client.Options.renderDistance;
        fogBlend = System.Math.Clamp(fogBlend, 0.25f, 1.0f);
        fogBlend = 1.0F - (float)Math.Pow(fogBlend, 0.25D);
        Vector3D<double> skyColor = world.Environment.GetSkyColor(_client.Camera, tickDelta);
        float skyRed = (float)skyColor.X;
        float skyGreen = (float)skyColor.Y;
        float skyBlue = (float)skyColor.Z;
        Vector3D<double> fogColor = world.GetFogColor(tickDelta);
        _fogColorRed = (float)fogColor.X;
        _fogColorGreen = (float)fogColor.Y;
        _fogColorBlue = (float)fogColor.Z;
        _fogColorRed += (skyRed - _fogColorRed) * fogBlend;
        _fogColorGreen += (skyGreen - _fogColorGreen) * fogBlend;
        _fogColorBlue += (skyBlue - _fogColorBlue) * fogBlend;
        float rainGradient = world.Environment.GetRainGradient(tickDelta);
        float rainDarken;
        float fogBrightness;
        if (rainGradient > 0.0F)
        {
            rainDarken = 1.0F - rainGradient * 0.5F;
            fogBrightness = 1.0F - rainGradient * 0.4F;
            _fogColorRed *= rainDarken;
            _fogColorGreen *= rainDarken;
            _fogColorBlue *= fogBrightness;
        }

        rainDarken = world.Environment.GetThunderGradient(tickDelta);
        if (rainDarken > 0.0F)
        {
            fogBrightness = 1.0F - rainDarken * 0.5F;
            _fogColorRed *= fogBrightness;
            _fogColorGreen *= fogBrightness;
            _fogColorBlue *= fogBrightness;
        }

        if (_cloudFog)
        {
            Vector3D<double> cloudColor = world.Environment.GetCloudColor(tickDelta);
            _fogColorRed = (float)cloudColor.X;
            _fogColorGreen = (float)cloudColor.Y;
            _fogColorBlue = (float)cloudColor.Z;
        }
        else if (camera.IsInFluid(Material.Water))
        {
            _fogColorRed = 0.02F;
            _fogColorGreen = 0.02F;
            _fogColorBlue = 0.2F;
        }
        else if (camera.IsInFluid(Material.Lava))
        {
            _fogColorRed = 0.6F;
            _fogColorGreen = 0.1F;
            _fogColorBlue = 0.0F;
        }

        fogBrightness = cameraController.LastViewBob + (cameraController.ViewBob - cameraController.LastViewBob) * tickDelta;
        _fogColorRed *= fogBrightness;
        _fogColorGreen *= fogBrightness;
        _fogColorBlue *= fogBrightness;

        GLManager.GL.ClearColor(_fogColorRed, _fogColorGreen, _fogColorBlue, 0.0F);
    }

    private void applyFog(int mode)
    {
        EntityLiving camera = _client.Camera;
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
        else if (camera.IsInFluid(Material.Water))
        {
            GLManager.GL.Fog(GLEnum.FogMode, (int)GLEnum.Exp);
            GLManager.GL.Fog(GLEnum.FogDensity, 0.1F);
            _client.WorldRenderer.ChunkRenderer.SetFogMode(1);
            _client.WorldRenderer.ChunkRenderer.SetFogDensity(0.1f);
        }
        else if (camera.IsInFluid(Material.Lava))
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

    private float[] updateFogColorBuffer(float red, float green, float blue, float alpha)
    {
        _fogColorBuffer[0] = red;
        _fogColorBuffer[1] = green;
        _fogColorBuffer[2] = blue;
        _fogColorBuffer[3] = alpha;
        return _fogColorBuffer;
    }
}
