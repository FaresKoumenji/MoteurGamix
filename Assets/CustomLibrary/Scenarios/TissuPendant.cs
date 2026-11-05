using UnityEngine;

/// <summary>
/// Scénario : un tissu accroché par quelques particules au centre de sa rangée du haut. On le lance
/// incliné : il se balance de gauche à droite comme un pendule (gravité + traînée de l'air), puis
/// la balançoire s'atténue. Les boutons "Pousser" relancent le mouvement.
/// Utilise le module Tissu (FLEX §8), la gravité et un sol. Le Transform n'est jamais modifié.
///
/// STABILITÉ (RK4, 16 sous-pas, dt = 0.02) : l'amortissement des ressorts ne doit pas dépasser
/// environ 1.4 sinon la simulation diverge (critère h . lambda &lt; 2.78 sur l'axe réel).
/// On le limite donc à 1.2 (OnValidate pour l'Inspector, Start pour la sécurité à l'exécution).
/// </summary>
public class TissuPendant : MonoBehaviour
{
    [Header("Tissu")]
    public int nx = 16;
    public int ny = 20;
    public float largeur = 2f;
    public float hauteur = 2.5f;
    public float masseTotale = 2f;
    public float raideur = 800f;

    [Tooltip("Amortissement des ressorts. Limité à 1.2 (au-delà RK4 diverge avec 16 sous-pas).")]
    public float amortissement = 0.5f;

    [Tooltip("Rupture quand l'allongement d'un ressort dépasse ce ratio (0 = incassable)")]
    public float seuilRupture = 0.5f;
    public CustomVector3 origine = new CustomVector3(-1f, 3f, 0f);

    [Header("Balancement")]
    [Tooltip("Nombre de particules accrochées au centre de la rangée du haut (1, 2 ou 4)")]
    public int pointsFixes = 2;
    [Tooltip("Inclinaison initiale autour du point d'accroche (degrés) : plus grand = plus d'amplitude")]
    public float angleInitial = 30f;
    [Tooltip("Traînée de l'air F = -lambda.v par particule : plus grand = le balancement s'arrête plus vite")]
    public float trainee = 0.0005f;
    [Tooltip("Vitesse ajoutée (m/s) par les boutons Pousser")]
    public float vitessePoussee = 2f;

    [Header("Intégration numérique")]
    public ModeIntegration mode = ModeIntegration.RK4;
    public int sousPas = 16;

    [Header("Affichage")]
    public bool dessinerRessorts = true;     // lignes vertes (Gizmos) en plus des points
    public bool placerCameraAutomatiquement = true;

    private const float AMORTISSEMENT_MAX = 1.2f;

    private PhysicsWorld monde;
    private Tissu tissu;

    // Appelée dans l'éditeur à chaque modification d'une valeur de l'Inspector
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

        // Sécurité : la valeur sérialisée dans la scène peut être ancienne (ex. 4)
        if (amortissement > AMORTISSEMENT_MAX)
        {
            Debug.LogWarning($"[TissuPendant] amortissement {amortissement} trop grand pour RK4, ramené à {AMORTISSEMENT_MAX}");
            amortissement = AMORTISSEMENT_MAX;
        }
        pointsFixes = Mathf.Clamp(pointsFixes, 1, nx);

        // Tissu sans fixation : on accroche nous-mêmes quelques particules au centre du bord haut
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

        // Inclinaison initiale : rotation dans le plan XY autour du point d'accroche.
        // Une rotation conserve toutes les distances : les ressorts restent à leur longueur de repos.
        float a = angleInitial * Mathf.Deg2Rad;
        float cs = Mathf.Cos(a), sn = Mathf.Sin(a);
        for (int i = 0; i < s.N; i++)
        {
            if (s.p[i].invmass <= 0f) continue;
            float dx = s.p[i].x.x - pivot.x;
            float dy = s.p[i].x.y - pivot.y;
            s.p[i].x = new CustomVector3(pivot.x + cs * dx - sn * dy,
                                         pivot.y + sn * dx + cs * dy,
                                         s.p[i].x.z);
        }

        // Forces : gravité et traînée de l'air
        s.forces.Add(new ForceGravite(new CustomVector3(0f, -9.81f, 0f)));
        s.forces.Add(new ForceTrainee(trainee));

        // Sol (pour que le tissu ne passe pas à travers)
        s.contraintes.Add(new ContraintePlanFluide(new CustomVector3(0f, 1f, 0f), CustomVector3.zero));

        monde.EnregistrerSysteme(s);

        // Rendu des particules
        var rendu = RenduSolide.Obtenir();
        rendu.AjouterParticules(s, 0.05f, new Color(0.2f, 0.5f, 0.9f));

        if (placerCameraAutomatiquement) PlacerCamera();

        Debug.Log($"[TissuPendant] {s.N} particules, {pointsFixes} accrochée(s), amortissement = {amortissement}, sous-pas = {sousPas}");
    }

    /// <summary>Ajoute une vitesse horizontale à toutes les particules libres (balancement).</summary>
    void Pousser(float vx)
    {
        if (tissu == null) return;
        SystemeParticules s = tissu.systeme;
        for (int i = 0; i < s.N; i++)
        {
            if (s.p[i].invmass <= 0f) continue;
            s.p[i].v = s.p[i].v + new CustomVector3(vx, 0f, 0f);
        }
    }

    void OnGUI()
    {
        if (tissu == null) return;
        if (GUI.Button(new Rect(10, 90, 110, 30), "< Pousser")) Pousser(-vitessePoussee);
        if (GUI.Button(new Rect(130, 90, 110, 30), "Pousser >")) Pousser(vitessePoussee);
    }

    void PlacerCamera()
    {
        var cam = Camera.main;
        if (cam == null) return;
        cam.transform.position = new Vector3(0f, 2f, -6f);
        cam.transform.LookAt(new Vector3(0f, 1.5f, 0f));
        cam.fieldOfView = 60f;
    }

    // Les ressorts structurels dessinés en lignes donnent l'aspect d'un tissu
    // (activer le bouton "Gizmos" de la vue Game).
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