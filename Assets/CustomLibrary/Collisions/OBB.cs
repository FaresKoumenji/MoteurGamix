using System;

/// <summary>
/// Oriented Bounding Box (Ch.4, slides 42–46) : centre c, axes u (CustomMatrix3 colonnes),
/// demi-tailles e. Nécessite 15 valeurs (3 + 9 + 3).
/// </summary>
[Serializable]
public struct OBB
{
    public CustomVector3 centre;
    public CustomMatrix3 axes;         // colonnes = axes locaux exprimés dans le monde
    public CustomVector3 demiTailles;

    public OBB(CustomVector3 centre, CustomMatrix3 axes, CustomVector3 demiTailles)
    {
        this.centre = centre;
        this.axes = axes;
        this.demiTailles = demiTailles;
    }

    /// <summary>Rayon de projection de l'OBB sur l'axe L (slide 43).</summary>
    public float Rayon(CustomVector3 L)
    {
        return demiTailles.x * Math.Abs(CustomVector3.Dot(axes.X, L))
             + demiTailles.y * Math.Abs(CustomVector3.Dot(axes.Y, L))
             + demiTailles.z * Math.Abs(CustomVector3.Dot(axes.Z, L));
    }
}