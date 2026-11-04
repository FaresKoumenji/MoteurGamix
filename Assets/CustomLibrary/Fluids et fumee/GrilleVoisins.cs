using System;
using System.Collections.Generic;

/// <summary>
/// Grille de voisinage par hachage spatial (FLEX §9, "efficient parallel hash-grid").
/// Les particules sont rangées dans des cellules de côté h (rayon de lissage).
/// On ne teste que les 27 cellules voisines pour trouver les paires.
/// </summary>
public class GrilleVoisins
{
    public float h = 0.2f;
    public int capacite = 0;

    readonly Dictionary<(int, int, int), List<int>> cellules =
        new Dictionary<(int, int, int), List<int>>();

    readonly List<List<int>> listes = new List<List<int>>(128);

    (int, int, int) Cellule(CustomVector3 p)
    {
        return ((int)Math.Floor(p.x / h),
                (int)Math.Floor(p.y / h),
                (int)Math.Floor(p.z / h));
    }

    /// <summary>Construit la grille à partir des positions x[0..N).</summary>
    public void Construire(CustomVector3[] x, int N)
    {
        foreach (var kv in cellules) kv.Value.Clear();
        listes.Clear();

        for (int i = 0; i < N; i++)
        {
            var c = Cellule(x[i]);
            if (!cellules.TryGetValue(c, out var l))
            {
                l = new List<int>(16);
                cellules[c] = l;
                listes.Add(l);
            }
            l.Add(i);
        }
        capacite = N;
    }

    /// <summary>
    /// Parcourt les voisins de i dans un rayon h (les 27 cellules voisines).
    /// L'utilisateur teste ensuite |x_i - x_j| lui-même (distance exacte).
    /// </summary>
    public void PourChaqueVoisin(CustomVector3[] x, int i, Action<int> action)
    {
        var ci = Cellule(x[i]);
        for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
                for (int dz = -1; dz <= 1; dz++)
                {
                    var k = (ci.Item1 + dx, ci.Item2 + dy, ci.Item3 + dz);
                    if (!cellules.TryGetValue(k, out var l)) continue;
                    for (int q = 0; q < l.Count; q++)
                    {
                        int j = l[q];
                        if (j == i) continue;
                        action(j);
                    }
                }
    }

    /// <summary>Même parcours, mais remplit une liste réutilisable (sans lambda, sans allocation).</summary>
    public void Voisins(CustomVector3[] x, int i, List<int> sortie)
    {
        sortie.Clear();
        var ci = Cellule(x[i]);
        for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
                for (int dz = -1; dz <= 1; dz++)
                {
                    var k = (ci.Item1 + dx, ci.Item2 + dy, ci.Item3 + dz);
                    if (!cellules.TryGetValue(k, out var l)) continue;
                    for (int q = 0; q < l.Count; q++)
                    {
                        int j = l[q];
                        if (j == i) continue;
                        sortie.Add(j);
                    }
                }
    }
}