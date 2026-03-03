namespace BetaSharp.Client.Rendering.Blocks;

internal readonly ref struct FaceColors(float rTl, float gTl, float bTl, float rBl, float gBl, float bBl, float rBr, float gBr, float bBr, float rTr, float gTr, float bTr)
{
    public readonly float RedTopLeft = rTl, GreenTopLeft = gTl, BlueTopLeft = bTl;
    public readonly float RedBottomLeft = rBl, GreenBottomLeft = gBl, BlueBottomLeft = bBl;
    public readonly float RedBottomRight = rBr, GreenBottomRight = gBr, BlueBottomRight = bBr;
    public readonly float RedTopRight = rTr, GreenTopRight = gTr, BlueTopRight = bTr;

    internal static FaceColors AssignVertexColors(float v0, float v1, float v2, float v3, float r, float g, float b, float faceShadow, bool tint)
    {
        float tr = (tint ? r : 1.0F) * faceShadow;
        float tg = (tint ? g : 1.0F) * faceShadow;
        float tb = (tint ? b : 1.0F) * faceShadow;

        return new FaceColors(
            tr * v0, tg * v0, tb * v0, // Top Left
            tr * v1, tg * v1, tb * v1, // Bottom Left
            tr * v2, tg * v2, tb * v2, // Bottom Right
            tr * v3, tg * v3, tb * v3 // Top Right
        );
    }
}
