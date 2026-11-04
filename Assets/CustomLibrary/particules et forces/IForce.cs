/// <summary>
/// Objet "force" (Ch.2 Partie 1, slide 33) : on peut ajouter, modifier ou étendre la liste des forces
/// SANS changer la configuration du système de particules.
/// Appliquer() ajoute la contribution de la force dans f[i] pour l'état (x, v) donné.
/// L'état est passé en paramètre car RK4 évalue les forces sur des états intermédiaires.
/// </summary>
public interface IForce
{
    void Appliquer(SystemeParticules s, CustomVector3[] x, CustomVector3[] v, CustomVector3[] f);

    /// <summary>Appelée une fois par pas, après l'intégration (ex. rupture d'un ressort).</summary>
    void ApresPas(SystemeParticules s);
}
