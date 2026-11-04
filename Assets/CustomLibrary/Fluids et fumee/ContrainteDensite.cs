using System;
using System.Collections.Generic;

/// <summary>
/// Contrainte de densité unilatérale (FLEX §7, Eq. 26) : C_i = rho_i / rho0 - 1 <= 0.
/// Projection PBD : lambda_i = -C_i / ( sum_k w_k |grad_k C_i|^2 + eps ),  dx_k = w_k . lambda_i . grad_k C_i
///   grad_i C_i = (1/rho0) sum_j m_j gradW(xi - xj)      grad_j C_i = -(m_j/rho0) gradW(xi - xj)
/// </summary>
public class ContrainteDensite : IContrainte
{
    public float h = 0.2f;
    public float rho0 = 1000f;
    public float lambdaRelaxation = 1f;
    public int iterations = 3;          // passes de Gauss-Seidel par appel
    public float epsilon = 1e-4f;       // régularisation du dénominateur
    public float rayonContact = 0f;     // distance minimale entre 2 particules (0 = désactivé), FLEX Eq. 22
    public GrilleVoisins grille;

    float poly6;
    float spiky;

    CustomVector3[] pos;
    List<int>[] vois;
    readonly List<CustomVector3> gBuf = new List<CustomVector3>(64);

    public ContrainteDensite(float h, float rho0)
    {
        this.h = h;
        this.rho0 = rho0;
        poly6 = 315f / (64f * (float)Math.PI * (float)Math.Pow(h, 9));
        spiky = -45f / ((float)Math.PI * (float)Math.Pow(h, 6));
        grille = new GrilleVoisins { h = h };
    }

    public float Poly6(float r2)
    {
        if (r2 >= h * h) return 0f;
        float d = h * h - r2;
        return poly6 * d * d * d;
    }

    public CustomVector3 SpikyGradient(CustomVector3 r)
    {
        float r2 = r.sqrMagnitude;
        if (r2 >= h * h || r2 < 1e-12f) return CustomVector3.zero;
        float rl = (float)Math.Sqrt(r2);
        float d = h - rl;
        return r * (spiky * d * d / rl);
    }

    void Preparer(SystemeParticules s)
    {
        int N = s.N;
        if (pos == null || pos.Length != N)
        {
            pos = new CustomVector3[N];
            vois = new List<int>[N];
            for (int i = 0; i < N; i++) vois[i] = new List<int>(32);
        }
        for (int i = 0; i < N; i++) pos[i] = s.p[i].x;

        // Voisins trouvés une fois au début (comme FLEX §9), pas à chaque itération
        grille.Construire(pos, N);
        for (int i = 0; i < N; i++) grille.Voisins(pos, i, vois[i]);
    }

    public void Projeter(SystemeParticules s)
    {
        int N = s.N;
        if (N == 0) return;

        Preparer(s);
        float w0 = Poly6(0f);

        for (int it = 0; it < iterations; it++)
        {
            for (int i = 0; i < N; i++)
            {
                float mi = s.p[i].m;
                if (mi <= 0f) continue;
                List<int> vi = vois[i];

                // densité courante de i
                float rho = mi * w0;
                for (int q = 0; q < vi.Count; q++)
                {
                    int j = vi[q];
                    rho += s.p[j].m * Poly6((pos[i] - pos[j]).sqrMagnitude);
                }

                float C = rho / rho0 - 1f;
                if (C <= 0f) continue;                  // unilatérale : sépare seulement

                // gradients
                CustomVector3 gi = CustomVector3.zero;
                float den = 0f;
                gBuf.Clear();
                for (int q = 0; q < vi.Count; q++)
                {
                    int j = vi[q];
                    CustomVector3 g = SpikyGradient(pos[i] - pos[j]) * (s.p[j].m / rho0);
                    gBuf.Add(g);
                    gi += g;
                    den += s.p[j].invmass * g.sqrMagnitude;     // |grad_j C_i|^2 = |g|^2
                }
                den += s.p[i].invmass * gi.sqrMagnitude + epsilon;

                float lambda = -C / den * lambdaRelaxation;

                // déplacer i
                if (s.p[i].invmass > 0f)
                {
                    s.p[i].x = s.p[i].x + gi * (s.p[i].invmass * lambda);
                    pos[i] = s.p[i].x;
                }
                // déplacer les voisins (grad_j C_i = -g)
                for (int q = 0; q < vi.Count; q++)
                {
                    int j = vi[q];
                    float wj = s.p[j].invmass;
                    if (wj <= 0f) continue;
                    s.p[j].x = s.p[j].x + gBuf[q] * (-wj * lambda);
                    pos[j] = s.p[j].x;
                }
            }

            if (rayonContact > 0f) ProjeterContacts(s);
        }
    }

    /// <summary>
    /// Contrainte de contact C(xi,xj) = |xij| - r >= 0 (FLEX Eq. 22), répartie selon les masses inverses.
    /// Nécessaire car la contrainte de densité est unilatérale : en surface rho &lt; rho0, donc rien ne sépare
    /// deux particules qui se chevauchent.
    /// </summary>
    void ProjeterContacts(SystemeParticules s)
    {
        int N = s.N;
        float r = rayonContact;
        float r2 = r * r;

        for (int i = 0; i < N; i++)
        {
            float wi = s.p[i].invmass;
            List<int> vi = vois[i];
            for (int q = 0; q < vi.Count; q++)
            {
                int j = vi[q];
                if (j <= i) continue;                       // chaque paire une seule fois
                float wj = s.p[j].invmass;
                float ws = wi + wj;
                if (ws <= 0f) continue;

                CustomVector3 d = pos[i] - pos[j];
                float d2 = d.sqrMagnitude;
                if (d2 >= r2) continue;

                CustomVector3 n;
                float dist;
                if (d2 < 1e-12f) { n = new CustomVector3(0f, 1f, 0f); dist = 0f; }   // particules confondues
                else { dist = (float)Math.Sqrt(d2); n = d * (1f / dist); }

                float pen = r - dist;
                if (wi > 0f) { s.p[i].x = s.p[i].x + n * (pen * wi / ws); pos[i] = s.p[i].x; }
                if (wj > 0f) { s.p[j].x = s.p[j].x + n * (-pen * wj / ws); pos[j] = s.p[j].x; }
            }
        }
    }
}