using System;

public enum FixationTissu { ColonneGauche, RangeeHaute, DeuxCoinsHauts, Aucune }

/// <summary>
/// Tissu = grille nx x ny de particules reliées par un réseau de ressorts (Ch.2 Partie 1, slide 29) :
///   structurels (voisins), de cisaillement (diagonales) et de flexion (voisins de voisins).
/// Plan initial : XY, i le long de +x, j vers le bas (-y). La liste des triangles sert au vent.
/// </summary>
public class Tissu
{
    public int nx, ny;
    public SystemeParticules systeme;
    public ForceRessort ressorts;
    public int[] triangles;

    public int Index(int i, int j) { return j * nx + i; }

    public Tissu(int nx, int ny, float largeur, float hauteur, CustomVector3 origine,
                 float masseTotale, float k, float c, float seuilRupture, FixationTissu fixation)
    {
        this.nx = nx; this.ny = ny;
        int n = nx * ny;
        systeme = new SystemeParticules(n, "Tissu");

        float dx = largeur / (nx - 1);
        float dy = hauteur / (ny - 1);
        float m = masseTotale / n;

        for (int j = 0; j < ny; j++)
            for (int i = 0; i < nx; i++)
            {
                int idx = Index(i, j);
                float bruit = 0.01f * (float)Math.Sin(i * 12.9898f + j * 78.233f);   // brise la symétrie
                systeme.p[idx].x = new CustomVector3(origine.x + i * dx, origine.y - j * dy, origine.z + bruit);
                systeme.p[idx].v = CustomVector3.zero;
                systeme.p[idx].m = m;
                systeme.p[idx].invmass = 1f / m;
                systeme.p[idx].phase = 0;
            }

        // Réseau de ressorts (les positions initiales fixent les longueurs au repos L0)
        ressorts = new ForceRessort();
        ressorts.seuilRupture = seuilRupture;

        for (int j = 0; j < ny; j++)
            for (int i = 0; i < nx; i++)
            {
                int a = Index(i, j);
                if (i + 1 < nx) ressorts.Ajouter(systeme, a, Index(i + 1, j), k, c, ForceRessort.Structurel, true);
                if (j + 1 < ny) ressorts.Ajouter(systeme, a, Index(i, j + 1), k, c, ForceRessort.Structurel, true);

                if (i + 1 < nx && j + 1 < ny)
                {
                    ressorts.Ajouter(systeme, a, Index(i + 1, j + 1), k * 0.5f, c, ForceRessort.Cisaillement, true);
                    ressorts.Ajouter(systeme, Index(i + 1, j), Index(i, j + 1), k * 0.5f, c, ForceRessort.Cisaillement, true);
                }

                if (i + 2 < nx) ressorts.Ajouter(systeme, a, Index(i + 2, j), k * 0.3f, c, ForceRessort.Flexion, false);
                if (j + 2 < ny) ressorts.Ajouter(systeme, a, Index(i, j + 2), k * 0.3f, c, ForceRessort.Flexion, false);
            }

        // Triangles (deux par case)
        triangles = new int[(nx - 1) * (ny - 1) * 6];
        int t = 0;
        for (int j = 0; j < ny - 1; j++)
            for (int i = 0; i < nx - 1; i++)
            {
                int a = Index(i, j), b = Index(i + 1, j), cc = Index(i, j + 1), d = Index(i + 1, j + 1);
                triangles[t++] = a; triangles[t++] = b; triangles[t++] = cc;
                triangles[t++] = b; triangles[t++] = d; triangles[t++] = cc;
            }

        // Fixation (particules clouées : invmass = 0)
        switch (fixation)
        {
            case FixationTissu.ColonneGauche:
                for (int j = 0; j < ny; j++) systeme.Fixer(Index(0, j));
                break;
            case FixationTissu.RangeeHaute:
                for (int i = 0; i < nx; i++) systeme.Fixer(Index(i, 0));
                break;
            case FixationTissu.DeuxCoinsHauts:
                systeme.Fixer(Index(0, 0));
                systeme.Fixer(Index(nx - 1, 0));
                break;
        }

        systeme.forces.Add(ressorts);
    }
}
