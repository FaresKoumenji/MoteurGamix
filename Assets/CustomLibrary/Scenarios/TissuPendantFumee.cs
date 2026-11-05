using UnityEngine;

/// <summary>
/// Scénario : un tissu accroché au centre de sa rangée du haut se balance d'avant en arrière
/// (charnière le long de X). De la fumée monte du sol et le percute.
/// Ce fichier ne contient aucune physique : il instancie et branche les modules
/// (Tissu, Fumee, CouplageFumeeTissu, ForceGravite, ForceTrainee, ContraintePlanFluide, RenduSolide).
/// </summary>
public class TissuPendantFumee : MonoBehaviour
{
    [Header("Tissu")]
    public int nx = 16;
    public int ny = 20;
    public float largeur = 2f;
    public float hauteur = 2.5f;
    public float masseTotale = 5f;
    public float raideur = 800f;
    [Tooltip("Limité à 1.2 (au-delà RK4 diverge avec 16 sous-pas).")]
    public float amortissement = 0.5f;
    public float seuilRupture = 0.5f;
    public CustomVector3 origine = new CustomVector3(-1f, 3f, 0f);

    [Header("Balancement")]
    [Tooltip("Nombre de particules accrochées au centre de la rangée du haut")]
    public int pointsFixes = 2;
    [Tooltip("Inclinaison avant/arrière autour de la charnière (axe X). 0 = pend droit")]
    public float angleInitial = 0f;
    public float trainee = 0.0005f;
    public float vitessePoussee = 2f;

    [Header("Intégration numérique")]
    public ModeIntegration mode = ModeIntegration.RK4;
    public int sousPas = 16;

    [Header("Fumée")]
    public int nbFumee = 800;
    public CustomVector3 origineFumee = new CustomVector3(0f, 0f, -0.3f);
    public CustomVector3 rayonEmission = new CustomVector3(0.6f, 0.05f, 0.2f);
    public float dureeVieFumee = 6f;
    [Tooltip("Positif = monte (g effectif = 9.81 x flottabilité)")]
    public float flottabilite = 0.5f;
    [Tooltip("Traînée de la fumée : plus grand = montée plus lente")]
    public float traineeFumee = 1.5f;
    public float rayonRenduFumee = 0.08f;
    public Color couleurFumee = new Color(0.75f, 0.75f, 0.78f, 1f);

    [Header("Couplage fumée <-> tissu")]
    public ParametresCouplage couplage = new ParametresCouplage();

    [Header("Affichage")]
    public bool dessinerRessorts = true;
    public bool placerCameraAutomatiquement = true;

    private const float AMORTISSEMENT_MAX = 1.2f;

    private PhysicsWorld monde;
    private Tissu tissu;
    private Fumee fumee;
    private CouplageFumeeTissu couplageFumeeTissu;

    void OnValidate()
    {
        amortissement = Mathf.Clamp(amortissement, 0f, AMORTISSEMENT_MAX);
        sousPas = Mathf.Max(1, sousPas);
        pointsFixes = Mathf.Clamp(pointsFixes, 1, Mathf.Max(1, nx));
        trainee = Mathf.Max(0f, trainee);
    }

    void Start()
    {
        monde = PhysicsWorld.Obtenir();

        if (amortissement > AMORTISSEMENT_MAX) amortissement = AMORTISSEMENT_MAX;
        pointsFixes = Mathf.Clamp(pointsFixes, 1, nx);

        tissu = new Tissu(
            nx, ny, largeur, hauteur, origine,
            masseTotale, raideur, amortissement, seuilRupture,
            FixationTissu.Aucune);

        SystemeParticules s = tissu.systeme;
        s.mode = mode;
        s.sousPas = sousPas;

        int premier = nx / 2 - pointsFixes / 2;
        CustomVector3 pivot = CustomVector3.zero;
        for (int q = 0; q < pointsFixes; q++)
        {
            int idx = tissu.Index(premier + q, 0);
            s.Fixer(idx);
            pivot += s.p[idx].x;
        }
        pivot = pivot / pointsFixes;

        // Inclinaison initiale dans le plan YZ autour de la charnière (axe X)
        if (Mathf.Abs(angleInitial) > 0.001f)
        {
            float a = angleInitial * Mathf.Deg2Rad;
            float cs = Mathf.Cos(a), sn = Mathf.Sin(a);
            for (int i = 0; i < s.N; i++)
            {
                if (s.p[i].invmass <= 0f) continue;
                float dy = s.p[i].x.y - pivot.y;
                float dz = s.p[i].x.z - pivot.z;
                s.p[i].x = new CustomVector3(s.p[i].x.x,
                                             pivot.y + cs * dy - sn * dz,
                                             pivot.z + sn * dy + cs * dz);
            }
        }

        s.forces.Add(new ForceGravite(new CustomVector3(0f, -9.81f, 0f)));
        s.forces.Add(new ForceTrainee(trainee));
        s.contraintes.Add(new ContraintePlanFluide(new CustomVector3(0f, 1f, 0f), CustomVector3.zero));

        monde.EnregistrerSysteme(s);

        RenduSolide.Obtenir().AjouterParticules(s, 0.05f, new Color(0.2f, 0.5f, 0.9f));

        // Fumée : un second système de particules, enregistré dans le monde et rendu comme le tissu
        fumee = new Fumee(nbFumee, origineFumee, rayonEmission, dureeVieFumee, flottabilite, traineeFumee);
        monde.EnregistrerSysteme(fumee.systeme);
        RenduSolide.Obtenir().AjouterParticules(fumee.systeme, rayonRenduFumee, couleurFumee);

        // Échange de quantité de mouvement fumée <-> tissu
        couplageFumeeTissu = new CouplageFumeeTissu(fumee.systeme, s, couplage);

        if (placerCameraAutomatiquement) PlacerCamera();
    }

    void Update()
    {
        if (tissu == null) return;
        fumee.Recycler(Time.deltaTime);
        couplageFumeeTissu.Appliquer(Time.deltaTime);
    }

    void Pousser(float vz)
    {
        if (tissu == null) return;
        SystemeParticules s = tissu.systeme;
        for (int i = 0; i < s.N; i++)
        {
            if (s.p[i].invmass <= 0f) continue;
            s.p[i].v = s.p[i].v + new CustomVector3(0f, 0f, vz);
        }
    }

    void OnGUI()
    {
        if (tissu == null) return;
        if (GUI.Button(new Rect(10, 90, 110, 30), "< Arrière")) Pousser(vitessePoussee);
        if (GUI.Button(new Rect(130, 90, 110, 30), "Avant >")) Pousser(-vitessePoussee);
        if (GUI.Button(new Rect(10, 130, 230, 30), couplage.actif ? "Interaction fumée : ON" : "Interaction fumée : OFF"))
            couplage.actif = !couplage.actif;
    }

    void PlacerCamera()
    {
        var cam = Camera.main;
        if (cam == null) return;
        cam.transform.position = new Vector3(6f, 2f, -3f);
        cam.transform.LookAt(new Vector3(0f, 1.5f, 0f));
        cam.fieldOfView = 60f;
    }

    void OnDrawGizmos()
    {
        if (!dessinerRessorts || tissu == null) return;

        SystemeParticules s = tissu.systeme;
        var liste = tissu.ressorts.ressorts;

        Gizmos.color = new Color(0.3f, 0.9f, 0.4f);
        for (int q = 0; q < liste.Count; q++)
        {
            Ressort r = liste[q];
            if (!r.actif || r.type != ForceRessort.Structurel) continue;
            Gizmos.DrawLine(s.p[r.i].x.ToUnity(), s.p[r.j].x.ToUnity());
        }
    }
}