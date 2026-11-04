using System;
using UnityEngine;

/// <summary>
/// Orientation stored as a unit quaternion (part of the state vector).
/// Nothing here touches UnityEngine.Transform.
/// </summary>
[Serializable]
public struct CustomQuaternion
{
    public float x, y, z, w;

    public CustomQuaternion(float x, float y, float z, float w)
    {
        this.x = x; this.y = y; this.z = z; this.w = w;
    }

    public static CustomQuaternion identity => new CustomQuaternion(0f, 0f, 0f, 1f);

    public static CustomQuaternion FromAxisAngle(CustomVector3 axis, float radians)
    {
        CustomVector3 a = axis.normalized;
        float s = (float)Math.Sin(radians * 0.5f);
        return new CustomQuaternion(a.x * s, a.y * s, a.z * s, (float)Math.Cos(radians * 0.5f));
    }

    /// <summary>Euler angles in degrees, same order as Unity (Z, then X, then Y).</summary>
    public static CustomQuaternion FromEulerDegrees(CustomVector3 euler)
    {
        float d2r = (float)Math.PI / 180f;
        CustomQuaternion qx = FromAxisAngle(CustomVector3.right, euler.x * d2r);
        CustomQuaternion qy = FromAxisAngle(CustomVector3.up, euler.y * d2r);
        CustomQuaternion qz = FromAxisAngle(CustomVector3.forward, euler.z * d2r);
        return qy * qx * qz;
    }

    public static CustomQuaternion operator *(CustomQuaternion a, CustomQuaternion b)
    {
        return new CustomQuaternion(
            a.w * b.x + a.x * b.w + a.y * b.z - a.z * b.y,
            a.w * b.y - a.x * b.z + a.y * b.w + a.z * b.x,
            a.w * b.z + a.x * b.y - a.y * b.x + a.z * b.w,
            a.w * b.w - a.x * b.x - a.y * b.y - a.z * b.z);
    }

    public CustomQuaternion Normalized()
    {
        float m = (float)Math.Sqrt(x * x + y * y + z * z + w * w);
        if (m < 1e-6f) return identity;
        return new CustomQuaternion(x / m, y / m, z / m, w / m);
    }

    /// <summary>
    /// Integrates orientation over dt given world-space angular velocity:
    /// q' = q + 0.5 * dt * (0, w) * q   (explicit Euler), then renormalize.
    /// </summary>
    public CustomQuaternion Integrate(CustomVector3 angularVelocity, float dt)
    {
        CustomQuaternion omega = new CustomQuaternion(angularVelocity.x, angularVelocity.y, angularVelocity.z, 0f);
        CustomQuaternion dq = omega * this;
        return new CustomQuaternion(
            x + 0.5f * dt * dq.x,
            y + 0.5f * dt * dq.y,
            z + 0.5f * dt * dq.z,
            w + 0.5f * dt * dq.w).Normalized();
    }

    /// <summary>Rotates a vector by this quaternion (via the rotation matrix).</summary>
    public CustomVector3 Rotate(CustomVector3 v)
    {
        float xx = x * x, yy = y * y, zz = z * z;
        float xy = x * y, xz = x * z, yz = y * z;
        float wx = w * x, wy = w * y, wz = w * z;
        return new CustomVector3(
            (1f - 2f * (yy + zz)) * v.x + 2f * (xy - wz) * v.y + 2f * (xz + wy) * v.z,
            2f * (xy + wz) * v.x + (1f - 2f * (xx + zz)) * v.y + 2f * (yz - wx) * v.z,
            2f * (xz - wy) * v.x + 2f * (yz + wx) * v.y + (1f - 2f * (xx + yy)) * v.z);
    }

    /// <summary>Builds a 4x4 TRS-like matrix (rotation + translation) manually, field by field.</summary>
    public Matrix4x4 ToMatrix(CustomVector3 translation)
    {
        Matrix4x4 m = Matrix4x4.identity;
        float xx = x * x, yy = y * y, zz = z * z;
        float xy = x * y, xz = x * z, yz = y * z;
        float wx = w * x, wy = w * y, wz = w * z;

        m.m00 = 1f - 2f * (yy + zz); m.m01 = 2f * (xy - wz);      m.m02 = 2f * (xz + wy);
        m.m10 = 2f * (xy + wz);      m.m11 = 1f - 2f * (xx + zz); m.m12 = 2f * (yz - wx);
        m.m20 = 2f * (xz - wy);      m.m21 = 2f * (yz + wx);      m.m22 = 1f - 2f * (xx + yy);

        m.m03 = translation.x;
        m.m13 = translation.y;
        m.m23 = translation.z;
        return m;
    }
}
