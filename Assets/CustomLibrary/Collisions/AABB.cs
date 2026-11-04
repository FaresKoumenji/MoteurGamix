using System;

/// <summary>
/// Axis-Aligned Bounding Box (Ch.4, slide 36) : 6 valeurs min/max.
/// Test de recouvrement par projection sur les 3 axes.
/// </summary>
[Serializable]
public struct AABB
{
    public CustomVector3 min;
    public CustomVector3 max;

    public AABB(CustomVector3 min, CustomVector3 max) { this.min = min; this.max = max; }

    public CustomVector3 centre => (min + max) * 0.5f;
    public CustomVector3 etendue => (max - min) * 0.5f;

    /// <summary>Recouvrement des projections sur x, y, z (slide 36).</summary>
    public bool Recouvre(in AABB b)
    {
        return min.x <= b.max.x && max.x >= b.min.x
            && min.y <= b.max.y && max.y >= b.min.y
            && min.z <= b.max.z && max.z >= b.min.z;
    }

    /// <summary>Point de la boîte le plus proche du point c (slide 37).</summary>
    public CustomVector3 PointLePlusProche(CustomVector3 c)
    {
        return new CustomVector3(
            Math.Clamp(c.x, min.x, max.x),
            Math.Clamp(c.y, min.y, max.y),
            Math.Clamp(c.z, min.z, max.z));
    }

    /// <summary>Construit l'AABB englobant un tableau de points.</summary>
    public static AABB Englobant(CustomVector3[] points, int n)
    {
        CustomVector3 mn = points[0], mx = points[0];
        for (int i = 1; i < n; i++)
        {
            if (points[i].x < mn.x) mn.x = points[i].x;
            if (points[i].y < mn.y) mn.y = points[i].y;
            if (points[i].z < mn.z) mn.z = points[i].z;
            if (points[i].x > mx.x) mx.x = points[i].x;
            if (points[i].y > mx.y) mx.y = points[i].y;
            if (points[i].z > mx.z) mx.z = points[i].z;
        }
        return new AABB(mn, mx);
    }
}