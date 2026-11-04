using System.Collections.Generic;
using UnityEngine;

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

    [Header("Fluides")]
    public List<Fluide> fluides = new List<Fluide>();

    [Header("Corps rigides")]
    public List<CustomRigidbody> corps = new List<CustomRigidbody>();

    [Header("Phase large")]
    public TypePhaseLarge typePhaseLarge = TypePhaseLarge.Grille;

    [Header("Réponse aux collisions")]
    public float restitution = 0.3f;
    public float frottement = 0.4f;

    [Header("Affichage")]
    public bool afficherStats = true;

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

    public void EnregistrerFluide(Fluide f)
    {
        if (!fluides.Contains(f)) fluides.Add(f);
    }

    public void EnregistrerCorps(CustomRigidbody rb)
    {
        if (!corps.Contains(rb)) corps.Add(rb);
    }

    void FixedUpdate()
    {
        Step(Time.fixedDeltaTime);
    }

    public void Step(float dt)
    {
        chrono.Reset(); chrono.Start();

        // 1) Intégration
        for (int k = 0; k < systemes.Count; k++) systemes[k].Avancer(dt);
        for (int k = 0; k < fluides.Count; k++) fluides[k].Avancer(dt);
        for (int k = 0; k < corps.Count; k++) corps[k].Integrer(dt);

        NettoyerNaN();

        // 2) Volumes
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

        // 5) Réponse
        ReponseCollision.ResoudreContacts(contacts, corps, dt, restitution, frottement);

        NettoyerNaN();

        // 6) Synchro visuelle
        for (int k = 0; k < corps.Count; k++) corps[k].SynchroVisuelle();

        chrono.Stop();
        msPas = chrono.Elapsed.TotalMilliseconds;
    }

    void NettoyerNaN()
    {
        for (int k = 0; k < corps.Count; k++)
        {
            var rb = corps[k];
            if (rb == null) continue;

            bool nan =
                float.IsNaN(rb.state.x.x) || float.IsNaN(rb.state.x.y) || float.IsNaN(rb.state.x.z) ||
                float.IsNaN(rb.state.P.x) || float.IsNaN(rb.state.P.y) || float.IsNaN(rb.state.P.z) ||
                float.IsNaN(rb.state.L.x) || float.IsNaN(rb.state.L.y) || float.IsNaN(rb.state.L.z) ||
                float.IsNaN(rb.state.R.X.x) || float.IsNaN(rb.state.R.Y.y) || float.IsNaN(rb.state.R.Z.z);

            if (nan)
            {
                Debug.LogWarning($"[NaN] {rb.name} → reset à initialPosition ({rb.initialPosition.x}, {rb.initialPosition.y}, {rb.initialPosition.z})");
                rb.state.x = rb.initialPosition;
                rb.state.P = CustomVector3.zero;
                rb.state.L = CustomVector3.zero;
                rb.state.R = CustomMatrix3.identity;
                rb.ForcerRefresh();
            }
        }

        for (int k = 0; k < fluides.Count; k++)
        {
            var s = fluides[k].systeme;
            for (int i = 0; i < s.N; i++)
            {
                if (float.IsNaN(s.p[i].x.x) || float.IsNaN(s.p[i].x.y) || float.IsNaN(s.p[i].x.z))
                {
                    Debug.LogWarning($"[NaN] fluide particule {i} → reset");
                    s.p[i].x = CustomVector3.zero;
                    s.p[i].v = CustomVector3.zero;
                }
            }
        }
    }

    void ChoisirPhaseLarge()
    {
        if (typePhaseLarge != typePhaseLargePrecedent || phaseLarge == null)
        {
            switch (typePhaseLarge)
            {
                case TypePhaseLarge.ForceBrute: phaseLarge = new PhaseLargeForceBrute(); break;
                case TypePhaseLarge.SAP: phaseLarge = new PhaseLargeSAP(); break;
                case TypePhaseLarge.Grille: phaseLarge = new PhaseLargeGrille(); break;
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

        int partFluides = 0;
        for (int k = 0; k < fluides.Count; k++) partFluides += fluides[k].systeme.N;

        GUI.Box(new Rect(10, 10, 480, 130),
            "MOTEUR PHYSIQUE\n" +
            "Systèmes : " + systemes.Count + "   Particules : " + totalParticules + "   Corps : " + corps.Count + "\n" +
            "Fluides : " + fluides.Count + "   Particules fluides : " + partFluides + "\n" +
            "Phase large : " + typePhaseLarge + "   Paires : " + paires.Count + "   Contacts : " + contacts.Count + "\n" +
            "Pas de physique : " + msPas.ToString("F2") + " ms   |   " + fps.ToString("F0") + " FPS");
    }
}