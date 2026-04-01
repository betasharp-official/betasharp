using BetaSharp.Util.Maths;

namespace BetaSharp.Client.Rendering;

public interface ICuller
{
    bool IsBoundingBoxInFrustum(Box aabb);

    void SetPosition(double x, double y, double z);
}
