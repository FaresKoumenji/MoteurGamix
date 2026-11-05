using System;

public class Fluide
{
    public SystemeParticules systeme;
    public ContrainteDensite contrainteDensite;
    public CouplageFluideSolide couplage;
    public float h = 0.2f;
    public float rho0 = 1000f;

    /// <summary>
    /// N = nombre de particules VISÉ. Le réseau est cubique (espacement identique en x, y, z),
    /// donc le nombre réel est nx*ny*nz (voir systeme.N).
    /// </summary>
    public Fluide(int N, CustomVector3 origine, CustomVector3 taille, float h = 0.2f, float rho0 = 1000f)
    {
        this.rho0 = rho0;

        // Cas N = 0 : pas d'eau, on désactive tout
        if (N <= 0)
        {
            systeme = new SystemeParticules(1, "Fluide");
            systeme.mode = ModeIntegration.EulerSemiImplicite;
            systeme.sousPas = 8;
            systeme.vitesseMax = 3f;
            systeme.p[0].x = origine;
            systeme.p[0].m = 0f;
            systeme.p[0].invmass = 0f;
            systeme.p[0].v = CustomVector3.zero;

            this.h = Math.Max(h, 1e-3f);
            contrainteDensite = new ContrainteDensite(this.h, rho0);
            contrainteDensite.lambdaRelaxation = 1f;
            couplage = new CouplageFluideSolide(this.h, rho0);
            return;
        }

        // Réseau cubique : espacement visé s0 = (V/N)^(1/3), puis nombre de particules par axe
        float volume = taille.x * taille.y * taille.z;
        float s0 = (float)Math.Pow(volume / N, 1.0 / 3.0);
        if (s0 < 1e-4f) s0 = 1e-4f;

        int nx = Math.Max(1, (int)Math.Round(taille.x / s0));
        int ny = Math.Max(1, (int)Math.Round(taille.y / s0));
        int nz = Math.Max(1, (int)Math.Round(taille.z / s0));
        int n = nx * ny * nz;

        systeme = new SystemeParticules(n, "Fluide");
        systeme.mode = ModeIntegration.EulerSemiImplicite;
        systeme.sousPas = 8;
        systeme.vitesseMax = 3f;

        float dx = taille.x / nx;
        float dy = taille.y / ny;
        float dz = taille.z / nz;

        // espacement effectif = racine cubique du volume d'une cellule du réseau
        float espacement = (float)Math.Pow(dx * dy * dz, 1.0 / 3.0);

        float m = rho0 * espacement * espacement * espacement;
        this.h = Math.Max(h, espacement * 2.2f);   // h ~ 2.2 s : somme du noyau ~ 1 sur le réseau
        if (this.h < 1e-3f) this.h = 1e-3f;

        int idx = 0;
        for (int k = 0; k < nz; k++)
            for (int j = 0; j < ny; j++)
                for (int i = 0; i < nx; i++)
                {
                    float px = origine.x + (i + 0.5f) * dx;
                    float py = origine.y + (j + 0.5f) * dy;
                    float pz = origine.z + (k + 0.5f) * dz;
                    systeme.p[idx].x = new CustomVector3(px, py, pz);
                    systeme.p[idx].v = CustomVector3.zero;
                    systeme.p[idx].m = m;
                    systeme.p[idx].invmass = 1f / m;
                    systeme.p[idx].phase = 1;
                    idx++;
                }

        systeme.forces.Add(new ForceGravite(new CustomVector3(0f, -9.81f, 0f)));
        systeme.forces.Add(new ForceTrainee(0.1f));

        contrainteDensite = new ContrainteDensite(this.h, rho0);
        contrainteDensite.lambdaRelaxation = 1f;
        contrainteDensite.rayonContact = 0.95f * espacement;   // distance min entre particules (FLEX Eq. 22)
        couplage = new CouplageFluideSolide(this.h, rho0);
        couplage.espacement = espacement;
    }

    // ------------------------------------------------------------------------------------------
    // Remplissage d'un récipient cubique (utilisé par les scénarios, pas de formule à refaire)
    // ------------------------------------------------------------------------------------------

    /// <summary>Volume d'eau d'un récipient cubique de côté `cote` rempli à `fraction` de sa hauteur.</summary>
    public static float VolumeRemplissage(float cote, float fraction)
    {
        return cote * cote * (cote * fraction);
    }

    /// <summary>Hauteur y de la surface au repos (le fond est à y = -cote/2).</summary>
    public static float NiveauSurface(float cote, float fraction)
    {
        return -cote * 0.5f + cote * fraction;
    }

    /// <summary>
    /// Nombre de particules à demander au constructeur pour un espacement initial voulu :
    /// une particule par cellule d'arête `espacement` (le constructeur impose h >= 2.2 x espacement).
    /// </summary>
    public static int NbParticulesPourEspacement(float cote, float fraction, float espacement)
    {
        return (int)Math.Ceiling(VolumeRemplissage(cote, fraction) / (espacement * espacement * espacement));
    }

    public void Avancer(float dt)
    {
        int n = Math.Max(1, systeme.sousPas);
        float hpas = dt / n;
        for (int k = 0; k < n; k++)
        {
            SolveurODE.Pas(systeme, hpas, systeme.mode);
            contrainteDensite.Projeter(systeme);
            // Le couplage (qui repousse les particules hors des solides) s'applique AVANT les murs :
            // sinon il peut pousser une particule à travers un mur, et rien ne la ramène.
            if (couplage != null) couplage.Appliquer(systeme, hpas);
            for (int c = 0; c < systeme.contraintes.Count; c++)
                systeme.contraintes[c].Projeter(systeme);
        }
        for (int k = 0; k < systeme.forces.Count; k++)
            systeme.forces[k].ApresPas(systeme);
    }
}