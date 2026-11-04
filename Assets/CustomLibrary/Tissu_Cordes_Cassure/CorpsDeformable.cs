using System;

/// <summary>
/// Corps déformable cassable : bloc nx x ny x nz de particules sur un réseau cubique,
/// reliées par des ressorts rompables (article FLEX, sections 5.3 et 8 : déformation, rupture).
///   - voisins de face : ressorts structurels ;
///   - arêtes et diagonales du cube : ressorts de cisaillement (k/2), sinon le bloc s'écrase.
/// Quand un ressort dépasse L0 (1 + seuilRupture) il casse : le bloc se fissure puis se fragmente.
/// </summary>
public class CorpsDeformable
{
    public int nx, ny, nz;
    public SystemeParticules systeme;
    public ForceRessort ressorts;

    public int Index(int i, int j, int k) { return (k * ny + j) * nx + i; }

    public CorpsDeformable(int nx, int ny, int nz, float pas, CustomVector3 origine, float masseTotale,
                           float raideur, float c, float seuilRupture, CustomVector3 vitesseInitiale)
    {
        this.nx = nx; this.ny = ny; this.nz = nz;
        int n = nx * ny * nz;
        systeme = new SystemeParticules(n, "Corps déformable");
        float m = masseTotale / n;

        for (int kk = 0; kk < nz; kk++)
            for (int jj = 0; jj < ny; jj++)
                for (int ii = 0; ii < nx; ii++)
                {
                    int idx = Index(ii, jj, kk);
                    systeme.p[idx].x = new CustomVector3(origine.x + ii * pas, origine.y + jj * pas, origine.z + kk * pas);
                    systeme.p[idx].v = vitesseInitiale;
                    systeme.p[idx].m = m;
                    systeme.p[idx].invmass = 1f / m;
                    systeme.p[idx].phase = 0;
                }

        ressorts = new ForceRessort();
        ressorts.seuilRupture = seuilRupture;

        for (int kk = 0; kk < nz; kk++)
            for (int jj = 0; jj < ny; jj++)
                for (int ii = 0; ii < nx; ii++)
                {
                    int a = Index(ii, jj, kk);
                    for (int dk = -1; dk <= 1; dk++)
                        for (int dj = -1; dj <= 1; dj++)
                            for (int di = -1; di <= 1; di++)
                            {
                                if (di == 0 && dj == 0 && dk == 0) continue;
                                // moitié positive des voisins : chaque ressort n'est créé qu'une fois
                                if (dk < 0 || (dk == 0 && (dj < 0 || (dj == 0 && di < 0)))) continue;

                                int ni = ii + di, nj = jj + dj, nk = kk + dk;
                                if (ni < 0 || ni >= nx || nj < 0 || nj >= ny || nk < 0 || nk >= nz) continue;

                                int ordre = Math.Abs(di) + Math.Abs(dj) + Math.Abs(dk);
                                bool face = (ordre == 1);
                                ressorts.Ajouter(systeme, a, Index(ni, nj, nk),
                                                 face ? raideur : raideur * 0.5f, c,
                                                 face ? ForceRessort.Structurel : ForceRessort.Cisaillement, true);
                            }
                }

        systeme.forces.Add(ressorts);
    }
}