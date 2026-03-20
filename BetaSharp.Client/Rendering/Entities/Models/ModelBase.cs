using BetaSharp.Entities;

namespace BetaSharp.Client.Rendering.Entities.Models;

public abstract class ModelBase
{
    public float onGround;
    public bool isRiding = false;

    public virtual void render(float walkAnimPhase, float walkSpeed, float ageInTicks, float headYaw, float headPitch, float modelScale)
    {
    }

    public virtual void setRotationAngles(float walkAnimPhase, float walkSpeed, float ageInTicks, float headYaw, float headPitch, float modelScale)
    {
    }

    public virtual void setLivingAnimations(EntityLiving entity, float walkAnimPhase, float walkSpeed, float partialTicks)
    {
    }
}
