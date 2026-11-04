using System;
using UnityEngine;

/// <summary>
/// Orientation matrix R(t) = ( X'  Y'  Z' ), one column per body axis expressed in the world frame
/// (Ch.2 Part 2, slides 34-36).
/// </summary>
[Serializable]
public struct CustomMatrix3
{
    public CustomVector3 X; // column 1 : X'
    public CustomVector3 Y; // column 2 : Y'
    public CustomVector3 Z; // column 3 : Z'

    public CustomMatrix3(CustomVector3 x, CustomVector3 y, CustomVector3 z)
    {
        X = x; Y = y; Z = z;
    }

    public static CustomMatrix3 identity =>
        new CustomMatrix3(CustomVector3.right, CustomVector3.up, CustomVector3.forward);

    // ---- Basic operations -------------------------------------------------

    /// <summary>R * v</summary>
    public CustomVector3 Multiply(CustomVector3 v)
    {
        return X * v.x + Y * v.y + Z * v.z;
    }

    /// <summary>R^T * v  (the rows of R are the columns of R^T)</summary>
    public CustomVector3 MultiplyTranspose(CustomVector3 v)
    {
        return new CustomVector3(
            CustomVector3.Dot(X, v),
            CustomVector3.Dot(Y, v),
            CustomVector3.Dot(Z, v));
    }

    public static CustomMatrix3 operator *(CustomMatrix3 a, CustomMatrix3 b)
    {
        return new CustomMatrix3(a.Multiply(b.X), a.Multiply(b.Y), a.Multiply(b.Z));
    }

    public static CustomMatrix3 operator +(CustomMatrix3 a, CustomMatrix3 b)
    {
        return new CustomMatrix3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    }

    public static CustomMatrix3 operator *(CustomMatrix3 a, float s)
    {
        return new CustomMatrix3(a.X * s, a.Y * s, a.Z * s);
    }

    // ---- Course formulas --------------------------------------------------

    /// <summary>
    /// dR/dt = w* R  (slide 35 / 41). The skew-symmetric matrix w* applied to each column
    /// gives  dX'/dt = w ^ X',  dY'/dt = w ^ Y',  dZ'/dt = w ^ Z'.
    /// </summary>
    public CustomMatrix3 SkewTimes(CustomVector3 w)
    {
        return new CustomMatrix3(
            CustomVector3.Cross(w, X),
            CustomVector3.Cross(w, Y),
            CustomVector3.Cross(w, Z));
    }

    /// <summary>
    /// Gram-Schmidt re-orthonormalization (slide 36):
    /// normalize T, remove the projection of N on T', normalize N, B' = T' ^ N'.
    /// </summary>
    public CustomMatrix3 Orthonormalized()
    {
        CustomVector3 t = X.normalized;
        CustomVector3 n = Y - t * CustomVector3.Dot(Y, t);
        n = n.normalized;
        CustomVector3 b = CustomVector3.Cross(t, n);
        return new CustomMatrix3(t, n, b);
    }

    // ---- Initial orientation (Ch.1: Rx, Ry, Rz) ---------------------------

    public static CustomMatrix3 Rx(float beta)
    {
        float c = (float)Math.Cos(beta), s = (float)Math.Sin(beta);
        return new CustomMatrix3(
            new CustomVector3(1f, 0f, 0f),
            new CustomVector3(0f, c, s),
            new CustomVector3(0f, -s, c));
    }

    public static CustomMatrix3 Ry(float alpha)
    {
        float c = (float)Math.Cos(alpha), s = (float)Math.Sin(alpha);
        return new CustomMatrix3(
            new CustomVector3(c, 0f, -s),
            new CustomVector3(0f, 1f, 0f),
            new CustomVector3(s, 0f, c));
    }

    public static CustomMatrix3 Rz(float gamma)
    {
        float c = (float)Math.Cos(gamma), s = (float)Math.Sin(gamma);
        return new CustomMatrix3(
            new CustomVector3(c, s, 0f),
            new CustomVector3(-s, c, 0f),
            new CustomVector3(0f, 0f, 1f));
    }

    /// <summary>R = Ry * Rx * Rz, angles in degrees.</summary>
    public static CustomMatrix3 FromEulerDegrees(CustomVector3 euler)
    {
        float d2r = (float)Math.PI / 180f;
        return Ry(euler.y * d2r) * Rx(euler.x * d2r) * Rz(euler.z * d2r);
    }

    // ---- Display: 4x4 homogeneous matrix (Ch.1), filled manually ----------

    public Matrix4x4 ToMatrix4x4(CustomVector3 translation)
    {
        Matrix4x4 m = Matrix4x4.identity;

        m.m00 = X.x; m.m01 = Y.x; m.m02 = Z.x; m.m03 = translation.x;
        m.m10 = X.y; m.m11 = Y.y; m.m12 = Z.y; m.m13 = translation.y;
        m.m20 = X.z; m.m21 = Y.z; m.m22 = Z.z; m.m23 = translation.z;

        return m;
    }
}
