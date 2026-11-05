using System;
using System.Collections.Generic;

public class CouplageFluideSolide
{
    public List<CustomRigidbody> solides = new List<CustomRigidbody>();
    public float h = 0.2f;
    public float sSolide = 1f;
    public float rho0 = 1000f;
    public float traineeSolide = 1.5f;

    float poly6;

    public CouplageFluideSolide(float h = 0.2f, float rho0 = 1000f)
    {
        this.h = h;
        this.rho0 = rho0;
        poly6 = 315f / (64f * (float)Math.PI * (float)Math.Pow(h, 9));
    }

    float Poly6(float r2)
    {
        if (r2 >= h * h) return 0f;
        float d = h * h - r2;
        return poly6 * d * d * d;
    }

    /// <summary>Masse d'une sphère pleine de densité rho (kg/m3) et de rayon r (m).</summary>
    public static float MasseSphere(float rho, float rayon)
    {
        return rho * VolumeSphere(rayon);
    }

    public static float VolumeSphere(float rayon)
    {
        return (4f / 3f) * (float)Math.PI * rayon * rayon * rayon;
    }

    public void Appliquer(SystemeParticules s, float dt)
    {
        if (solides.Count == 0) return;

        int ns = solides.Count;
        CustomVector3[] xs = new CustomVector3[ns];
        float[] rs = new float[ns];
        for (int k = 0; k < ns; k++)
        {
            xs[k] = solides[k].CentreMonde;
            rs[k] = solides[k].rayon;
        }

        for (int k = 0; k < ns; k++)
        {
            if (solides[k].invMass <= 0f) continue;

            CustomVector3 velSolide = solides[k].velocity;
            CustomVector3 vEau = CustomVector3.zero;
            int nEau = 0;
            float rhoEau = 0f;      // densité SPH de l'eau au centre du solide

            for (int i = 0; i < s.N; i++)
            {
                if (s.p[i].invmass <= 0f) continue;
                CustomVector3 d = s.p[i].x - xs[k];
                float dist = d.magnitude;
                float seuil = rs[k] + h;
                if (dist > seuil || dist < 1e-6f) continue;

                // Correction de position (les particules d'eau ne peuvent pas entrer dans le solide)
                if (dist < rs[k])
                {
                    float profondeur = rs[k] - dist;
                    CustomVector3 n = d / dist;
                    s.p[i].x = s.p[i].x + n * profondeur;
                    float vrel = CustomVector3.Dot(s.p[i].v - velSolide, n);
                    if (vrel < 0f) s.p[i].v = s.p[i].v - n * vrel;
                }

                // Densité d'eau au centre du solide : rho = somme m_j W(|x_c - x_j|)
                float w = Poly6(dist * dist);
                if (w <= 0f) continue;
                rhoEau += s.p[i].m * w;

                // Pour la traînée visqueuse
                vEau += s.p[i].v;
                nEau++;
            }

            // Fraction immergée : 0 dans l'air, 1 dans l'eau, ~0.5 quand le centre est à la surface
            float immersion = Math.Min(1f, Math.Max(0f, rhoEau / rho0));

            // Poussée d'Archimède : F = immersion . rho0 . V . g, vers le haut (-g), appliquée au centre
            float poussee = immersion * rho0 * VolumeSphere(rs[k]) * 9.81f * sSolide;
            CustomVector3 impulsion = new CustomVector3(0f, poussee * dt, 0f);

            // Traînée visqueuse : seulement dans l'eau
            if (nEau > 0) vEau = vEau / nEau;
            CustomVector3 vRel = velSolide - vEau;
            CustomVector3 forceTrainee = vRel * (-traineeSolide * solides[k].Mass * immersion);

            CustomVector3 impTotale = impulsion + forceTrainee * dt;
            solides[k].AppliquerImpulsion(impTotale, xs[k]);
        }
    }
}