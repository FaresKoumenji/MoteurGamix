using System;

[System.Serializable]
public struct CustomVector3
{
    // Public Coordinates (Matching Unity naming)
    public float x;
    public float y;
    public float z;

    // Constructors
    public CustomVector3(float x, float y, float z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public CustomVector3(float x, float y)
    {
        this.x = x;
        this.y = y;
        this.z = 0f;
    }

    // Static Standard Directions & Values
    public static CustomVector3 zero => new CustomVector3(0f, 0f, 0f);
    public static CustomVector3 one => new CustomVector3(1f, 1f, 1f);
    public static CustomVector3 forward => new CustomVector3(0f, 0f, 1f);
    public static CustomVector3 back => new CustomVector3(0f, 0f, -1f);
    public static CustomVector3 up => new CustomVector3(0f, 1f, 0f);
    public static CustomVector3 down => new CustomVector3(0f, -1f, 0f);
    public static CustomVector3 left => new CustomVector3(-1f, 0f, 0f);
    public static CustomVector3 right => new CustomVector3(1f, 0f, 0f);

    // Magnitude & Normalization
    public float magnitude => (float)Math.Sqrt(x * x + y * y + z * z);
    public float sqrMagnitude => x * x + y * y + z * z;

    public CustomVector3 normalized
    {
        get
        {
            float mag = magnitude;
            if (mag > 1e-5f)
                return this / mag;
            return zero;
        }
    }

    public void Normalize()
    {
        float mag = magnitude;
        if (mag > 1e-5f)
            this /= mag;
        else
            this = zero;
    }

    // Common Static Math Methods
    public static float Dot(CustomVector3 lhs, CustomVector3 rhs)
    {
        return lhs.x * rhs.x + lhs.y * rhs.y + lhs.z * rhs.z;
    }

    public static CustomVector3 Cross(CustomVector3 lhs, CustomVector3 rhs)
    {
        return new CustomVector3(
            lhs.y * rhs.z - lhs.z * rhs.y,
            lhs.z * rhs.x - lhs.x * rhs.z,
            lhs.x * rhs.y - lhs.y * rhs.x
        );
    }

    public static float Distance(CustomVector3 a, CustomVector3 b)
    {
        return (a - b).magnitude;
    }

    public static CustomVector3 Lerp(CustomVector3 a, CustomVector3 b, float t)
    {
        t = Math.Max(0f, Math.Min(1f, t));
        return new CustomVector3(
            a.x + (b.x - a.x) * t,
            a.y + (b.y - a.y) * t,
            a.z + (b.z - a.z) * t
        );
    }

    // Operator Overloads
    public static CustomVector3 operator +(CustomVector3 a, CustomVector3 b) => new CustomVector3(a.x + b.x, a.y + b.y, a.z + b.z);
    public static CustomVector3 operator -(CustomVector3 a, CustomVector3 b) => new CustomVector3(a.x - b.x, a.y - b.y, a.z - b.z);
    public static CustomVector3 operator -(CustomVector3 a) => new CustomVector3(-a.x, -a.y, -a.z);
    public static CustomVector3 operator *(CustomVector3 a, float d) => new CustomVector3(a.x * d, a.y * d, a.z * d);
    public static CustomVector3 operator *(float d, CustomVector3 a) => new CustomVector3(a.x * d, a.y * d, a.z * d);
    public static CustomVector3 operator /(CustomVector3 a, float d) => new CustomVector3(a.x / d, a.y / d, a.z / d);

    public static bool operator ==(CustomVector3 lhs, CustomVector3 rhs)
    {
        float diffX = lhs.x - rhs.x;
        float diffY = lhs.y - rhs.y;
        float diffZ = lhs.z - rhs.z;
        return (diffX * diffX + diffY * diffY + diffZ * diffZ) < 9.999999e-11f;
    }

    public static bool operator !=(CustomVector3 lhs, CustomVector3 rhs) => !(lhs == rhs);

    public override bool Equals(object other)
    {
        if (!(other is CustomVector3)) return false;
        return this == (CustomVector3)other;
    }

    public override int GetHashCode() => x.GetHashCode() ^ (y.GetHashCode() << 2) ^ (z.GetHashCode() >> 2);
    public override string ToString() => $"({x:F1}, {y:F1}, {z:F1})";
}