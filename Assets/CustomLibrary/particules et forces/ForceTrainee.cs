/// <summary>Frottement fluide : F = -lambda.v (Ch.2 Partie 1, slide 6, et force de frottement du Rigidbody).</summary>
public class ForceTrainee : IForce
{
    public float lambda;

    public ForceTrainee(float lambda) { this.lambda = lambda; }

    public void Appliquer(SystemeParticules s, CustomVector3[] x, CustomVector3[] v, CustomVector3[] f)
    {
        for (int i = 0; i < s.N; i++)
            f[i] += v[i] * (-lambda);
    }

    public void ApresPas(SystemeParticules s) { }
}
