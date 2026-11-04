using System;

/// <summary>
/// Contrainte de plan pour les particules du fluide (FLEX §7.1, "position projection
/// during the regular particle collision pipeline"). Variante de ContraintePlan
/// qui ne fait que projeter (pas de réponse élastique : l'eau reste au repos au fond).
/// </summary>
public class ContraintePlanFluide : IContrainte
{
    public CustomVector3 n;
    public float D;
    public float rayon = 0f;

    public ContraintePlanFluide(CustomVector3 normale, CustomVector3 point)
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
            s.p[i].x = s.p[i].x - n * sd;

            // amortir la vitesse normale (évite le rebond)
            float vn = CustomVector3.Dot(s.p[i].v, n);
            if (vn < 0f) s.p[i].v = s.p[i].v - n * vn;
        }
    }
}