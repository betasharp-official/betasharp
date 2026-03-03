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

    public Vec3D? getIntermediateWithXValue(Vec3D var1, double var2)
    {
        double var4 = var1.x - x;
        double var6 = var1.y - y;
        double var8 = var1.z - z;
        if (var4 * var4 < (double)1.0E-7F)
        {
            return null;
        }
        else
        {
            double var10 = (var2 - x) / var4;
            return var10 >= 0.0D && var10 <= 1.0D ? new Vec3D(x + var4 * var10, y + var6 * var10, z + var8 * var10) : null;
        }
    }

    public Vec3D? getIntermediateWithYValue(Vec3D var1, double var2)
    {
        double var4 = var1.x - x;
        double var6 = var1.y - y;
        double var8 = var1.z - z;
        if (var6 * var6 < (double)1.0E-7F)
        {
            return null;
        }
        else
        {
            double var10 = (var2 - y) / var6;
            return var10 >= 0.0D && var10 <= 1.0D ? new Vec3D(x + var4 * var10, y + var6 * var10, z + var8 * var10) : null;
        }
    }

    public Vec3D? getIntermediateWithZValue(Vec3D var1, double var2)
    {
        double var4 = var1.x - x;
        double var6 = var1.y - y;
        double var8 = var1.z - z;
        if (var8 * var8 < (double)1.0E-7F)
        {
            return null;
        }
        else
        {
            double var10 = (var2 - z) / var8;
            return var10 >= 0.0D && var10 <= 1.0D ? new Vec3D(x + var4 * var10, y + var6 * var10, z + var8 * var10) : null;
        }
    }

    public void rotateAroundX(float var1)
    {
        float var2 = MathHelper.Cos(var1);
        float var3 = MathHelper.Sin(var1);
        double var4 = x;
        double var6 = y * (double)var2 + z * (double)var3;
        double var8 = z * (double)var2 - y * (double)var3;
        x = var4;
        y = var6;
        z = var8;
    }

    public void rotateAroundY(float var1)
    {
        float var2 = MathHelper.Cos(var1);
        float var3 = MathHelper.Sin(var1);
        double var4 = x * (double)var2 + z * (double)var3;
        double var6 = y;
        double var8 = z * (double)var2 - x * (double)var3;
        x = var4;
        y = var6;
        z = var8;
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
