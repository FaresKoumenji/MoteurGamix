using System;
using System.Collections.Generic;

public class CouplageFluideSolide
{
    public List<CustomRigidbody> solides = new List<CustomRigidbody>();
    public float h = 0.2f;
    public float sSolide = 1f;
    public float rho0 = 1000f;
    public float traineeSolide = 1.5f;
    /// <summary>Espacement initial des particules d'eau (renseigné par Fluide). 0 = déduit de h (h = 2.2 x espacement).</summary>
    public float espacement = 0f;

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

            // Surface locale de l'eau sous/autour du solide, déduite des particules
            float esp = espacement > 0f ? espacement : h / 2.2f;
            float rHoriz = rs[k] + esp;
            float ySurface = float.NegativeInfinity;

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

                // Surface locale : la particule la plus haute dans l'empreinte horizontale du solide
                // (on ignore celles qui sont bien au-dessus du solide : éclaboussures)
                if (d.x * d.x + d.z * d.z <= rHoriz * rHoriz && s.p[i].x.y <= xs[k].y + rs[k] + esp)
                    ySurface = Math.Max(ySurface, s.p[i].x.y + 0.5f * esp);

                // Pour la traînée visqueuse
                vEau += s.p[i].v;
                nEau++;
            }

            // Volume immergé : calotte sphérique de hauteur delta sous la surface locale
            //   V_calotte = pi . delta^2 . (3r - delta) / 3     (delta = 0 : hors de l'eau, delta = 2r : tout immergé)
            float r = rs[k];
            float delta = 0f;
            if (!float.IsNegativeInfinity(ySurface))
                delta = Math.Min(2f * r, Math.Max(0f, ySurface - (xs[k].y - r)));
            float vImmerge = (float)Math.PI * delta * delta * (3f * r - delta) / 3f;
            float immersion = vImmerge / VolumeSphere(r);

            // Poussée d'Archimède : F = rho0 . V_immergé . g, vers le haut, appliquée au centre
            float poussee = rho0 * vImmerge * 9.81f * sSolide;
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