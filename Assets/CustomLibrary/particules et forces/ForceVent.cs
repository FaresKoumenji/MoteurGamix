using System;

/// <summary>
/// Ventilateur / vent sur un tissu (article FLEX, section 8 : chaque triangle est un profil mince).
/// Pour chaque triangle : vitesse relative vrel = v_triangle - v_vent, normale n, aire A,
/// force  F = -coefficient . A . (vrel . n) n , répartie sur les 3 particules.
/// Le champ de vent est un cône le long de l'axe du ventilateur, atténué avec la distance,
/// avec une turbulence (rafales) sinusoïdale.
/// </summary>
public class ForceVent : IForce
{
    public int[] triangles;                       // 3 indices par triangle
    public bool actif = true;

    public CustomVector3 origine = CustomVector3.zero;
    public CustomVector3 axe = CustomVector3.forward;   // unitaire
    public float vitesse = 8f;                    // m/s au centre, près du ventilateur
    public float rayon = 2f;                      // rayon du jet à l'origine
    public float portee = 8f;                     // distance maximale
    public float coefficient = 0.5f;              // ~ masse volumique de l'air x coefficient de traînée
    public float turbulence = 0.3f;               // amplitude relative des rafales
    public CustomVector3 ventAmbiant = CustomVector3.zero;

    public ForceVent(int[] triangles) { this.triangles = triangles; }

    /// <summary>Vitesse du vent au point pos, à l'instant t.</summary>
    public CustomVector3 Vent(CustomVector3 pos, float t)
    {
        CustomVector3 w = ventAmbiant;
        if (!actif) return w;

        CustomVector3 r = pos - origine;
        float a = CustomVector3.Dot(r, axe);
        if (a > 0f && a < portee)
        {
            CustomVector3 radial = r - axe * a;
            float rd = radial.magnitude;
            float rayonLocal = rayon + 0.2f * a;                 // le jet s'élargit
            if (rd < rayonLocal)
            {
                float att = (1f - a / portee) * (1f - rd / rayonLocal);
                float rafale = 1f + turbulence * (float)Math.Sin(3.1f * t + 5f * pos.x + 2f * pos.y);
                w += axe * (vitesse * att * rafale);
            }
        }
        return w;
    }

    public void Appliquer(SystemeParticules s, CustomVector3[] x, CustomVector3[] v, CustomVector3[] f)
    {
        if (!actif && ventAmbiant.sqrMagnitude < 1e-12f) return;

        for (int q = 0; q + 2 < triangles.Length; q += 3)
        {
            int ia = triangles[q], ib = triangles[q + 1], ic = triangles[q + 2];

            CustomVector3 n = CustomVector3.Cross(x[ib] - x[ia], x[ic] - x[ia]);
            float deuxAire = n.magnitude;
            if (deuxAire < 1e-9f) continue;
            CustomVector3 nh = n / deuxAire;
            float aire = 0.5f * deuxAire;

            CustomVector3 centre = (x[ia] + x[ib] + x[ic]) / 3f;
            CustomVector3 vTri = (v[ia] + v[ib] + v[ic]) / 3f;
            CustomVector3 vrel = vTri - Vent(centre, s.t);

            CustomVector3 force = nh * (-coefficient * aire * CustomVector3.Dot(vrel, nh));
            CustomVector3 part = force / 3f;
            f[ia] += part; f[ib] += part; f[ic] += part;
        }
    }

    public void ApresPas(SystemeParticules s) { }
}
