using System;
using System.Collections.Generic;

/// <summary>Un ressort amorti entre les particules i et j (Ch.2 Partie 1, slides 19 et 29).</summary>
[Serializable]
public struct Ressort
{
    public int i, j;
    public float L0;        // longueur au repos
    public float k;         // raideur
    public float c;         // coefficient d'amortissement
    public int type;        // 0 structurel, 1 cisaillement, 2 flexion
    public bool rompable;
    public bool actif;
    public bool unilateral; // true = résiste seulement à l'étirement (corde / tether)
}

/// <summary>
/// Réseau de ressorts : loi de Hooke F = -k (|d| - L0) u, plus amortissement axial -c (vrel . u) u.
/// Si seuilRupture > 0, un ressort rompable casse quand sa longueur dépasse L0 (1 + seuilRupture)
/// (déchirure du tissu, ballon qui éclate). Les types structurel / cisaillement / flexion évitent
/// l'exemple négatif "NO SHEAR AND BEND SPRINGS" montré à la slide 29.
/// Si unilateral = true, le ressort ne pousse pas quand il est comprimé (longueur < L0) :
/// c'est le comportement des "tethers" de FLEX §8 (corde lâche qui ne se replie pas sur elle-même).
/// </summary>
public class ForceRessort : IForce
{
    public const int Structurel = 0, Cisaillement = 1, Flexion = 2;

    public List<Ressort> ressorts = new List<Ressort>();
    public float seuilRupture = 0f;     // 0 = incassable
    public int nbRompus = 0;

    public void Ajouter(SystemeParticules s, int i, int j, float k, float c, int type, bool rompable,
                        bool unilateral = false)
    {
        Ressort r = new Ressort();
        r.i = i; r.j = j; r.k = k; r.c = c; r.type = type;
        r.rompable = rompable; r.actif = true; r.unilateral = unilateral;
        r.L0 = CustomVector3.Distance(s.p[i].x, s.p[j].x);
        ressorts.Add(r);
    }

    public void Appliquer(SystemeParticules s, CustomVector3[] x, CustomVector3[] v, CustomVector3[] f)
    {
        for (int q = 0; q < ressorts.Count; q++)
        {
            Ressort r = ressorts[q];
            if (!r.actif) continue;

            CustomVector3 d = x[r.i] - x[r.j];
            float len = d.magnitude;
            if (len < 1e-6f) continue;

            CustomVector3 u = d / len;
            float vrel = CustomVector3.Dot(v[r.i] - v[r.j], u);

            float intensite = -(r.k * (len - r.L0) + r.c * vrel);

            // Tether unilatéral : ne résiste pas à la compression
            if (r.unilateral && len < r.L0 && intensite < 0f)
                intensite = 0f;

            CustomVector3 fi = u * intensite;
            f[r.i] += fi;
            f[r.j] -= fi;
        }
    }

    public void ApresPas(SystemeParticules s)
    {
        if (seuilRupture <= 0f) return;

        for (int q = 0; q < ressorts.Count; q++)
        {
            Ressort r = ressorts[q];
            if (!r.actif || !r.rompable) continue;

            float len = CustomVector3.Distance(s.p[r.i].x, s.p[r.j].x);
            if (len > r.L0 * (1f + seuilRupture))
            {
                r.actif = false;
                ressorts[q] = r;
                nbRompus++;
            }
        }
    }
}