/// <summary>
/// Contrainte de position (article FLEX, section 4 : position-based dynamics).
/// Projetée après CHAQUE sous-pas d'intégration pour corriger les positions et les vitesses
/// (ex. interdire la pénétration du sol), contrairement aux forces IForce qui entrent dans a = F/m.
/// </summary>
public interface IContrainte
{
    void Projeter(SystemeParticules s);
}
