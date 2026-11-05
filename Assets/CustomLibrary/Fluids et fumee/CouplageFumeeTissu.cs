using UnityEngine;

/// <summary>Réglages du couplage fumée <-> tissu (visibles dans l'Inspector du scénario).</summary>
[System.Serializable]
public class ParametresCouplage
{
    public bool actif = true;
    [Tooltip("Distance d'interaction entre une particule de fumée et une particule du tissu (m)")]
    public float rayon = 0.3f;
    [Tooltip("Force (N) sur une particule du tissu quand une fumée est tout près")]
    public float force = 0.4f;
    [Range(0f, 1f)] [Tooltip("Part de la poussée dirigée vers l'extérieur (le reste pousse vers le haut)")]
    public float etalement = 0.35f;
    [Tooltip("Accélération maximale (m/s²) cumulée sur une particule du tissu")]
    public float accelMax = 40f;

    [Tooltip("Masse effective d'une particule de fumée (kg) : petit = facilement déviée")]
    public float masseFumee = 0.05f;
    [Tooltip("1 = Newton exact : la fumée subit la force opposée")]
    public float reaction = 1f;
    [Range(0f, 1f)] [Tooltip("0 = la fumée s'écrase, 1 = rebond élastique")]
    public float rebond = 0.4f;
    [Tooltip("Le tissu en mouvement emporte la fumée (par seconde)")]
    public float entrainement = 6f;
}

/// <summary>
/// Module physique : échange de quantité de mouvement entre deux SystemeParticules
/// (la fumée et le tissu). La fumée pousse le tissu ; le tissu bloque, dévie et entraîne la fumée.
/// Appeler Appliquer(dt) une fois par image.
/// </summary>
public class CouplageFumeeTissu
{
    private readonly SystemeParticules fumee;
    private readonly SystemeParticules tissu;
    private readonly ParametresCouplage param;
    private Vector3[] forcesTissu;

    public CouplageFumeeTissu(SystemeParticules fumee, SystemeParticules tissu, ParametresCouplage param)
    {
        this.fumee = fumee;
        this.tissu = tissu;
        this.param = param;
        forcesTissu = new Vector3[tissu.N];
    }

    public void Appliquer(float dt)
    {
        if (!param.actif || dt <= 0f) return;
        System.Array.Clear(forcesTissu, 0, forcesTissu.Length);
        float r2 = param.rayon * param.rayon;

        for (int k = 0; k < fumee.N; k++)
        {
            if (fumee.p[k].invmass <= 0f) continue;
            Vector3 xk = fumee.p[k].x.ToUnity();

            Vector3 reac = Vector3.zero, vitTissu = Vector3.zero, normale = Vector3.zero;
            float poids = 0f;

            for (int i = 0; i < tissu.N; i++)
            {
                if (tissu.p[i].invmass <= 0f) continue;
                Vector3 d = tissu.p[i].x.ToUnity() - xk;
                float d2 = d.sqrMagnitude;
                if (d2 >= r2) continue;

                float dist = Mathf.Sqrt(d2);
                float f = 1f - dist / param.rayon;                 // 1 tout près, 0 au bord
                Vector3 dirExt = dist > 1e-4f ? d / dist : Vector3.zero;
                dirExt.y = Mathf.Max(0f, dirExt.y);
                Vector3 poussee = Vector3.up * (1f - param.etalement) + dirExt * param.etalement;
                Vector3 force = poussee * (param.force * f);
                forcesTissu[i] += force;

                reac -= force;                                     // 3e loi de Newton
                vitTissu += tissu.p[i].v.ToUnity() * f;
                if (dist > 1e-4f) normale -= d / dist * f;         // tissu -> fumée
                poids += f;
            }

            if (poids <= 0f) continue;

            Vector3 v = fumee.p[k].v.ToUnity();
            v += reac * (param.reaction / param.masseFumee) * dt;  // réaction

            Vector3 n = normale.sqrMagnitude > 1e-8f ? normale.normalized : Vector3.down;
            float vn = Vector3.Dot(v, n);
            if (vn < 0f)                                           // blocage : ne traverse pas le tissu
                v -= n * (vn * (1f + param.rebond) * Mathf.Clamp01(poids * 0.5f));

            v += (vitTissu / poids - v) * Mathf.Clamp01(param.entrainement * poids * dt);  // entraînement

            fumee.p[k].v = new CustomVector3(v.x, v.y, v.z);
        }

        // Application sur le tissu : a = F / m (modification de la vitesse)
        for (int i = 0; i < tissu.N; i++)
        {
            if (tissu.p[i].invmass <= 0f) continue;
            Vector3 a = forcesTissu[i] * tissu.p[i].invmass;
            if (a.sqrMagnitude < 1e-8f) continue;
            if (a.magnitude > param.accelMax) a = a.normalized * param.accelMax;
            tissu.p[i].v = tissu.p[i].v + new CustomVector3(a.x * dt, a.y * dt, a.z * dt);
        }
    }
}
