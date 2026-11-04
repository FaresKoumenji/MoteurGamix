using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Couplage particules ↔ corps rigides (FLEX §7.1).
/// </summary>
public class ContrainteParticuleRigidbody : IContrainte
{
    public List<CustomRigidbody> corps = new List<CustomRigidbody>();
    public float rayonParticule = 0.05f;

    public ContrainteParticuleRigidbody(List<CustomRigidbody> corps, float rayonParticule = 0.05f)
    {
        this.corps = corps;
        this.rayonParticule = rayonParticule;
    }

    public void Projeter(SystemeParticules s)
    {
        if (corps.Count == 0) return;

        for (int k = 0; k < corps.Count; k++)
        {
            CustomRigidbody rb = corps[k];
            if (rb == null) continue;
            if (rb.invMass <= 0f) continue;

            AABB b = rb.Volume;

            for (int i = 0; i < s.N; i++)
            {
                if (s.p[i].invmass <= 0f) continue;

                CustomVector3 p = s.p[i].x;
                CustomVector3 q = b.PointLePlusProche(p);
                CustomVector3 d = p - q;
                float dist2 = d.sqrMagnitude;

                if (dist2 > rayonParticule * rayonParticule) continue;

                CustomVector3 n;
                float profondeur;

                if (dist2 > 1e-10f)
                {
                    float dist = (float)Math.Sqrt(dist2);
                    n = d / dist;
                    profondeur = rayonParticule - dist;
                }
                else
                {
                    CustomVector3 e = b.etendue;
                    CustomVector3 rel = p - b.centre;
                    float dx = e.x - Math.Abs(rel.x);
                    float dy = e.y - Math.Abs(rel.y);
                    float dz = e.z - Math.Abs(rel.z);
                    if (dx < dy && dx < dz)
                        n = new CustomVector3(rel.x >= 0 ? 1f : -1f, 0, 0);
                    else if (dy < dz)
                        n = new CustomVector3(0, rel.y >= 0 ? 1f : -1f, 0);
                    else
                        n = new CustomVector3(0, 0, rel.z >= 0 ? 1f : -1f);
                    profondeur = rayonParticule + Math.Min(dx, Math.Min(dy, dz));
                }

                s.p[i].x = s.p[i].x + n * profondeur;

                CustomVector3 vPart = s.p[i].v;
                CustomVector3 vCorps = rb.VitesseAuPoint(p);
                CustomVector3 vRel = vPart - vCorps;
                float vn = CustomVector3.Dot(vRel, n);

                if (vn < 0f)
                {
                    float massePart = Math.Max(s.p[i].m, 1e-4f);
                    CustomVector3 impulsion = n * (massePart * (-vn));
                    rb.AppliquerImpulsion(-impulsion, p);
                }
            }
        }
    }
}