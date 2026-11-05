using System;

/// <summary>
/// Contrainte qui détecte le contact d'un corps rigide (boule) avec les particules
/// d'un système, et appelle un callback de libération. Utilisée pour les scénarios
/// "objet rigide qui se fragmente au choc" : les particules sont gelées (invmass = 0)
/// jusqu'à l'impact, puis libérées d'un coup.
///
/// Ce n'est pas un scénario : c'est une contrainte générique réutilisable.
/// </summary>
public class LiberationImpact : IContrainte
{
    public CustomRigidbody declencheur;
    public float rayonDeclencheur;
    public float margeContact = 0.05f;
    public Action<CustomVector3> auContact;   // reçoit la vitesse du déclencheur

    private bool dejaDeclenche = false;

    public LiberationImpact(CustomRigidbody declencheur, float rayonDeclencheur, Action<CustomVector3> auContact)
    {
        this.declencheur = declencheur;
        this.rayonDeclencheur = rayonDeclencheur;
        this.auContact = auContact;
    }

    public void Projeter(SystemeParticules s)
    {
        if (dejaDeclenche || declencheur == null) return;
        if (declencheur.invMass <= 0f) return;

        CustomVector3 centre = declencheur.CentreMonde;
        float seuil = rayonDeclencheur + margeContact;
        float seuil2 = seuil * seuil;

        for (int i = 0; i < s.N; i++)
        {
            if (s.p[i].invmass > 0f) continue;
            if ((s.p[i].x - centre).sqrMagnitude < seuil2)
            {
                dejaDeclenche = true;
                auContact?.Invoke(declencheur.velocity);
                return;
            }
        }
    }
}