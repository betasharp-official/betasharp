using System.Runtime.CompilerServices;
using BetaSharp.Util.Hit;

namespace BetaSharp.Util.Maths;

public struct Box
{
    public double MinX { get; set; }
    public double MinY { get; set; }
    public double MinZ { get; set; }
    public double MaxX { get; set; }
    public double MaxY { get; set; }
    public double MaxZ { get; set; }

    public Box(double minX, double minY, double minZ, double maxX, double maxY, double maxZ)
    {
        MinX = Math.Min(minX, maxX);
        MaxX = Math.Max(minX, maxX);
        MinY = Math.Min(minY, maxY);
        MaxY = Math.Max(minY, maxY);
        MinZ = Math.Min(minZ, maxZ);
        MaxZ = Math.Max(minZ, maxZ);
    }

    public Box Stretch(double x, double y, double z)
    {
        var (newMinX, newMaxX) = x < 0 ? (MinX + x, MaxX) : (MinX, MaxX + x);
        var (newMinY, newMaxY) = y < 0 ? (MinY + y, MaxY) : (MinY, MaxY + y);
        var (newMinZ, newMaxZ) = z < 0 ? (MinZ + z, MaxZ) : (MinZ, MaxZ + z);

        return new Box(newMinX, newMinY, newMinZ, newMaxX, newMaxY, newMaxZ);
    }

    public Box Expand(double x, double y, double z) =>
        new(MinX - x, MinY - y, MinZ - z, MaxX + x, MaxY + y, MaxZ + z);

    public Box Offset(double x, double y, double z) =>
        new(MinX + x, MinY + y, MinZ + z, MaxX + x, MaxY + y, MaxZ + z);

    public double GetXOffset(in Box other, double offsetX)
    {
        if (other.MaxY <= MinY || other.MinY >= MaxY || other.MaxZ <= MinZ || other.MinZ >= MaxZ)
            return offsetX;

        if (offsetX > 0 && other.MaxX <= MinX)
            return Math.Min(offsetX, MinX - other.MaxX);

        if (offsetX < 0 && other.MinX >= MaxX)
            return Math.Max(offsetX, MaxX - other.MinX);

        return offsetX;
    }

    public double GetYOffset(in Box other, double offsetY)
    {
        if (other.MaxX <= MinX || other.MinX >= MaxX || other.MaxZ <= MinZ || other.MinZ >= MaxZ)
            return offsetY;

        if (offsetY > 0 && other.MaxY <= MinY)
        {
            double diff = MinY - other.MaxY;
            if (diff < offsetY) offsetY = diff;
        }
        else if (offsetY < 0 && other.MinY >= MaxY)
        {
            double diff = MaxY - other.MinY;
            if (diff > offsetY) offsetY = diff;
        }

        return offsetY;
    }

    public double GetZOffset(in Box other, double offsetZ)
    {
        if (other.MaxX <= MinX || other.MinX >= MaxX || other.MaxY <= MinY || other.MinY >= MaxY)
            return offsetZ;

        if (offsetZ > 0 && other.MaxZ <= MinZ)
        {
            double diff = MinZ - other.MaxZ;
            if (diff < offsetZ) offsetZ = diff;
        }
        else if (offsetZ < 0 && other.MinZ >= MaxZ)
        {
            double diff = MaxZ - other.MinZ;
            if (diff > offsetZ) offsetZ = diff;
        }

        return offsetZ;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Intersects(in Box other) =>
        other.MaxX > MinX && other.MinX < MaxX &&
        other.MaxY > MinY && other.MinY < MaxY &&
        other.MaxZ > MinZ && other.MinZ < MaxZ;

    public Box Translate(double x, double y, double z)
    {
        MinX += x;
        MinY += y;
        MinZ += z;
        MaxX += x;
        MaxY += y;
        MaxZ += z;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(in Vec3D pos) =>
        pos.x > MinX && pos.x < MaxX &&
        pos.y > MinY && pos.y < MaxY &&
        pos.z > MinZ && pos.z < MaxZ;

    public double AverageEdgeLength => (MaxX - MinX + (MaxY - MinY) + (MaxZ - MinZ)) / 3.0;

    public Box Contract(double x, double y, double z) =>
        new(MinX + x, MinY + y, MinZ + z, MaxX - x, MaxY - y, MaxZ - z);

    private enum Axis { X, Y, Z }

    public HitResult Raycast(Vec3D start, Vec3D end)
    {
        Vec3D? hitX = GetClosest(start, end, start.getIntermediateWithXValue(end, MinX), start.getIntermediateWithXValue(end, MaxX), Axis.X);
        Vec3D? hitY = GetClosest(start, end, start.getIntermediateWithYValue(end, MinY), start.getIntermediateWithYValue(end, MaxY), Axis.Y);
        Vec3D? hitZ = GetClosest(start, end, start.getIntermediateWithZValue(end, MinZ), start.getIntermediateWithZValue(end, MaxZ), Axis.Z);

        Vec3D? finalHit = null;
        int side = -1;

        UpdateHit(hitX, ref finalHit, ref side, start.getIntermediateWithXValue(end, MinX) == hitX ? 4 : 5);
        UpdateHit(hitY, ref finalHit, ref side, start.getIntermediateWithYValue(end, MinY) == hitY ? 0 : 1);
        UpdateHit(hitZ, ref finalHit, ref side, start.getIntermediateWithZValue(end, MinZ) == hitZ ? 2 : 3);

        return finalHit is null
            ? new HitResult(HitResultType.MISS)
            : new HitResult(0, 0, 0, side, finalHit.Value, HitResultType.TILE);

        void UpdateHit(in Vec3D? candidate, ref Vec3D? current, ref int currentSide, int candidateSide)
        {
            if (candidate is null) return;
            if (current is null || start.distanceTo(candidate.Value) < start.distanceTo(current.Value))
            {
                current = candidate;
                currentSide = candidateSide;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsValid(in Vec3D p, Axis axis) => axis switch
    {
        Axis.X => p.y >= MinY && p.y <= MaxY && p.z >= MinZ && p.z <= MaxZ,
        Axis.Y => p.x >= MinX && p.x <= MaxX && p.z >= MinZ && p.z <= MaxZ,
        Axis.Z => p.x >= MinX && p.x <= MaxX && p.y >= MinY && p.y <= MaxY,
        _ => false
    };

    private Vec3D? GetClosest(in Vec3D start, in Vec3D end, in Vec3D? a, in Vec3D? b, Axis axis)
    {
        bool aValid = a is not null && IsValid(a.Value, axis);
        bool bValid = b is not null && IsValid(b.Value, axis);

        if (aValid && bValid)
            return start.distanceTo(a!.Value) < start.distanceTo(b!.Value) ? a : b;

        return aValid ? a : (bValid ? b : null);
    }

    public override string ToString() => $"Box[{MinX:F2}, {MinY:F2}, {MinZ:F2} -> {MaxX:F2}, {MaxY:F2}, {MaxZ:F2}]";
}
