using System;
using System.Collections.Generic;

/// <summary>
/// Phase large par grille uniforme (Ch.4, slides 51–55) :
/// Dictionary&lt;Vector3Int, List&lt;int&gt;&gt;, 27 cellules voisines,
/// on ne teste que j &gt; i (une seule fois par paire).
/// La cellule fait environ 2 fois la taille du plus gros objet.
/// Les listes sont vidées mais conservées pour éviter les allocations.
/// </summary>
public class PhaseLargeGrille : IPhaseLarge
{
    public float tailleCellule = 1f;     // s : côté de cellule
    public int nbCorps = 0;

    private readonly Dictionary<(int, int, int), List<int>> grille =
        new Dictionary<(int, int, int), List<int>>();
    private readonly List<List<int>> liste = new List<List<int>>(64);

    (int, int, int) Cellule(CustomVector3 p, float s)
    {
        return ((int)Math.Floor(p.x / s),
                (int)Math.Floor(p.y / s),
                (int)Math.Floor(p.z / s));
    }

    public void TrouverPaires(List<CustomRigidbody> corps, List<(int i, int j)> paires)
    {
        nbCorps = corps.Count;
        if (corps.Count < 2) return;

        // Taille de cellule ≈ 2 × rayon le plus grand
        float maxDim = 0f;
        for (int i = 0; i < corps.Count; i++)
        {
            CustomVector3 e = corps[i].Volume.etendue;
            float d = Math.Max(e.x, Math.Max(e.y, e.z)) * 2f;
            if (d > maxDim) maxDim = d;
        }
        tailleCellule = Math.Max(0.05f, maxDim);

        // Vide les listes existantes (sans les désallouer)
        foreach (var kv in grille) kv.Value.Clear();
        liste.Clear();

        // Insertion
        for (int i = 0; i < corps.Count; i++)
        {
            var c = Cellule(corps[i].Volume.centre, tailleCellule);
            if (!grille.TryGetValue(c, out var l))
            {
                l = new List<int>(8);
                grille[c] = l;
                liste.Add(l);
            }
            l.Add(i);
        }

        // Interrogation : 27 voisins
        for (int i = 0; i < corps.Count; i++)
        {
            var c = Cellule(corps[i].Volume.centre, tailleCellule);
            for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
            for (int dz = -1; dz <= 1; dz++)
            {
                var k = (c.Item1 + dx, c.Item2 + dy, c.Item3 + dz);
                if (!grille.TryGetValue(k, out var l)) continue;
                for (int q = 0; q < l.Count; q++)
                {
                    int j = l[q];
                    if (j <= i) continue;
                    if (corps[i].Volume.Recouvre(corps[j].Volume))
                        paires.Add((i, j));
                }
            }
        }
    }
}