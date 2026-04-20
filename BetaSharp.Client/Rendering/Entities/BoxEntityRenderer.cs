using BetaSharp.Client.Rendering.Core;
using BetaSharp.Entities;
using BetaSharp.Util.Maths;

namespace BetaSharp.Client.Rendering.Entities;

public class BoxEntityRenderer : EntityRenderer
{

    public override void Render(Entity target, double x, double y, double z, float yaw, float tickDelta)
    {
        GLManager.GL.PushMatrix();
        renderShape(target.BoundingBox, new Vec3D(x - target.LastTickX, y - target.LastTickY, z - target.LastTickZ));
        GLManager.GL.PopMatrix();
    }
}
