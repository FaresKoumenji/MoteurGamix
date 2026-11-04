using System;
using System.Collections.Generic;

/// <summary>
/// Sweep and Prune (Ch.4, slides 58–62) : tri par min.x, puis balayage
/// avec liste active. Les paires dont les projections sur x se recouvrent
/// sont testées par AABB complet. Complexité ≈ O(n log n + k).
/// </summary>
public class PhaseLargeSAP : IPhaseLarge
{
    struct Entree
    {
        public float minX, maxX;
        public int indice;
    }

    public void TrouverPaires(List<CustomRigidbody> corps, List<(int i, int j)> paires)
    {
        int n = corps.Count;
        if (n < 2) return;

        var entrees = new Entree[n];
        for (int i = 0; i < n; i++)
        {
            AABB b = corps[i].Volume;
            entrees[i] = new Entree { minX = b.min.x, maxX = b.max.x, indice = i };
        }

        Array.Sort(entrees, (a, b) => a.minX.CompareTo(b.minX));

        var actifs = new List<int>(32);
        for (int k = 0; k < n; k++)
        {
            float minK = entrees[k].minX;

            for (int a = actifs.Count - 1; a >= 0; a--)
            {
                int idx = actifs[a];
                if (corps[idx].Volume.max.x < minK) actifs.RemoveAt(a);
            }

            for (int a = 0; a < actifs.Count; a++)
            {
                int i = entrees[k].indice;
                int j = actifs[a];
                if (j < i) { int t = i; i = j; j = t; }

                if (corps[i].Volume.Recouvre(corps[j].Volume))
                    paires.Add((i, j));
            }
            actifs.Add(entrees[k].indice);
        }
    }
}