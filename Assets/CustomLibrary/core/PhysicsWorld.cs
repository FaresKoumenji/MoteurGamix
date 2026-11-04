using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Monde physique : possède la liste des systèmes de particules et des corps rigides,
/// et exécute UN pas ordonné (Ch.4, slide 72) :
///   Integrer → MettreAJourVolumes → TrouverPaires → PhaseEtroite.Test → ResoudreContacts.
/// Seul Time.fixedDeltaTime est lu : le Transform n'est jamais utilisé pour la physique.
/// </summary>
public class PhysicsWorld : MonoBehaviour
{
    public enum TypePhaseLarge { ForceBrute, SAP, Grille }

    static PhysicsWorld instance;

    public static PhysicsWorld Obtenir()
    {
        if (instance == null)
        {
            GameObject go = new GameObject("PhysicsWorld");
            instance = go.AddComponent<PhysicsWorld>();
        }
        return instance;
    }

    [Header("Systèmes de particules")]
    public List<SystemeParticules> systemes = new List<SystemeParticules>();

    [Header("Corps rigides")]
    public List<CustomRigidbody> corps = new List<CustomRigidbody>();

    [Header("Phase large")]
    public TypePhaseLarge typePhaseLarge = TypePhaseLarge.Grille;
    public float tailleCellule = 1f;

    [Header("Réponse aux collisions")]
    public float restitution = 0.3f;
    public float frottement  = 0.4f;

    [Header("Affichage")]
    public bool afficherStats = true;

    // Buffers réutilisés (pas d'allocation à chaque frame)
    readonly List<(int i, int j)> paires = new List<(int, int)>(128);
    readonly List<Contact> contacts = new List<Contact>(128);

    IPhaseLarge phaseLarge;
    TypePhaseLarge typePhaseLargePrecedent;

    System.Diagnostics.Stopwatch chrono = new System.Diagnostics.Stopwatch();
    double msPas;
    float fps = 60f;

    void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
    }

    public void EnregistrerSysteme(SystemeParticules s)
    {
        if (!systemes.Contains(s)) systemes.Add(s);
    }

    public void EnregistrerCorps(CustomRigidbody rb)
    {
        if (!corps.Contains(rb)) corps.Add(rb);
    }

    void FixedUpdate()
    {
        Step(Time.fixedDeltaTime);
    }

    /// <summary>Un pas de simulation (Ch.4, slide 72).</summary>
    public void Step(float dt)
    {
        chrono.Reset(); chrono.Start();

        // 1) Intégration
        for (int k = 0; k < systemes.Count; k++) systemes[k].Avancer(dt);
        for (int k = 0; k < corps.Count;   k++) corps[k].Integrer(dt);

        // 2) Mise à jour des volumes englobants
        for (int k = 0; k < corps.Count; k++) corps[k].MettreAJourVolume();

        // 3) Phase large
        paires.Clear();
        ChoisirPhaseLarge();
        phaseLarge.TrouverPaires(corps, paires);

        // 4) Phase étroite
        contacts.Clear();
        for (int k = 0; k < paires.Count; k++)
        {
            int i = paires[k].i, j = paires[k].j;
            if (PhaseEtroite.Test(corps[i], corps[j], out Contact c))
            {
                c.indiceA = i;
                c.indiceB = j;
                contacts.Add(c);
            }
        }

        // 5) Réponse aux collisions
        ReponseCollision.ResoudreContacts(contacts, corps, dt, restitution, frottement);

        // 6) Synchronisation visuelle (Gizmos)
        for (int k = 0; k < corps.Count; k++) corps[k].SynchroVisuelle();

        chrono.Stop();
        msPas = chrono.Elapsed.TotalMilliseconds;
    }

    void ChoisirPhaseLarge()
    {
        if (typePhaseLarge != typePhaseLargePrecedent || phaseLarge == null)
        {
            switch (typePhaseLarge)
            {
                case TypePhaseLarge.ForceBrute: phaseLarge = new PhaseLargeForceBrute(); break;
                case TypePhaseLarge.SAP:        phaseLarge = new PhaseLargeSAP();        break;
                case TypePhaseLarge.Grille:     phaseLarge = new PhaseLargeGrille();     break;
            }
            typePhaseLargePrecedent = typePhaseLarge;
        }
    }

    void Update()
    {
        fps = Mathf.Lerp(fps, 1f / Mathf.Max(1e-5f, Time.unscaledDeltaTime), 0.05f);
    }

    void OnGUI()
    {
        if (!afficherStats) return;

        int totalParticules = 0;
        for (int k = 0; k < systemes.Count; k++) totalParticules += systemes[k].N;

        GUI.Box(new Rect(10, 10, 430, 110),
            "MOTEUR PHYSIQUE\n" +
            "Systèmes : " + systemes.Count + "   Particules : " + totalParticules + "   Corps : " + corps.Count + "\n" +
            "Phase large : " + typePhaseLarge + "   Paires : " + paires.Count + "   Contacts : " + contacts.Count + "\n" +
            "Pas de physique : " + msPas.ToString("F2") + " ms   |   " + fps.ToString("F0") + " FPS");
    }
}