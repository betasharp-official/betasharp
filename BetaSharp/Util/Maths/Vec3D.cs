namespace BetaSharp.Util.Maths;

public record struct Vec3D
{
    public static readonly Vec3D Zero = new Vec3D(0.0D, 0.0D, 0.0D);

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

    public Vec3D? getIntermediateWithXValue(Vec3D endpoint, double targetX)
    {
        double dx = endpoint.x - x;
        double dy = endpoint.y - y;
        double dz = endpoint.z - z;
        if (dx * dx < (double)1.0E-7F)
        {
            return null;
        }
        else
        {
            double t = (targetX - x) / dx;
            return t >= 0.0D && t <= 1.0D ? new Vec3D(x + dx * t, y + dy * t, z + dz * t) : null;
        }
    }

    public Vec3D? getIntermediateWithYValue(Vec3D endpoint, double targetY)
    {
        double dx = endpoint.x - x;
        double dy = endpoint.y - y;
        double dz = endpoint.z - z;
        if (dy * dy < (double)1.0E-7F)
        {
            return null;
        }
        else
        {
            double t = (targetY - y) / dy;
            return t >= 0.0D && t <= 1.0D ? new Vec3D(x + dx * t, y + dy * t, z + dz * t) : null;
        }
    }

    public Vec3D? getIntermediateWithZValue(Vec3D endpoint, double targetZ)
    {
        double dx = endpoint.x - x;
        double dy = endpoint.y - y;
        double dz = endpoint.z - z;
        if (dz * dz < (double)1.0E-7F)
        {
            return null;
        }
        else
        {
            double t = (targetZ - z) / dz;
            return t >= 0.0D && t <= 1.0D ? new Vec3D(x + dx * t, y + dy * t, z + dz * t) : null;
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
