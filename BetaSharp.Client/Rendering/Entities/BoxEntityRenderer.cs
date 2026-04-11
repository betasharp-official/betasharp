using BetaSharp.Client.Rendering.Core;
using BetaSharp.Entities;
using BetaSharp.Util.Maths;

namespace BetaSharp.Client.Rendering.Entities;

public class BoxEntityRenderer : EntityRenderer
{

    public override void Render(Entity target, double x, double y, double z, float yaw, float tickDelta)
    {
        Scene.PushMatrix();
        renderShape(target.boundingBox, new Vec3D(x - target.lastTickX, y - target.lastTickY, z - target.lastTickZ));
        Scene.PopMatrix();
    }
}
