using System;

/// <summary>
/// Sphère englobante (Ch.4, slides 22–23, 30–31) : centre + rayon.
/// Utilisée pour le billard (slide 32) et la balle rebondissante (slide 14).
/// </summary>
[Serializable]
public struct SphereEnglobante
{
    public CustomVector3 centre;
    public float rayon;

    public SphereEnglobante(CustomVector3 centre, float rayon)
    {
        this.centre = centre;
        this.rayon = rayon;
    }
}