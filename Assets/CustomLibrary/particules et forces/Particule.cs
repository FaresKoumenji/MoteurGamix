using System;

/// <summary>
/// Particule (article FLEX, section 3) : position, vitesse, inverse de la masse et phase.
/// invmass = 0 : particule fixe (clouée). phase : groupe de particules (tissu, liquide A, liquide B...).
/// </summary>
[Serializable]
public struct Particule
{
    public CustomVector3 x;   // position
    public CustomVector3 v;   // vitesse
    public float m;           // masse
    public float invmass;     // 1/m  (0 = particule fixe)
    public int phase;         // identifiant de groupe
}
