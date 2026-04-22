using BetaSharp.Blocks;
using BetaSharp.Blocks.Materials;
using BetaSharp.Client.Options;
using BetaSharp.Client.Rendering.Core;
using BetaSharp.Entities;
using BetaSharp.Util.Hit;
using BetaSharp.Util.Maths;

namespace BetaSharp.Client.Rendering;

public class CameraController
{
    private readonly BetaSharp _game;
    private readonly float _thirdPersonDistance = 4.0F;
    private readonly float _frontThirdPersonDistance = 4.0F;
    public float ViewBob { get; private set; }
    public float LastViewBob { get; private set; }

    private float _prevThirdPersonDistance = 4.0F;
    private float _prevFrontThirdPersonDistance = 4.0F;
    private readonly float _thirdPersonYaw = 0.0F;
    private float _prevThirdPersonYaw;
    private readonly float _thirdPersonPitch = 0.0F;
    private float _prevThirdPersonPitch;
    private readonly float _cameraRoll = 0.0F;
    private float _prevCameraRoll;
    private readonly float _cameraRollAmount = 0.0F;
    private float _prevCameraRollAmount;
    private bool _isZoomHeld;
    private float _zoomScale = 2.0F;
    public double CameraZoom { get; private set; } = 1.0D;
    public double CameraYaw { get; } = 0.0D;
    public double CameraPitch { get; } = 0.0D;
    public bool IsZoomActive => _isZoomHeld;

    public CameraController(BetaSharp game)
    {
        _game = game;
    }

    public void UpdateCamera()
    {
        LastViewBob = ViewBob;
        _prevThirdPersonDistance = _thirdPersonDistance;
        _prevFrontThirdPersonDistance = _frontThirdPersonDistance;
        _prevThirdPersonYaw = _thirdPersonYaw;
        _prevThirdPersonPitch = _thirdPersonPitch;
        _prevCameraRoll = _cameraRoll;
        _prevCameraRollAmount = _cameraRollAmount;

        float luminance = _game.World.GetLuminance(MathHelper.Floor(_game.Camera.X), MathHelper.Floor(_game.Camera.Y), MathHelper.Floor(_game.Camera.Z));
        float renderDistFactor = System.Math.Clamp((_game.Options.renderDistance - 4.0F) / 28.0F, 0.0F, 1.0F);
        float targetBob = luminance * (1.0F - renderDistFactor) + renderDistFactor;
        ViewBob += (targetBob - ViewBob) * 0.1F;
    }

    public void SetZoomState(bool isHeld, float zoomScale)
    {
        _isZoomHeld = isHeld;
        _zoomScale = System.Math.Clamp(zoomScale, 1.25F, 20.0F);
        CameraZoom = 1.0D;
    }

    public float GetFov(float tickDelta, bool isHand = false)
    {
        EntityLiving cameraEntity = _game.Camera;
        float fov = isHand ? 70.0F : (30.0F + _game.Options.Fov * 90.0F);

        if (cameraEntity.IsInFluid(Material.Water))
        {
            fov -= 10.0F;
        }

        if (cameraEntity.Health <= 0)
        {
            float deathTimeF = cameraEntity.DeathTime + tickDelta;
            fov /= (1.0F - 500.0F / (deathTimeF + 500.0F)) * 2.0F + 1.0F;
        }

        if (_isZoomHeld && !isHand)
        {
            float zoomProgress = 1.0F / _zoomScale;
            float easedZoomProgress = (float)System.Math.Pow(zoomProgress, 2.0D);
            fov = 1.0F + (fov - 1.0F) * easedZoomProgress;
        }

        return fov + _prevCameraRoll + (_cameraRoll - _prevCameraRoll) * tickDelta;
    }

    public void ApplyDamageTiltEffect(float tickDelta)
    {
        EntityLiving cameraEntity = _game.Camera;
        float hurtTimeF = cameraEntity.HurtTime - tickDelta;

        if (cameraEntity.Health <= 0)
        {
            float deathTimeF = cameraEntity.DeathTime + tickDelta;
            GLManager.GL.Rotate(40.0F - 8000.0F / (deathTimeF + 200.0F), 0.0F, 0.0F, 1.0F);
        }

        if (hurtTimeF >= 0.0F)
        {
            hurtTimeF /= cameraEntity.MaxHurtTime;
            hurtTimeF = MathHelper.Sin(hurtTimeF * hurtTimeF * hurtTimeF * hurtTimeF * (float)Math.PI);
            float attackedYaw = cameraEntity.AttackedAtYaw;
            GLManager.GL.Rotate(-attackedYaw, 0.0F, 1.0F, 0.0F);
            GLManager.GL.Rotate(-hurtTimeF * 14.0F, 0.0F, 0.0F, 1.0F);
            GLManager.GL.Rotate(attackedYaw, 0.0F, 1.0F, 0.0F);
        }
    }

    public void ApplyViewBobbing(float tickDelta)
    {
        if (_game.Camera is EntityPlayer player)
        {
            float speedDelta = player.HorizontalSpeed - player.PrevHorizontalSpeed;
            float speed = -(player.HorizontalSpeed + speedDelta * tickDelta);
            float bobAmount = player.prevStepBobbingAmount + (player.stepBobbingAmount - player.prevStepBobbingAmount) * tickDelta;
            float pitch = player.CameraPitch + (player.Tilt - player.CameraPitch) * tickDelta;

            GLManager.GL.Translate(MathHelper.Sin(speed * (float)Math.PI) * bobAmount * 0.5F, -Math.Abs(MathHelper.Cos(speed * (float)Math.PI) * bobAmount), 0.0F);
            GLManager.GL.Rotate(MathHelper.Sin(speed * (float)Math.PI) * bobAmount * 3.0F, 0.0F, 0.0F, 1.0F);
            GLManager.GL.Rotate(Math.Abs(MathHelper.Cos(speed * (float)Math.PI - 0.2F) * bobAmount) * 5.0F, 1.0F, 0.0F, 0.0F);
            GLManager.GL.Rotate(pitch, 1.0F, 0.0F, 0.0F);
        }
    }

    public void ApplyCameraTransform(float tickDelta)
    {
        EntityLiving cameraEntity = _game.Camera;
        float eyeHeightOffset = cameraEntity.StandingEyeHeight - 1.62F;
        double x = cameraEntity.PrevX + (cameraEntity.X - cameraEntity.PrevX) * (double)tickDelta;
        double y = cameraEntity.PrevY + (cameraEntity.Y - cameraEntity.PrevY) * (double)tickDelta - (double)eyeHeightOffset;
        double z = cameraEntity.PrevZ + (cameraEntity.Z - cameraEntity.PrevZ) * (double)tickDelta;

        GLManager.GL.Rotate(_prevCameraRollAmount + (_cameraRollAmount - _prevCameraRollAmount) * tickDelta, 0.0F, 0.0F, 1.0F);

        if (cameraEntity.isSleeping())
        {
            eyeHeightOffset = (float)((double)eyeHeightOffset + 1.0D);
            GLManager.GL.Translate(0.0F, 0.3F, 0.0F);
            if (!_game.Options.DebugCamera)
            {
                int blockId = _game.World.Reader.GetBlockId(MathHelper.Floor(cameraEntity.X), MathHelper.Floor(cameraEntity.Y), MathHelper.Floor(cameraEntity.Z));
                if (blockId == Block.Bed.id)
                {
                    int meta = _game.World.Reader.GetBlockMeta(MathHelper.Floor(cameraEntity.X), MathHelper.Floor(cameraEntity.Y), MathHelper.Floor(cameraEntity.Z));
                    int rotation = meta & 3;
                    GLManager.GL.Rotate(rotation * 90, 0.0F, 1.0F, 0.0F);
                }

                GLManager.GL.Rotate(cameraEntity.PrevYaw + (cameraEntity.Yaw - cameraEntity.PrevYaw) * tickDelta + 180.0F, 0.0F, -1.0F, 0.0F);
                GLManager.GL.Rotate(cameraEntity.PrevPitch + (cameraEntity.Pitch - cameraEntity.PrevPitch) * tickDelta, -1.0F, 0.0F, 0.0F);
            }
        }
        else if (_game.Options.CameraMode == EnumCameraMode.ThirdPerson || _game.Options.CameraMode == EnumCameraMode.FrontThirdPerson)
        {
            double currentDistance;
            if (_game.Options.CameraMode == EnumCameraMode.FrontThirdPerson)
            {
                currentDistance = _prevFrontThirdPersonDistance + (_frontThirdPersonDistance - _prevFrontThirdPersonDistance) * tickDelta;
            }
            else
            {
                currentDistance = _prevThirdPersonDistance + (_thirdPersonDistance - _prevThirdPersonDistance) * tickDelta;
            }

            float targetPitch;
            float targetYaw;

            if (_game.Options.DebugCamera)
            {
                targetYaw = _prevThirdPersonYaw + (_thirdPersonYaw - _prevThirdPersonYaw) * tickDelta;
                targetPitch = _prevThirdPersonPitch + (_thirdPersonPitch - _prevThirdPersonPitch) * tickDelta;
                GLManager.GL.Translate(0.0F, 0.0F, (float)-currentDistance);
                GLManager.GL.Rotate(targetPitch, 1.0F, 0.0F, 0.0F);
                GLManager.GL.Rotate(targetYaw, 0.0F, 1.0F, 0.0F);
            }
            else
            {
                targetYaw = cameraEntity.Yaw;
                targetPitch = cameraEntity.Pitch;

                double vecX = (double)(-MathHelper.Sin(targetYaw / 180.0F * (float)Math.PI) * MathHelper.Cos(targetPitch / 180.0F * (float)Math.PI)) * currentDistance;
                double vecZ = (double)(MathHelper.Cos(targetYaw / 180.0F * (float)Math.PI) * MathHelper.Cos(targetPitch / 180.0F * (float)Math.PI)) * currentDistance;
                double vecY = (double)(-MathHelper.Sin(targetPitch / 180.0F * (float)Math.PI)) * currentDistance;

                for (int i = 0; i < 8; ++i)
                {
                    float offsetX = ((i & 1) * 2 - 1) * 0.1F;
                    float offsetY = ((i >> 1 & 1) * 2 - 1) * 0.1F;
                    float offsetZ = ((i >> 2 & 1) * 2 - 1) * 0.1F;

                    HitResult hit = new HitResult(HitResultType.MISS);

                    if (_game.Options.CameraMode == EnumCameraMode.FrontThirdPerson)
                    {
                        hit = _game.World.Reader.Raycast(
                            new Vec3D(x + offsetX, y + offsetY, z + offsetZ),
                            new Vec3D(x + vecX + offsetX + offsetZ, y + vecY + offsetY, z + vecZ + offsetZ)
                        );
                    }
                    else
                    {
                        hit = _game.World.Reader.Raycast(
                            new Vec3D(x + offsetX, y + offsetY, z + offsetZ),
                            new Vec3D(x - vecX + offsetX + offsetZ, y - vecY + offsetY, z - vecZ + offsetZ)
                        );
                    }

                    if (hit.Type != HitResultType.MISS)
                    {
                        double dist = hit.Pos.distanceTo(new Vec3D(x, y, z));
                        if (dist < currentDistance)
                        {
                            currentDistance = dist;
                        }
                    }
                }

                GLManager.GL.Rotate(cameraEntity.Pitch - targetPitch, 1.0F, 0.0F, 0.0F);
                GLManager.GL.Rotate(cameraEntity.Yaw - targetYaw, 0.0F, 1.0F, 0.0F);
                GLManager.GL.Translate(0.0F, 0.0F, (float)-currentDistance);
                if (_game.Options.CameraMode == EnumCameraMode.FrontThirdPerson)
                {
                    GLManager.GL.Rotate(180.0F, 0.0F, 1.0F, 0.0F);
                }
                GLManager.GL.Rotate(targetYaw - cameraEntity.Yaw, 0.0F, 1.0F, 0.0F);
                GLManager.GL.Rotate(targetPitch - cameraEntity.Pitch, 1.0F, 0.0F, 0.0F);
            }
        }
        else
        {
            GLManager.GL.Translate(0.0F, 0.0F, -0.1F);
        }

        if (!_game.Options.DebugCamera)
        {
            GLManager.GL.Rotate(cameraEntity.PrevPitch + (cameraEntity.Pitch - cameraEntity.PrevPitch) * tickDelta, 1.0F, 0.0F, 0.0F);
            GLManager.GL.Rotate(cameraEntity.PrevYaw + (cameraEntity.Yaw - cameraEntity.PrevYaw) * tickDelta + 180.0F, 0.0F, 1.0F, 0.0F);
        }

        GLManager.GL.Translate(0.0F, eyeHeightOffset, 0.0F);
    }
}
