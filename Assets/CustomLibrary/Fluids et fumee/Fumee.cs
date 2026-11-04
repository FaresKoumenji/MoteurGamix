using System;

public class Fumee
{
    public SystemeParticules systeme;
    public float dureeVie = 6f;
    public float flottabilite = 0.5f;   // positif : monte

    public Fumee(int N, CustomVector3 origine, CustomVector3 rayonEmission)
    {
        systeme = new SystemeParticules(N, "Fumée");
        systeme.mode = ModeIntegration.EulerSemiImplicite;
        systeme.sousPas = 2;
        systeme.vitesseMax = 10f;

        var rnd = new Random(42);
        for (int i = 0; i < N; i++)
        {
            float rx = (float)(rnd.NextDouble() * 2 - 1);
            float ry = (float)(rnd.NextDouble() * 2 - 1);
            float rz = (float)(rnd.NextDouble() * 2 - 1);
            systeme.p[i].x = origine + new CustomVector3(rx * rayonEmission.x, ry * rayonEmission.y, rz * rayonEmission.z);
            systeme.p[i].v = new CustomVector3(0f, 1f, 0f);
            systeme.p[i].m = 1f;
            systeme.p[i].invmass = 1f;
            systeme.p[i].phase = 2;
        }

        // Gravité inversée pour la flottabilité : g effectif = +4.9 m/s² (montée)
        systeme.forces.Add(new ForceGravite(new CustomVector3(0f, 9.81f * flottabilite, 0f)));
        systeme.forces.Add(new ForceTrainee(0.5f));
    }

    public void Avancer(float dt) => systeme.Avancer(dt);
}