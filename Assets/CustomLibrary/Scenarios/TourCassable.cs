using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Scénario : une boule frappe une tour.
/// Mode Rigide  : ressorts très raides + seuil bas → casse nette au contact, sans flexion.
/// Mode Souple  : ressorts souples + seuil haut → se plie puis casse.
/// Les deux utilisent CorpsDeformable (FLEX §5.3, §8) — pas de duplication.
/// </summary>
public class TourCassable : MonoBehaviour
{
    public enum ComportementTour { Rigide, Souple }

    [Header("Comportement")]
    public ComportementTour comportement = ComportementTour.Rigide;

    [Header("Tour")]
    public int nxTour = 6;
    public int nyTour = 20;
    public int nzTour = 6;
    public float pasTour = 0.15f;
    public float masseTour = 30f;
    public CustomVector3 centreTour = new CustomVector3(0f, 1.5f, 0f);

    [Header("Boule")]
    public float rayonBoule = 0.5f;
    public float masseBoule = 30f;
    public float vitesseInitiale = 25f;
    public CustomVector3 positionBoule = new CustomVector3(-8f, 3f, 0f);
    public CustomVector3 directionBoule = new CustomVector3(1f, -0.05f, 0f);

    [Header("Sol")]
    public float epaisseurSol = 0.5f;

    [Header("Caméra")]
    public bool placerCameraAutomatiquement = true;

    private PhysicsWorld monde;
    private CorpsDeformable tour;
    private CustomRigidbody boule;

    void Start()
    {
        monde = PhysicsWorld.Obtenir();

        CreerSolPhysique();
        CreerBoule();
        CreerTour();

        // Rendu
        var rendu = RenduSolide.Obtenir();
        rendu.AjouterCubeFixe(new Vector3(0, -0.25f, 0),
                              new Vector3(30f, 0.5f, 30f),
                              new Color(0.7f, 0.7f, 0.7f));
        rendu.AjouterParticules(tour.systeme, pasTour * 0.5f, new Color(0.6f, 0.8f, 0.4f));
        rendu.AjouterRigidbody(boule, new Color(1f, 0.3f, 0.3f));

        if (placerCameraAutomatiquement) PlacerCamera();

        Debug.Log($"[TourCassable] monde={monde.corps.Count} corps, mode={comportement}, particules={tour.systeme.N}");
    }

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
        cube.vertexB = new Vector3(hx, -hy, -hz);
        cube.vertexC = new Vector3(hx, -hy, hz);
        cube.vertexD = new Vector3(-hx, -hy, hz);
        cube.vertexE = new Vector3(-hx, hy, -hz);
        cube.vertexF = new Vector3(hx, hy, -hz);
        cube.vertexG = new Vector3(hx, hy, hz);
        cube.vertexH = new Vector3(-hx, hy, hz);

        rb.Reinitialiser();
        rb.forme = FormeRigide.AABB;
        monde.EnregistrerCorps(rb);
    }

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

    void CreerTour()
    {
        float raideur, amortissement, seuil;
        int sousPas;

        if (comportement == ComportementTour.Rigide)
        {
            raideur = 50000f;
            amortissement = 50f;
            seuil = 0.01f;     // casse au moindre allongement
            sousPas = 24;
        }
        else
        {
            raideur = 2000f;
            amortissement = 8f;
            seuil = 0.10f;
            sousPas = 16;
        }

        tour = new CorpsDeformable(
            nxTour, nyTour, nzTour, pasTour, centreTour,
            masseTour, raideur, amortissement, seuil, CustomVector3.zero);

        tour.systeme.forces.Add(new ForceGravite(new CustomVector3(0f, -9.81f, 0f)));
        tour.systeme.contraintes.Add(new ContraintePlanFluide(new CustomVector3(0f, 1f, 0f), CustomVector3.zero));
        tour.systeme.sousPas = sousPas;
        tour.systeme.vitesseMax = 50f;

        // Couplage boule ↔ tour (la boule pousse les particules)
        var couplage = new ContrainteParticuleRigidbody(
            new List<CustomRigidbody> { boule }, pasTour * 0.4f);
        tour.systeme.contraintes.Add(couplage);

        monde.EnregistrerSysteme(tour.systeme);
    }

    void PlacerCamera()
    {
        var cam = Camera.main;
        if (cam == null) return;
        cam.transform.position = new Vector3(-14f, 6f, -14f);
        cam.transform.LookAt(new Vector3(0f, 2f, 0f));
        cam.fieldOfView = 55f;
    }
}