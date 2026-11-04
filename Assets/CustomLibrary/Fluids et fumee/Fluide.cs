using System;

public class Fluide
{
    public SystemeParticules systeme;
    public ContrainteDensite contrainteDensite;
    public CouplageFluideSolide couplage;
    public float h = 0.2f;
    public float rho0 = 1000f;

    public Fluide(int N, CustomVector3 origine, CustomVector3 taille, float h = 0.2f, float rho0 = 1000f)
    {
        this.rho0 = rho0;

        int n = Math.Max(1, N);
        systeme = new SystemeParticules(n, "Fluide");
        systeme.mode = ModeIntegration.EulerSemiImplicite;
        systeme.sousPas = 8;
        systeme.vitesseMax = 3f;

        // Cas N = 0 : pas d'eau, on désactive tout
        if (N <= 0)
        {
            for (int i = 0; i < n; i++)
            {
                systeme.p[i].x = origine;
                systeme.p[i].m = 0f;
                systeme.p[i].invmass = 0f;
                systeme.p[i].v = CustomVector3.zero;
            }
            this.h = Math.Max(h, 1e-3f);
            contrainteDensite = new ContrainteDensite(this.h, rho0);
            contrainteDensite.lambdaRelaxation = 1f;
            couplage = new CouplageFluideSolide(this.h, rho0);
            return;
        }

        int cote = (int)Math.Ceiling(Math.Pow(N, 1.0 / 3.0));
        if (cote < 1) cote = 1;

        float dx = taille.x / cote;
        float dy = taille.y / cote;
        float dz = taille.z / cote;

        float espacement = (dx + dy + dz) / 3f;
        if (espacement < 1e-4f) espacement = 1e-4f;

        float m = rho0 * espacement * espacement * espacement;
        this.h = Math.Max(h, espacement * 2.2f);
        if (this.h < 1e-3f) this.h = 1e-3f;

        int idx = 0;
        for (int k = 0; k < cote && idx < n; k++)
            for (int j = 0; j < cote && idx < n; j++)
                for (int i = 0; i < cote && idx < n; i++)
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
        for (; idx < n; idx++)
        {
            systeme.p[idx].x = origine;
            systeme.p[idx].m = 0f;
            systeme.p[idx].invmass = 0f;
        }

        systeme.forces.Add(new ForceGravite(new CustomVector3(0f, -9.81f, 0f)));
        systeme.forces.Add(new ForceTrainee(0.1f));

        contrainteDensite = new ContrainteDensite(this.h, rho0);
        contrainteDensite.lambdaRelaxation = 1f;
        couplage = new CouplageFluideSolide(this.h, rho0);
    }

    public void Avancer(float dt)
    {
        int n = Math.Max(1, systeme.sousPas);
        float hpas = dt / n;
        for (int k = 0; k < n; k++)
        {
            SolveurODE.Pas(systeme, hpas, systeme.mode);
            contrainteDensite.Projeter(systeme);
            for (int c = 0; c < systeme.contraintes.Count; c++)
                systeme.contraintes[c].Projeter(systeme);
            if (couplage != null) couplage.Appliquer(systeme, hpas);
        }
        for (int k = 0; k < systeme.forces.Count; k++)
            systeme.forces[k].ApresPas(systeme);
    }
}