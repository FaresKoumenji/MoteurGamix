using System;

/// <summary>
/// Plan infini n.P + D = 0 (Ch.3, slides 22 à 24) comme obstacle pour les particules.
/// Contact (Ch.4, application 1) : distance signée sd = n.P + D - rayon ; si sd &lt; 0 :
///   - on replace la particule sur le plan (P -= n.sd) ;
///   - restitution : vN' = -e.vN  (e = restitution) ;
///   - frottement de Coulomb : la vitesse tangentielle diminue de mu.(1+e).|vN| au plus.
/// Un contact quasi au repos (|vN| faible) a e = 0 pour éviter le tremblement.
/// </summary>
public class ContraintePlan : IContrainte
{
    public CustomVector3 n;            // normale unitaire
    public float D;                    // n.P + D = 0
    public float rayon = 0f;
    public float restitution = 0.3f;   // e
    public float friction = 0.4f;      // mu
    public float seuilRepos = 0.05f;   // m/s
    public int nbContacts;

    public ContraintePlan(CustomVector3 normale, CustomVector3 point)
    {
        n = normale.normalized;
        D = -CustomVector3.Dot(n, point);
    }

    public void Projeter(SystemeParticules s)
    {
        for (int i = 0; i < s.N; i++)
        {
            if (s.p[i].invmass <= 0f) continue;

            float sd = CustomVector3.Dot(n, s.p[i].x) + D - rayon;
            if (sd >= 0f) continue;

            nbContacts++;
            s.p[i].x = s.p[i].x - n * sd;

            float vn = CustomVector3.Dot(s.p[i].v, n);
            if (vn >= 0f) continue;                       // s'éloigne déjà

            float e = (-vn < seuilRepos) ? 0f : restitution;
            CustomVector3 vT = s.p[i].v - n * vn;
            float vtm = vT.magnitude;
            if (vtm > 1e-6f)
            {
                float reduction = Math.Min(vtm, friction * (1f + e) * (-vn));
                vT = vT * ((vtm - reduction) / vtm);
            }
            s.p[i].v = vT + n * (-e * vn);
        }
    }

    public void ApresPas(SystemeParticules s) { }
}
