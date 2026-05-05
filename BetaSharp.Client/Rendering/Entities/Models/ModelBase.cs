using BetaSharp.Entities;

namespace BetaSharp.Client.Rendering.Entities.Models;

public abstract class ModelBase
{
    public float onGround;
    public bool isRiding = false;

    public virtual void render(float limbSwing, float limbSwingAmount, float ageInTicks, float netHeadYaw, float headPitch, float scale)
    {
    }

    public virtual void setRotationAngles(float limbSwing, float limbSwingAmount, float ageInTicks, float netHeadYaw, float headPitch, float scale)
    {
    }

    public virtual void setLivingAnimations(EntityLiving entity, float limbSwing, float limbSwingAmount, float partialTick)
    {
    }
}
