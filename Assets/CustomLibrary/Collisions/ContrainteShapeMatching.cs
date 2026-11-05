using System;
using System.Collections.Generic;

/// <summary>
/// Shape matching (FLEX §5, eq. 15-16) :
///   Δx_i = (Q·r_i + c) − x_i*
/// où Q est la rotation obtenue par décomposition polaire de la matrice de covariance
///   A = Σ (x_i* − c) · r_iᵀ
/// Chaque groupe de particules (identifié par sa "phase") est maintenu rigide.
/// Un cube = 8 particules (sommets) + une contrainte ShapeMatching sur ces 8 particules.
/// </summary>
public class ContrainteShapeMatching : IContrainte
{
    public float raideur = 0.9f;   // 1 = totalement rigide, 0 = libre

    // Groupes : liste des indices de particules par groupe (ex. un groupe = un cube)
    public List<List<int>> groupes = new List<List<int>>();

    // Positions de repos (calculées une fois au démarrage)
    private List<CustomVector3[]> repos = new List<CustomVector3[]>();
    private bool initialise = false;

    public void AjouterGroupe(List<int> indices)
    {
        groupes.Add(indices);
        initialise = false;   // forcer recalcul des repos
    }

    void InitialiserRepos(SystemeParticules s)
    {
        repos.Clear();
        for (int g = 0; g < groupes.Count; g++)
        {
            List<int> groupe = groupes[g];
            int n = groupe.Count;
            CustomVector3 c = CustomVector3.zero;
            for (int i = 0; i < n; i++) c += s.p[groupe[i]].x;
            c = c / n;

            CustomVector3[] r = new CustomVector3[n];
            for (int i = 0; i < n; i++) r[i] = s.p[groupe[i]].x - c;
            repos.Add(r);
        }
        initialise = true;
    }

    public void Projeter(SystemeParticules s)
    {
        if (groupes.Count == 0) return;
        if (!initialise) InitialiserRepos(s);

        for (int g = 0; g < groupes.Count; g++)
        {
            List<int> groupe = groupes[g];
            int n = groupe.Count;
            if (n < 4) continue;

            // 1) Centre de masse actuel
            CustomVector3 c = CustomVector3.zero;
            for (int i = 0; i < n; i++) c += s.p[groupe[i]].x;
            c = c / n;

            // 2) Matrice de covariance A = Σ (x_i - c) r_iᵀ
            // Représentation : 3 colonnes (aX, aY, aZ) — chaque colonne = Σ (x-c) * r_i.component
            CustomVector3 aX = CustomVector3.zero;
            CustomVector3 aY = CustomVector3.zero;
            CustomVector3 aZ = CustomVector3.zero;
            CustomVector3[] r = repos[g];
            for (int i = 0; i < n; i++)
            {
                CustomVector3 d = s.p[groupe[i]].x - c;
                aX += d * r[i].x;
                aY += d * r[i].y;
                aZ += d * r[i].z;
            }

            // 3) Extraction de la rotation Q (itération de Müller 2016, "A Robust Method to Extract
            //    the Rotational Part of Deformations"). On approxime par Gram-Schmidt sur les colonnes
            //    de A, ce qui est suffisant pour un cube.
            CustomVector3 qx = aX.normalized;
            CustomVector3 qy = (aY - qx * CustomVector3.Dot(qx, aY)).normalized;
            CustomVector3 qz = CustomVector3.Cross(qx, qy);

            // 4) Appliquer Δx_i = (Q r_i + c) - x_i
            for (int i = 0; i < n; i++)
            {
                int idx = groupe[i];
                if (s.p[idx].invmass <= 0f) continue;

                // Q * r_i = qx * r_i.x + qy * r_i.y + qz * r_i.z
                CustomVector3 qr = qx * r[i].x + qy * r[i].y + qz * r[i].z;
                CustomVector3 cible = qr + c;

                CustomVector3 delta = (cible - s.p[idx].x) * raideur;
                s.p[idx].x = s.p[idx].x + delta;
            }
        }
    }
}