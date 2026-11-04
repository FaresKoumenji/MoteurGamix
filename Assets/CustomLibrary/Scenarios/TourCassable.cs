using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Scénario : une boule frappe une tour qui s'effondre en particules.
/// La tour est un CorpsDeformable (ressorts cassables, FLEX §5.3).
/// La boule est un CustomRigidbody (state X = (x, R, P, L)).
/// Le couplage est ContrainteParticuleRigidbody (FLEX §7.1).
/// </summary>
public class TourCassable : MonoBehaviour
{
    [Header("Tour")]
    public int nxTour = 6;
    public int nyTour = 20;
    public int nzTour = 6;
    public float pasTour = 0.15f;
    public float masseTour = 30f;
    public float raideurTour = 4000f;
    public float amortissementTour = 10f;
    public float seuilRupture = 0.08f;
    public CustomVector3 centreTour = new CustomVector3(0f, 1.5f, 0f);

    [Header("Boule")]
    public float rayonBoule = 0.5f;
    public float masseBoule = 15f;
    public float vitesseInitiale = 20f;
    public CustomVector3 positionBoule = new CustomVector3(-8f, 3f, 0f);
    public CustomVector3 directionBoule = new CustomVector3(1f, -0.1f, 0f);

    [Header("Caméra")]
    public bool placerCameraAutomatiquement = true;

    private PhysicsWorld monde;
    private CorpsDeformable tour;
    private CustomRigidbody boule;

    void Start()
    {
        monde = PhysicsWorld.Obtenir();

        // ---------- 1) Tour (CorpsDeformable attend la masse TOTALE) ----------
        tour = new CorpsDeformable(
            nxTour, nyTour, nzTour, pasTour, centreTour,
            masseTour,
            raideurTour, amortissementTour,
            seuilRupture,
            CustomVector3.zero);

        tour.systeme.forces.Add(new ForceGravite(new CustomVector3(0f, -9.81f, 0f)));
        tour.systeme.contraintes.Add(new ContraintePlanFluide(new CustomVector3(0f, 1f, 0f), CustomVector3.zero));

        // ---------- 2) Boule ----------
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

        // Vitesse initiale
        CustomVector3 v0 = directionBoule.normalized * vitesseInitiale;
        boule.state.P = v0 * masseBoule;
        boule.ForcerRefresh();

        monde.EnregistrerCorps(boule);

        // ---------- 3) Couplage particules ↔ boule ----------
        var couplage = new ContrainteParticuleRigidbody(
            new List<CustomRigidbody> { boule },
            rayonParticule: 0.05f);
        tour.systeme.contraintes.Add(couplage);

        // ---------- 4) Enregistrer la tour ----------
        monde.EnregistrerSysteme(tour.systeme);

        // ---------- 5) Rendu ----------
        var rendu = RenduSolide.Obtenir();

        rendu.AjouterCubeFixe(new Vector3(0, -0.25f, 0),
                              new Vector3(30f, 0.5f, 30f),
                              new Color(0.7f, 0.7f, 0.7f));

        rendu.AjouterParticules(tour.systeme, pasTour * 0.5f, new Color(0.6f, 0.8f, 0.4f));

        rendu.AjouterRigidbody(boule, new Color(1f, 0.3f, 0.3f));

        if (placerCameraAutomatiquement) PlacerCamera();

        Debug.Log($"[TourCassable] tour = {tour.systeme.N} particules, boule = {masseBoule} kg, v = {vitesseInitiale} m/s");
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