using System;
using System.Collections.Generic;

/// <summary>
/// Contrainte de contact entre particules (FLEX Eq. 22) :
///   C(xi, xj) = |xi - xj| - r >= 0
/// Projette les particules qui se chevauchent au prorata de leurs masses inverses.
/// Générique : sert au tissu, aux cubes en particules, aux fluides, etc.
/// </summary>
public class ContrainteParticuleParticule : IContrainte
{
    public float rayonContact = 0.05f;
    public GrilleVoisins grille;

    public ContrainteParticuleParticule(float rayonContact)
    {
        this.rayonContact = rayonContact;
        grille = new GrilleVoisins { h = rayonContact * 2.2f };
    }

    public void Projeter(SystemeParticules s)
    {
        int N = s.N;
        if (N < 2) return;

        CustomVector3[] positions = new CustomVector3[N];
        for (int i = 0; i < N; i++) positions[i] = s.p[i].x;
        grille.Construire(positions, N);

        float r2 = rayonContact * rayonContact;

        for (int i = 0; i < N; i++)
        {
            if (s.p[i].invmass <= 0f) continue;
            float wi = s.p[i].invmass;

            grille.PourChaqueVoisin(positions, i, j =>
            {
                if (j <= i) return;
                if (s.p[j].invmass <= 0f) return;
                float wj = s.p[j].invmass;
                float ws = wi + wj;
                if (ws <= 0f) return;

                CustomVector3 d = positions[i] - positions[j];
                float d2 = d.sqrMagnitude;
                if (d2 >= r2 || d2 < 1e-12f) return;

                float dist = (float)Math.Sqrt(d2);
                CustomVector3 n = d / dist;
                float pen = rayonContact - dist;

                s.p[i].x = s.p[i].x + n * (pen * wi / ws);
                s.p[j].x = s.p[j].x - n * (pen * wj / ws);

                positions[i] = s.p[i].x;
                positions[j] = s.p[j].x;
            });
        }
    }
}