using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Scénario : tissu lourd suspendu qui bascule sur des cubes empilés,
/// avec de la fumée qui monte et qui pousse le tissu.
/// Ce fichier ne contient QUE de l'instanciation : toute la physique est dans les modules.
///   - Tissu (Tissu + ForceGravite + ForceVent pour la poussée de fumée)
///   - Cubes (CustomRigidbody AABB) + PhaseEtroite + ReponseCollision
///   - ContrainteParticuleRigidbody (couplage tissu ↔ cubes)
///   - Fumee (particules passives avec gravité inversée)
///   - ForceVent (fumée → tissu)
/// </summary>
public class TissuFumeeCubes : MonoBehaviour
{
    [Header("Tissu")]
    public int nxTissu = 16;
    public int nyTissu = 20;
    public float largeurTissu = 2f;
    public float hauteurTissu = 2.5f;
    public float masseTissu = 4f;
    public float raideurTissu = 800f;
    public float amortissementTissu = 4f;
    public float seuilRuptureTissu = 0.5f;
    public CustomVector3 origineTissu = new CustomVector3(-1.2f, 3.5f, 0f);

    [Header("Cubes empilés")]
    public int nbCubes = 3;
    public float tailleCube = 0.6f;
    public float masseCube = 2f;
    public CustomVector3 baseCubes = new CustomVector3(1.2f, 0f, 0f);

    [Header("Fumée")]
    public int nbParticulesFumee = 400;
    public CustomVector3 origineFumee = new CustomVector3(0.5f, 0.2f, 0f);
    public CustomVector3 rayonEmissionFumee = new CustomVector3(0.8f, 0.1f, 0.8f);
    public float forceVentFumee = 6f;
    public float rayonVentFumee = 2.5f;
    public float porteeVentFumee = 8f;

    [Header("Sol")]
    public float epaisseurSol = 0.5f;

    [Header("Caméra")]
    public bool placerCameraAutomatiquement = true;

    private PhysicsWorld monde;
    private Tissu tissu;
    private Fumee fumee;
    private ForceVent ventFumee;
    private List<CustomRigidbody> cubes = new List<CustomRigidbody>();

    void Start()
    {
        monde = PhysicsWorld.Obtenir();

        CreerSolPhysique();
        CreerMurs();
        CreerCubes();
        CreerTissu();
        CreerFumee();

        // Rendu
        var rendu = RenduSolide.Obtenir();

        // Sol
        rendu.AjouterCubeFixe(new Vector3(0, -0.25f, 0),
                              new Vector3(20f, 0.5f, 20f),
                              new Color(0.7f, 0.7f, 0.7f));

        // Tissu (bleu)
        rendu.AjouterParticules(tissu.systeme, 0.05f, new Color(0.2f, 0.5f, 0.9f));

        // Cubes (jaune, bleu, vert — comme dans la capture)
        Color[] couleursCubes = {
            new Color(1f, 0.9f, 0.2f),   // jaune
            new Color(0.4f, 0.7f, 1f),   // bleu
            new Color(0.3f, 0.9f, 0.4f)  // vert
        };
        for (int i = 0; i < cubes.Count; i++)
            rendu.AjouterRigidbody(cubes[i], couleursCubes[i % couleursCubes.Length]);

        // Fumée (gris clair)
        rendu.AjouterParticules(fumee.systeme, 0.08f, new Color(0.8f, 0.8f, 0.8f, 0.7f));

        if (placerCameraAutomatiquement) PlacerCamera();

        Debug.Log($"[TissuFumeeCubes] tissu={tissu.systeme.N}, cubes={cubes.Count}, fumée={fumee.systeme.N}");
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
        float hx = 10f, hy = epaisseurSol * 0.5f, hz = 10f;
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

    // ---------- MURS ----------
    void CreerMurs()
    {
        CreerMur(new Vector3(-6, 4, 0), new Vector3(0.5f, 8f, 10f), "MurGauche");
        CreerMur(new Vector3( 6, 4, 0), new Vector3(0.5f, 8f, 10f), "MurDroit");
        CreerMur(new Vector3(0, 4, -5), new Vector3(10f, 8f, 0.5f), "MurArriere");
    }

    void CreerMur(Vector3 centre, Vector3 taille, string nom)
    {
        GameObject go = new GameObject(nom);
        go.AddComponent<CustomCube>();
        CustomRigidbody rb = go.AddComponent<CustomRigidbody>();

        rb.mass = 100000f;
        rb.hasGravity = false;
        rb.estStatique = true;
        rb.initialPosition = new CustomVector3(centre.x, centre.y, centre.z);

        var cube = go.GetComponent<CustomCube>();
        float hx = taille.x * 0.5f, hy = taille.y * 0.5f, hz = taille.z * 0.5f;
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

    // ---------- CUBES EMPILÉS ----------
    void CreerCubes()
    {
        for (int i = 0; i < nbCubes; i++)
        {
            GameObject go = new GameObject($"Cube_{i}");
            go.AddComponent<CustomCube>();
            CustomRigidbody rb = go.AddComponent<CustomRigidbody>();

            rb.mass = masseCube;
            rb.hasGravity = true;
            rb.initialPosition = new CustomVector3(
                baseCubes.x,
                baseCubes.y + i * tailleCube + tailleCube * 0.5f,
                baseCubes.z);
            rb.drag = 0.1f;
            rb.angularDrag = 0.1f;

            var cube = go.GetComponent<CustomCube>();
            float h = tailleCube * 0.5f;
            cube.vertexA = new Vector3(-h, -h, -h);
            cube.vertexB = new Vector3( h, -h, -h);
            cube.vertexC = new Vector3( h, -h,  h);
            cube.vertexD = new Vector3(-h, -h,  h);
            cube.vertexE = new Vector3(-h,  h, -h);
            cube.vertexF = new Vector3( h,  h, -h);
            cube.vertexG = new Vector3( h,  h,  h);
            cube.vertexH = new Vector3(-h,  h,  h);

            rb.Reinitialiser();
            rb.forme = FormeRigide.AABB;

            monde.EnregistrerCorps(rb);
            cubes.Add(rb);
        }
    }

    // ---------- TISSU ----------
    void CreerTissu()
    {
        tissu = new Tissu(
            nxTissu, nyTissu, largeurTissu, hauteurTissu, origineTissu,
            masseTissu, raideurTissu, amortissementTissu, seuilRuptureTissu,
            FixationTissu.RangeeHaute);

        // Gravité
        tissu.systeme.forces.Add(new ForceGravite(new CustomVector3(0f, -9.81f, 0f)));

        // Contraintes : sol + couplage cubes ↔ tissu
        tissu.systeme.contraintes.Add(new ContraintePlanFluide(new CustomVector3(0f, 1f, 0f), CustomVector3.zero));

        var couplage = new ContrainteParticuleRigidbody(cubes, 0.08f);
        tissu.systeme.contraintes.Add(couplage);

        monde.EnregistrerSysteme(tissu.systeme);
    }

    // ---------- FUMÉE ----------
    void CreerFumee()
    {
        // Fumée : particules passives avec gravité inversée (montent)
        fumee = new Fumee(nbParticulesFumee, origineFumee, rayonEmissionFumee);

        // Fumée sans contraintes de sol (elle traverse tout et monte)
        // Elle est enregistrée dans le monde pour être rendue
        monde.EnregistrerSysteme(fumee.systeme);

        // Force de vent appliquée au tissu par la fumée
        // (ForceVent est appliquée aux triangles du tissu : la fumée pousse le tissu)
        ventFumee = new ForceVent(tissu.triangles);
        ventFumee.origine = origineFumee;
        ventFumee.axe = new CustomVector3(0f, 1f, 0f);   // vers le haut
        ventFumee.vitesse = forceVentFumee;
        ventFumee.rayon = rayonVentFumee;
        ventFumee.portee = porteeVentFumee;
        ventFumee.turbulence = 0.3f;
        ventFumee.coefficient = 1.5f;

        tissu.systeme.forces.Add(ventFumee);
    }

    void PlacerCamera()
    {
        var cam = Camera.main;
        if (cam == null) return;
        cam.transform.position = new Vector3(6f, 5f, -8f);
        cam.transform.LookAt(new Vector3(0f, 2.5f, 0f));
        cam.fieldOfView = 60f;
    }
}