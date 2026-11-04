/// <summary>Poids : F = m.g appliqué à chaque particule (Ch.2 Partie 1, slides 6 et 19).</summary>
public class ForceGravite : IForce
{
    public CustomVector3 g;

    public ForceGravite(CustomVector3 g) { this.g = g; }

    public void Appliquer(SystemeParticules s, CustomVector3[] x, CustomVector3[] v, CustomVector3[] f)
    {
        for (int i = 0; i < s.N; i++)
            f[i] += g * s.p[i].m;
    }

    public void ApresPas(SystemeParticules s) { }
}
