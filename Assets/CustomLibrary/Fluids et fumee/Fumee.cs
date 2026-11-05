using System;

/// <summary>
/// Fumée : un SystemeParticules à part (phase 2) avec flottabilité (gravité inversée) et traînée.
/// Les particules sont recyclées : quand une particule dépasse sa durée de vie, elle réapparaît à la
/// source, ce qui donne une émission continue avec un nombre fixe de particules.
/// Appeler Recycler(dt) une fois par image (le système lui-même est avancé par le PhysicsWorld).
/// </summary>
public class Fumee
{
    public SystemeParticules systeme;
    public float dureeVie = 6f;
    public float flottabilite = 0.5f;   // positif : monte
    public float trainee = 1.5f;        // plus grand = montée plus lente

    private CustomVector3 origine;
    private CustomVector3 rayonEmission;
    private float[] age;
    private float[] vie;
    private Random rnd = new Random(42);

    public Fumee(int N, CustomVector3 origine, CustomVector3 rayonEmission,
                 float dureeVie = 6f, float flottabilite = 0.5f, float trainee = 1.5f)
    {
        this.origine = origine;
        this.rayonEmission = rayonEmission;
        this.dureeVie = dureeVie;
        this.flottabilite = flottabilite;
        this.trainee = trainee;

        systeme = new SystemeParticules(N, "Fumée");
        systeme.mode = ModeIntegration.EulerSemiImplicite;
        systeme.sousPas = 2;
        systeme.vitesseMax = 10f;

        age = new float[N];
        vie = new float[N];

        for (int i = 0; i < N; i++)
        {
            systeme.p[i].m = 1f;
            systeme.p[i].invmass = 1f;
            systeme.p[i].phase = 2;
            Reapparaitre(i);
            // Âges de départ répartis : l'émission est continue dès le début
            age[i] = (float)rnd.NextDouble() * vie[i];
            systeme.p[i].x = systeme.p[i].x + new CustomVector3(0f, age[i] * 1.5f, 0f);
        }

        // Gravité inversée pour la flottabilité : g effectif = +4.9 m/s² (montée)
        systeme.forces.Add(new ForceGravite(new CustomVector3(0f, 9.81f * flottabilite, 0f)));
        systeme.forces.Add(new ForceTrainee(trainee));
    }

    void Reapparaitre(int i)
    {
        float rx = (float)(rnd.NextDouble() * 2 - 1);
        float ry = (float)(rnd.NextDouble() * 2 - 1);
        float rz = (float)(rnd.NextDouble() * 2 - 1);
        systeme.p[i].x = origine + new CustomVector3(rx * rayonEmission.x, ry * rayonEmission.y, rz * rayonEmission.z);
        systeme.p[i].v = new CustomVector3(0f, 1f, 0f);
        age[i] = 0f;
        vie[i] = dureeVie * (0.8f + 0.4f * (float)rnd.NextDouble());
    }

    /// <summary>Fait vieillir les particules et réémet celles qui ont dépassé leur durée de vie.</summary>
    public void Recycler(float dt)
    {
        for (int i = 0; i < systeme.N; i++)
        {
            age[i] += dt;
            if (age[i] >= vie[i]) Reapparaitre(i);
        }
    }

    public void Avancer(float dt) => systeme.Avancer(dt);
}