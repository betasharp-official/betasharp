namespace BetaSharp.Util.Maths;

public record struct Vec3D
{
    public static readonly Vec3D Zero = new(0.0D, 0.0D, 0.0D);

    public double x;
    public double y;
    public double z;

    public Vec3D(double x, double y, double z)
    {
        if (x == -0.0D) x = 0.0D;
        if (y == -0.0D) y = 0.0D;
        if (z == -0.0D) z = 0.0D;

        this.x = x;
        this.y = y;
        this.z = z;
    }

    public double squareDistanceTo(Vec3D other)
    {
        double dx = other.x - x;
        double dy = other.y - y;
        double dz = other.z - z;
        return dx * dx + dy * dy + dz * dz;
    }

    public double distanceTo(Vec3D other)
    {
        return Math.Sqrt(squareDistanceTo(other));
    }

    public double squareDistance2DTo(Vec3D other)
    {
        double dx = other.x - x;
        double dz = other.z - z;
        return dx * dx + dz * dz;
    }

    public double magnitude()
    {
        return distanceTo(Zero);
    }

    public Vec3D normalize()
    {
        double mag = magnitude();
        return mag < 1.0E-4D ? Zero : this / mag;
    }

    public Vec3D crossProduct(Vec3D other)
    {
        return new Vec3D(y * other.z - z * other.y, z * other.x - x * other.z, x * other.y - y * other.x);
    }

    public Vec3D? getIntermediateWithXValue(Vec3D other, double xValue)
    {
        double deltaX = other.x - x;
        double deltaY = other.y - y;
        double deltaZ = other.z - z;
        if (deltaX * deltaX < (double)1.0E-7F)
        {
            return null;
        }
        else
        {
            double progress = (xValue - x) / deltaX;
            return progress >= 0.0D && progress <= 1.0D ? new Vec3D(x + deltaX * progress, y + deltaY * progress, z + deltaZ * progress) : null;
        }
    }

    public Vec3D? getIntermediateWithYValue(Vec3D other, double yValue)
    {
        double deltaX = other.x - x;
        double deltaY = other.y - y;
        double deltaZ = other.z - z;
        if (deltaY * deltaY < (double)1.0E-7F)
        {
            return null;
        }
        else
        {
            double progress = (yValue - y) / deltaY;
            return progress >= 0.0D && progress <= 1.0D ? new Vec3D(x + deltaX * progress, y + deltaY * progress, z + deltaZ * progress) : null;
        }
    }

    public Vec3D? getIntermediateWithZValue(Vec3D other, double zValue)
    {
        double deltaX = other.x - x;
        double deltaY = other.y - y;
        double deltaZ = other.z - z;
        if (deltaZ * deltaZ < (double)1.0E-7F)
        {
            return null;
        }
        else
        {
            double progress = (zValue - z) / deltaZ;
            return progress >= 0.0D && progress <= 1.0D ? new Vec3D(x + deltaX * progress, y + deltaY * progress, z + deltaZ * progress) : null;
        }
    }

    public void rotateAroundX(float angleRadians)
    {
        float cosAngle = MathHelper.Cos(angleRadians);
        float sinAngle = MathHelper.Sin(angleRadians);

        double rotatedY = y * cosAngle + z * sinAngle;
        double rotatedZ = z * cosAngle - y * sinAngle;

        y = rotatedY;
        z = rotatedZ;
    }

    public void rotateAroundY(float angleRadians)
    {
        float cosAngle = MathHelper.Cos(angleRadians);
        float sinAngle = MathHelper.Sin(angleRadians);

        double rotatedX = x * cosAngle + z * sinAngle;
        double rotatedZ = z * cosAngle - x * sinAngle;

        x = rotatedX;
        z = rotatedZ;
    }

    public override string ToString()
    {
        return "(" + x + ", " + y + ", " + z + ")";
    }

    public string ToString(string format)
    {
        return "(" + x.ToString(format) + ", " + y.ToString(format) + ", " + z.ToString(format) + ")";
    }

    public static Vec3D operator +(Vec3D a, Vec3D b)
    {
        return new Vec3D(a.x + b.x, a.y + b.y, a.z + b.z);
    }
    public static Vec3D operator -(Vec3D a, Vec3D b)
    {
        return new Vec3D(a.x - b.x, a.y - b.y, a.z - b.z);
    }

    public static Vec3D operator *(Vec3D a, Vec3D b)
    {
        return new Vec3D(a.x * b.x, a.y * b.y, a.z * b.z);
    }

    public static Vec3D operator /(Vec3D a, Vec3D b)
    {
        return new Vec3D(a.x / b.x, a.y / b.y, a.z / b.z);
    }

    public static Vec3D operator *(double a, Vec3D b)
    {
        return new Vec3D(a * b.x, a * b.y, a * b.z);
    }

    public static Vec3D operator /(Vec3D a, double b)
    {
        return new Vec3D(a.x / b, a.y / b, a.z / b);
    }
}
