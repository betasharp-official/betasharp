using BetaSharp.Client.Rendering.Legacy;
using BetaSharp.Entities;

namespace BetaSharp.Client.Rendering.Entities.Models;

public abstract class ModelBase
{
    public float onGround;
    public bool isRiding = false;

    public virtual void render(ILegacyFixedFunctionApi gl, float var1, float var2, float var3, float var4, float var5,
        float var6)
    {
    }

    public virtual void setRotationAngles(float var1, float var2, float var3, float var4, float var5, float var6)
    {
    }

    public virtual void setLivingAnimations(EntityLiving var1, float var2, float var3, float var4,
        ILegacyFixedFunctionApi gl)
    {
    }
}
