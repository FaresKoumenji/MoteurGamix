using System;

public class ContrainteDensite : IContrainte
{
    public float h = 0.2f;
    public float rho0 = 1000f;
    public float lambdaRelaxation = 1f;
    public GrilleVoisins grille;

    float poly6;
    float spiky;

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

    public void Projeter(SystemeParticules s)
    {
        int N = s.N;
        if (N == 0) return;

        // Construire la grille avec les positions ACTUELLES
        CustomVector3[] positions = new CustomVector3[N];
        for (int i = 0; i < N; i++) positions[i] = s.p[i].x;
        grille.Construire(positions, N);

        float w0 = Poly6(0f);

        // 1) Densité de chaque particule
        float[] rho = new float[N];
        for (int i = 0; i < N; i++)
        {
            float ri = 0f;
            int self = i;
            grille.PourChaqueVoisin(positions, i, j =>
            {
                CustomVector3 d = positions[self] - positions[j];
                ri += s.p[j].m * Poly6(d.sqrMagnitude);
            });
            ri += s.p[i].m * w0;
            rho[i] = ri;
        }

        // 2) Correction itérative (2 passes pour stabiliser)
        for (int passe = 0; passe < 2; passe++)
        {
            for (int i = 0; i < N; i++)
            {
                if (s.p[i].invmass <= 0f) continue;
                float C = rho[i] / rho0 - 1f;
                if (C <= 0f) continue;

                CustomVector3 grad = CustomVector3.zero;
                int self = i;
                grille.PourChaqueVoisin(positions, i, j =>
                {
                    CustomVector3 d = positions[self] - positions[j];
                    grad += SpikyGradient(d) * s.p[j].m;
                });
                grad = grad * (1f / rho0);

                float denom = grad.sqrMagnitude;
                if (denom < 1e-12f) continue;

                float lambda = -C / denom;
                CustomVector3 dx = grad * (lambda * s.p[i].invmass * lambdaRelaxation);
                s.p[i].x = s.p[i].x + dx;

                // Mettre à jour la position dans le buffer pour la 2e passe
                positions[i] = s.p[i].x;
            }

            // Recalculer la densité pour la 2e passe
            if (passe == 0)
            {
                for (int i = 0; i < N; i++)
                {
                    float ri = s.p[i].m * w0;
                    int self = i;
                    grille.PourChaqueVoisin(positions, i, j =>
                    {
                        CustomVector3 d = positions[self] - positions[j];
                        ri += s.p[j].m * Poly6(d.sqrMagnitude);
                    });
                    rho[i] = ri;
                }
            }
        }
    }
}