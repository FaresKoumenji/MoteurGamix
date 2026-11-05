using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Scénario : une boule frappe une tour cylindrique en particules.
/// Utilise uniquement les modules existants :
///   - SystemeParticules pour la tour
///   - ForceRessort (structurel + cisaillement) pour la cohésion
///   - ForceRessort.seuilRupture pour la cassure
///   - ContraintePlanFluide pour le sol
///   - CustomRigidbody + ContrainteParticuleRigidbody pour la boule
/// Aucune physique n'est codée dans ce fichier : uniquement instanciation.
/// </summary>
public class TourCylindrique : MonoBehaviour
{
    [Header("Tour cylindrique")]
    public float rayonTour = 1.5f;
    public float hauteurTour = 6f;
    public float pasParticule = 0.15f;      // espacement
    public float masseTour = 80f;
    public float raideur = 4000f;
    public float amortissement = 8f;
    public float seuilRupture = 0.05f;
    public CustomVector3 baseTour = new CustomVector3(0f, 0f, 0f);

    [Header("Boule")]
    public float rayonBoule = 0.5f;
    public float masseBoule = 30f;
    public float vitesseInitiale = 20f;
    public CustomVector3 positionBoule = new CustomVector3(-10f, 1.5f, 0f);
    public CustomVector3 directionBoule = new CustomVector3(1f, 0f, 0f);

    [Header("Sol")]
    public float epaisseurSol = 0.5f;

    [Header("Caméra")]
    public bool placerCameraAutomatiquement = true;

    private PhysicsWorld monde;
    private SystemeParticules tour;
    private ForceRessort ressorts;
    private CustomRigidbody boule;

    void Start()
    {
        monde = PhysicsWorld.Obtenir();

        CreerSolPhysique();
        CreerBoule();
        CreerTourCylindrique();

        // Rendu
        var rendu = RenduSolide.Obtenir();
        rendu.AjouterCubeFixe(new Vector3(0, -0.25f, 0),
                              new Vector3(30f, 0.5f, 30f),
                              new Color(0.7f, 0.7f, 0.7f));
        rendu.AjouterParticules(tour, pasParticule * 0.5f, new Color(0.9f, 0.85f, 0.7f));
        rendu.AjouterRigidbody(boule, new Color(1f, 0.6f, 0.2f));

        if (placerCameraAutomatiquement) PlacerCamera();

        Debug.Log($"[TourCylindrique] {tour.N} particules, {ressorts.ressorts.Count} ressorts, boule={masseBoule}kg");
    }

    // ---------- SOL ----------
    void CreerSolPhysique()
    {
        GameObject go = new GameObject("Sol");
        go.AddComponent<CustomCube>();
        CustomRigidbody rb = go.AddComponent<CustomRigidbody>();

        rb.mass = 100000f;
        rb.hasGravity = false;
        rb.estStatique = true;
        rb.initialPosition = new CustomVector3(0f, -epaisseurSol * 0.5f, 0f);

        var cube = go.GetComponent<CustomCube>();
        float hx = 15f, hy = epaisseurSol * 0.5f, hz = 15f;
        cube.vertexA = new Vector3(-hx, -hy, -hz);
        cube.vertexB = new Vector3( hx, -hy, -hz);
        cube.vertexC = new Vector3( hx, -hy,  hz);
        cube.vertexD = new Vector3(-hx, -hy,  hz);
        cube.vertexE = new Vector3(-hx,  hy, -hz);
        cube.vertexF = new Vector3( hx,  hy, -hz);
        cube.vertexG = new Vector3( hx,  hy,  hz);
        cube.vertexH = new Vector3(-hx,  hy,  hz);

        rb.Reinitialiser();
        rb.forme = FormeRigide.AABB;
        monde.EnregistrerCorps(rb);
    }

    // ---------- BOULE ----------
    void CreerBoule()
    {
        GameObject goBoule = new GameObject("Boule");
        goBoule.AddComponent<CustomCube>();
        boule = goBoule.AddComponent<CustomRigidbody>();

        boule.mass = masseBoule;
        boule.hasGravity = true;
        boule.initialPosition = positionBoule;
        boule.drag = 0.1f;
        boule.angularDrag = 0.1f;

        boule.Reinitialiser();
        boule.forme = FormeRigide.Sphere;
        boule.rayon = rayonBoule;

        CustomVector3 v0 = directionBoule.normalized * vitesseInitiale;
        boule.state.P = v0 * masseBoule;
        boule.ForcerRefresh();

        monde.EnregistrerCorps(boule);
    }

    // ---------- TOUR CYLINDRIQUE ----------
    void CreerTourCylindrique()
    {
        // 1) Compter les particules nécessaires
        var positions = new List<CustomVector3>();
        float pasAngulaire = pasParticule / rayonTour;
        int nbAngulaire = Mathf.CeilToInt(2f * Mathf.PI / pasAngulaire);
        int nbVertical = Mathf.CeilToInt(hauteurTour / pasParticule);
        int nbRadial = Mathf.CeilToInt(rayonTour / pasParticule);

        // 2) Remplir le cylindre (coquille + intérieur)
        for (int iy = 0; iy < nbVertical; iy++)
        {
            float y = baseTour.y + (iy + 0.5f) * pasParticule;
            for (int ir = 0; ir < nbRadial; ir++)
            {
                float r = (ir + 0.5f) * pasParticule;
                if (r > rayonTour) continue;

                // Anneau à ce rayon
                int nAng = Mathf.Max(6, Mathf.CeilToInt(2f * Mathf.PI * r / pasParticule));
                for (int ia = 0; ia < nAng; ia++)
                {
                    float theta = ia * 2f * Mathf.PI / nAng;
                    float x = baseTour.x + r * Mathf.Cos(theta);
                    float z = baseTour.z + r * Mathf.Sin(theta);
                    positions.Add(new CustomVector3(x, y, z));
                }
            }
        }

        // 3) Créer le système de particules
        int N = positions.Count;
        tour = new SystemeParticules(N, "Tour cylindrique");
        tour.mode = ModeIntegration.EulerSemiImplicite;
        tour.sousPas = 12;
        tour.vitesseMax = 50f;

        float m = masseTour / N;
        for (int i = 0; i < N; i++)
        {
            tour.p[i].x = positions[i];
            tour.p[i].v = CustomVector3.zero;
            tour.p[i].m = m;
            tour.p[i].invmass = 1f / m;
            tour.p[i].phase = 0;
        }

        // 4) Ressorts : chaque particule reliée à ses voisines dans un rayon ~1.5 pas
        ressorts = new ForceRessort();
        ressorts.seuilRupture = seuilRupture;

        GrilleVoisins grille = new GrilleVoisins { h = pasParticule * 1.8f };
        grille.Construire(positions.ToArray(), N);

        float distanceMax = pasParticule * 1.5f;
        float distanceMax2 = distanceMax * distanceMax;

        for (int i = 0; i < N; i++)
        {
            grille.PourChaqueVoisin(positions.ToArray(), i, j =>
            {
                if (j <= i) return;
                float d2 = (positions[i] - positions[j]).sqrMagnitude;
                if (d2 >= distanceMax2) return;

                // Ressort structurel (ou cisaillement selon la distance)
                bool structurel = d2 < pasParticule * pasParticule * 1.1f;
                float k = structurel ? raideur : raideur * 0.5f;
                int type = structurel ? ForceRessort.Structurel : ForceRessort.Cisaillement;
                ressorts.Ajouter(tour, i, j, k, amortissement, type, true);
            });
        }

        tour.forces.Add(ressorts);
        tour.forces.Add(new ForceGravite(new CustomVector3(0f, -9.81f, 0f)));
        tour.contraintes.Add(new ContraintePlanFluide(new CustomVector3(0f, 1f, 0f), CustomVector3.zero));

        // Couplage boule ↔ tour
        var couplage = new ContrainteParticuleRigidbody(
            new List<CustomRigidbody> { boule }, pasParticule * 0.6f);
        tour.contraintes.Add(couplage);

        monde.EnregistrerSysteme(tour);
    }

    void PlacerCamera()
    {
        var cam = Camera.main;
        if (cam == null) return;
        cam.transform.position = new Vector3(-14f, 6f, -14f);
        cam.transform.LookAt(new Vector3(0f, 2.5f, 0f));
        cam.fieldOfView = 55f;
    }
}