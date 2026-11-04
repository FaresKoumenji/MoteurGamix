using System;

/// <summary>
/// Corde : chaîne de particules reliées par des ressorts (article FLEX, section 8 : contraintes
/// d'étirement et de flexion). Les ressorts d'étirement sont UNILATÉRAUX : une corde résiste à
/// l'allongement mais pas à la compression (elle devient lâche, comme les "tethers" de l'article).
/// La corde relie deux particules DÉJÀ existantes du système (ex. le bord d'un tissu et une charge).
/// </summary>
public static class Corde
{
    /// <summary>Retourne les indices de la corde : a, particules intermédiaires, b.</summary>
    public static int[] Relier(SystemeParticules s, ForceRessort ressorts, int a, int b,
                               int nSegments, float masseTotale, float k, float c, bool rompable = false)
    {
        nSegments = Math.Max(1, nSegments);
        int nInt = nSegments - 1;
        int debut = (nInt > 0) ? s.AjouterParticules(nInt) : s.N;

        int[] idx = new int[nSegments + 1];
        idx[0] = a;
        idx[nSegments] = b;

        CustomVector3 pa = s.p[a].x;
        CustomVector3 pb = s.p[b].x;
        float m = masseTotale / Math.Max(1, nInt);

        for (int q = 1; q < nSegments; q++)
        {
            int i = debut + q - 1;
            float t = q / (float)nSegments;
            s.p[i].x = pa + (pb - pa) * t;
            s.p[i].v = CustomVector3.zero;
            s.p[i].m = m;
            s.p[i].invmass = 1f / m;
            s.p[i].phase = 1;
            idx[q] = i;
        }

        for (int q = 0; q < nSegments; q++)
            ressorts.Ajouter(s, idx[q], idx[q + 1], k, c, ForceRessort.Structurel, rompable, true);

        for (int q = 0; q + 2 <= nSegments; q++)
            ressorts.Ajouter(s, idx[q], idx[q + 2], k * 0.2f, c, ForceRessort.Flexion, false);

        return idx;
    }
}
