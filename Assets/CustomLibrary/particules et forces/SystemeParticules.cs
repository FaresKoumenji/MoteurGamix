using System;
using System.Collections.Generic;

/// <summary>
/// Système de particules (Ch.2 Partie 1, slides 30 à 33) :
///   - N particules (x, v, m), un temps t ;
///   - une LISTE d'objets forces (IForce) ;
///   - un solveur ODE (SolveurODE) qui intègre le vecteur d'état de toutes les particules.
/// Les accélérations sont a = F / m, avec F = somme des forces de la liste.
/// </summary>
public class SystemeParticules
{
    public string nom;
    public int N;
    public Particule[] p;
    public List<IForce> forces = new List<IForce>();
    public float t;

    public ModeIntegration mode = ModeIntegration.RK4;
    public int sousPas = 16;
    public float vitesseMax = 60f;

    public CustomVector3[] F, x0, v0, xt, vt, a1, a2, a3, a4, k2x, k3x;

    public SystemeParticules(int n, string nom = "Système")
    {
        this.nom = nom;
        N = n;
        p = new Particule[n];
        AllouerBuffers(n);
    }

    void AllouerBuffers(int n)
    {
        F = new CustomVector3[n];
        x0 = new CustomVector3[n];
        v0 = new CustomVector3[n];
        xt = new CustomVector3[n];
        vt = new CustomVector3[n];
        a1 = new CustomVector3[n];
        a2 = new CustomVector3[n];
        a3 = new CustomVector3[n];
        a4 = new CustomVector3[n];
        k2x = new CustomVector3[n];
        k3x = new CustomVector3[n];
    }

    public void Fixer(int i)
    {
        p[i].invmass = 0f;
        p[i].v = CustomVector3.zero;
    }

    /// <summary>Ajoute n particules à la fin. Retourne l'ancien N (= indice de la première nouvelle).</summary>
    public int AjouterParticules(int n)
    {
        if (n <= 0) return N;
        int ancienN = N;
        int nouveauN = N + n;

        Array.Resize(ref p, nouveauN);
        AllouerBuffers(nouveauN);
        N = nouveauN;
        return ancienN;
    }

    /// <summary>Supprime n particules à partir de l'indice debut.</summary>
    public void SupprimerParticules(int debut, int n)
    {
        if (n <= 0 || debut < 0 || debut + n > N) return;
        int nouveauN = N - n;
        Particule[] np = new Particule[nouveauN];
        Array.Copy(p, 0, np, 0, debut);
        Array.Copy(p, debut + n, np, debut, N - (debut + n));
        p = np;
        AllouerBuffers(nouveauN);
        N = nouveauN;
    }

    /// <summary>Initialise une particule. masse = 0 => particule fixe.</summary>
    public void InitialiserParticule(int i, CustomVector3 position, CustomVector3 vitesse, float masse)
    {
        if (i < 0 || i >= N) return;
        p[i].x = position;
        p[i].v = vitesse;
        p[i].m = masse;
        p[i].invmass = masse > 0f ? 1f / masse : 0f;
        p[i].phase = 0;
    }

    /// <summary>a(x, v) = (somme des forces) / m.</summary>
    public void Accelerations(CustomVector3[] x, CustomVector3[] v, CustomVector3[] a)
    {
        for (int i = 0; i < N; i++) F[i] = CustomVector3.zero;

        for (int k = 0; k < forces.Count; k++)
            forces[k].Appliquer(this, x, v, F);

        for (int i = 0; i < N; i++)
            a[i] = (p[i].invmass > 0f) ? F[i] * p[i].invmass : CustomVector3.zero;
    }

    /// <summary>Avance le système de dt, en sousPas sous-pas.</summary>
    public void Avancer(float dt)
    {
        int n = Math.Max(1, sousPas);
        float h = dt / n;
        for (int k = 0; k < n; k++)
            SolveurODE.Pas(this, h, mode);

        for (int k = 0; k < forces.Count; k++)
            forces[k].ApresPas(this);
    }
}