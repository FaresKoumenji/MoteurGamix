using System;

public static class PhaseEtroite
{
    public static bool SphereSphere(CustomVector3 cA, float rA,
                                    CustomVector3 cB, float rB,
                                    out Contact c)
    {
        c = default;
        CustomVector3 d = cB - cA;
        float rSum = rA + rB;
        float dist2 = d.sqrMagnitude;
        if (dist2 >= rSum * rSum) return false;

        float dist = (float)Math.Sqrt(dist2);
        c.normale = dist > 1e-6f ? d / dist : CustomVector3.up;
        c.profondeur = rSum - dist;
        float k = rA - 0.5f * c.profondeur;
        c.point = cA + c.normale * k;
        return true;
    }

    public static bool SphereAABB(CustomVector3 c, float r, in AABB b, out CustomVector3 q)
    {
        q = b.PointLePlusProche(c);
        return (c - q).sqrMagnitude <= r * r;
    }

    public static bool OverlapOBB(in OBB A, in OBB B)
    {
        CustomVector3[] uA = { A.axes.X, A.axes.Y, A.axes.Z };
        CustomVector3[] uB = { B.axes.X, B.axes.Y, B.axes.Z };
        CustomVector3 eA = A.demiTailles;
        CustomVector3 eB = B.demiTailles;
        CustomVector3 T = B.centre - A.centre;

        for (int i = 0; i < 3; i++)
        {
            if (Separe(uA[i], T, uA, eA, uB, eB)) return false;
            if (Separe(uB[i], T, uA, eA, uB, eB)) return false;
        }
        for (int i = 0; i < 3; i++)
            for (int j = 0; j < 3; j++)
            {
                CustomVector3 L = CustomVector3.Cross(uA[i], uB[j]);
                if (L.sqrMagnitude < 1e-6f) continue;
                if (Separe(L, T, uA, eA, uB, eB)) return false;
            }
        return true;
    }

    static bool Separe(CustomVector3 L, CustomVector3 T,
                       CustomVector3[] uA, CustomVector3 eA,
                       CustomVector3[] uB, CustomVector3 eB)
    {
        return Math.Abs(CustomVector3.Dot(T, L))
             > Rayon(uA, eA, L) + Rayon(uB, eB, L);
    }

    static float Rayon(CustomVector3[] u, CustomVector3 e, CustomVector3 L)
    {
        return e.x * Math.Abs(CustomVector3.Dot(u[0], L))
             + e.y * Math.Abs(CustomVector3.Dot(u[1], L))
             + e.z * Math.Abs(CustomVector3.Dot(u[2], L));
    }

    public static bool Test(CustomRigidbody a, CustomRigidbody b, out Contact c)
    {
        c = default;

        if (a.forme == FormeRigide.Sphere && b.forme == FormeRigide.Sphere)
            return SphereSphere(a.CentreMonde, a.rayon, b.CentreMonde, b.rayon, out c);

        if (a.forme == FormeRigide.Sphere && b.forme == FormeRigide.AABB)
            return SphereAABBContact(a, b, out c);

        if (a.forme == FormeRigide.AABB && b.forme == FormeRigide.Sphere)
        {
            bool ok = SphereAABBContact(b, a, out c);
            if (ok) c.normale = -c.normale;   // inverser pour que A → B
            return ok;
        }

        if (a.forme == FormeRigide.AABB && b.forme == FormeRigide.AABB)
            return AABBAABB(a, b, out c);

        if (a.forme == FormeRigide.OBB && b.forme == FormeRigide.OBB)
        {
            if (!OverlapOBB(a.OBBMonde, b.OBBMonde)) return false;
            c.normale = (b.CentreMonde - a.CentreMonde).normalized;
            c.profondeur = 0.01f;
            c.point = (a.CentreMonde + b.CentreMonde) * 0.5f;
            return true;
        }

        return false;
    }

    static bool SphereAABBContact(CustomRigidbody sphere, CustomRigidbody box, out Contact c)
    {
        c = default;
        CustomVector3 centre = sphere.CentreMonde;
        AABB b = box.Volume;

        CustomVector3 q = b.PointLePlusProche(centre);
        CustomVector3 d = centre - q;
        float dist2 = d.sqrMagnitude;

        if (dist2 > sphere.rayon * sphere.rayon) return false;

        if (dist2 > 1e-10f)
        {
            float dist = (float)Math.Sqrt(dist2);
            // Normale : de la sphère vers la boîte
            c.normale = -d / dist;
            c.profondeur = sphere.rayon - dist;
            c.point = q;
        }
        else
        {
            CustomVector3 e = b.etendue;
            CustomVector3 rel = centre - b.centre;
            float dx = e.x - Math.Abs(rel.x);
            float dy = e.y - Math.Abs(rel.y);
            float dz = e.z - Math.Abs(rel.z);

            CustomVector3 n;
            if (dx < dy && dx < dz)
                n = new CustomVector3(rel.x >= 0 ? 1f : -1f, 0, 0);
            else if (dy < dz)
                n = new CustomVector3(0, rel.y >= 0 ? 1f : -1f, 0);
            else
                n = new CustomVector3(0, 0, rel.z >= 0 ? 1f : -1f);

            // On veut la normale de la sphère vers la boîte → on inverse
            c.normale = -n;
            c.profondeur = sphere.rayon + Math.Min(dx, Math.Min(dy, dz));
            c.point = centre;
        }

        return true;
    }

    static bool AABBAABB(CustomRigidbody a, CustomRigidbody b, out Contact c)
    {
        c = default;
        AABB ba = a.Volume, bb = b.Volume;
        if (!ba.Recouvre(bb)) return false;

        float ox = Math.Min(ba.max.x, bb.max.x) - Math.Max(ba.min.x, bb.min.x);
        float oy = Math.Min(ba.max.y, bb.max.y) - Math.Max(ba.min.y, bb.min.y);
        float oz = Math.Min(ba.max.z, bb.max.z) - Math.Max(ba.min.z, bb.min.z);

        CustomVector3 centreA = ba.centre, centreB = bb.centre;

        if (ox <= oy && ox <= oz)
        {
            c.normale = new CustomVector3(centreA.x < centreB.x ? 1f : -1f, 0, 0);
            c.profondeur = ox;
            c.point = new CustomVector3((Math.Max(ba.min.x, bb.min.x) + Math.Min(ba.max.x, bb.max.x)) * 0.5f,
                                        (centreA.y + centreB.y) * 0.5f,
                                        (centreA.z + centreB.z) * 0.5f);
        }
        else if (oy <= oz)
        {
            c.normale = new CustomVector3(0, centreA.y < centreB.y ? 1f : -1f, 0);
            c.profondeur = oy;
            c.point = new CustomVector3((centreA.x + centreB.x) * 0.5f,
                                        (Math.Max(ba.min.y, bb.min.y) + Math.Min(ba.max.y, bb.max.y)) * 0.5f,
                                        (centreA.z + centreB.z) * 0.5f);
        }
        else
        {
            c.normale = new CustomVector3(0, 0, centreA.z < centreB.z ? 1f : -1f);
            c.profondeur = oz;
            c.point = new CustomVector3((centreA.x + centreB.x) * 0.5f,
                                        (centreA.y + centreB.y) * 0.5f,
                                        (Math.Max(ba.min.z, bb.min.z) + Math.Min(ba.max.z, bb.max.z)) * 0.5f);
        }
        return true;
    }
}