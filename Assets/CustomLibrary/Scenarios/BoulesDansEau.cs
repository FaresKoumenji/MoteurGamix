using System.Collections.Generic;
using UnityEngine;

public class BoulesDansEau : MonoBehaviour
{
    [Header("Récipient")]
    public float cote = 6f;
    public int nbParticulesEau = 0;
    public float hLissage = 0.5f;
    public float rho0 = 1000f;

    [Header("Boules")]
    public float rayonBoule = 0.6f;
    public float[] densites = { 400f, 1000f, 1600f };
    public CustomVector3[] positions = {
        new CustomVector3(-2f, 2f, 0f),
        new CustomVector3( 0f, 2f, 0f),
        new CustomVector3( 2f, 2f, 0f)
    };

    [Header("Caméra")]
    public bool placerCameraAutomatiquement = true;

    // Épaisseur des murs PHYSIQUES (épais : évite que le centre d'une boule se retrouve dans le mur)
    const float epPhys = 2f;
    // Épaisseur des murs affichés
    const float epRendu = 0.2f;

    private PhysicsWorld monde;
    private Fluide eau;
    private float d;

    void Start()
    {
        monde = PhysicsWorld.Obtenir();
        d = cote * 0.5f;

        if (nbParticulesEau < 0) nbParticulesEau = 0;

        var fond = new ContraintePlanFluide(new CustomVector3(0f, 1f, 0f), new CustomVector3(0f, -d, 0f));
        var murGauche = new ContraintePlanFluide(new CustomVector3(1f, 0f, 0f), new CustomVector3(-d, 0f, 0f));
        var murDroite = new ContraintePlanFluide(new CustomVector3(-1f, 0f, 0f), new CustomVector3(d, 0f, 0f));
        var murArriere = new ContraintePlanFluide(new CustomVector3(0f, 0f, 1f), new CustomVector3(0f, 0f, -d));
        var murAvant = new ContraintePlanFluide(new CustomVector3(0f, 0f, -1f), new CustomVector3(0f, 0f, d));

        CustomVector3 origine = new CustomVector3(-d, -d, -d);
        CustomVector3 taille = new CustomVector3(cote, cote * 0.55f, cote);

        eau = new Fluide(nbParticulesEau, origine, taille, hLissage, rho0);
        eau.systeme.contraintes.Add(fond);
        eau.systeme.contraintes.Add(murGauche);
        eau.systeme.contraintes.Add(murDroite);
        eau.systeme.contraintes.Add(murArriere);
        eau.systeme.contraintes.Add(murAvant);
        eau.systeme.vitesseMax = 3f;
        eau.systeme.sousPas = 8;

        monde.EnregistrerFluide(eau);

        // Boules
        for (int i = 0; i < densites.Length && i < positions.Length; i++)
        {
            float rho = densites[i];
            float volume = (4f / 3f) * Mathf.PI * rayonBoule * rayonBoule * rayonBoule;
            float masse = rho * volume;

            Debug.Log($"[BOULE {i}] rho={rho}, rayon={rayonBoule}, volume={volume:F4}, masse={masse:F2}");

            GameObject go = new GameObject($"Boule_{i}");
            go.AddComponent<CustomCube>();
            CustomRigidbody rb = go.AddComponent<CustomRigidbody>();

            rb.mass = masse;
            rb.hasGravity = true;
            rb.initialPosition = positions[i];
            rb.drag = 0.8f;
            rb.angularDrag = 0.8f;

            rb.Reinitialiser();

            rb.forme = FormeRigide.Sphere;
            rb.rayon = rayonBoule;

            Debug.Log($"[BOULE {i} APRES] mass={rb.mass}, invMass={rb.invMass:F6}, forme={rb.forme}, rayon={rb.rayon}");

            monde.EnregistrerCorps(rb);
            eau.couplage.solides.Add(rb);
        }

        // Murs physiques : face intérieure exactement à ±d, face du dessus du sol à y = -d
        float e = epPhys;
        float h = e * 0.5f;
        CreerMurPhysique(new Vector3(0, -d - h, 0), new Vector3(cote + 2 * e, e, cote + 2 * e), "Sol");
        CreerMurPhysique(new Vector3(-d - h, 0, 0), new Vector3(e, cote, cote), "MurGauche");
        CreerMurPhysique(new Vector3(d + h, 0, 0), new Vector3(e, cote, cote), "MurDroit");
        CreerMurPhysique(new Vector3(0, 0, -d - h), new Vector3(cote, cote, e), "MurArriere");
        CreerMurPhysique(new Vector3(0, 0, d + h), new Vector3(cote, cote, e), "MurAvant");

        // Rendu : mêmes faces intérieures que la physique
        float r = epRendu;
        float rh = r * 0.5f;
        var rendu = RenduSolide.Obtenir();
        rendu.AjouterCubeFixe(new Vector3(0, -d - 0.25f, 0), new Vector3(cote + 2, 0.5f, cote + 2), new Color(0.7f, 0.7f, 0.7f));
        rendu.AjouterCubeFixe(new Vector3(-d - rh, 0, 0), new Vector3(r, cote, cote), new Color(0.6f, 0.6f, 0.6f));
        rendu.AjouterCubeFixe(new Vector3(d + rh, 0, 0), new Vector3(r, cote, cote), new Color(0.6f, 0.6f, 0.6f));
        rendu.AjouterCubeFixe(new Vector3(0, 0, -d - rh), new Vector3(cote, cote, r), new Color(0.6f, 0.6f, 0.6f));
        rendu.AjouterCubeFixe(new Vector3(0, 0, d + rh), new Vector3(cote, cote, r), new Color(0.6f, 0.6f, 0.6f));

        if (nbParticulesEau > 0)
            rendu.AjouterParticules(eau.systeme, eau.h * 0.22f, new Color(0.2f, 0.6f, 1f));

        var couleurs = new Color[] { new Color(1f, 0.4f, 0.4f), new Color(1f, 0.9f, 0.3f), new Color(0.4f, 0.9f, 0.4f) };
        for (int i = 0; i < eau.couplage.solides.Count; i++)
            rendu.AjouterRigidbody(eau.couplage.solides[i], couleurs[i % couleurs.Length]);

        if (placerCameraAutomatiquement) PlacerCamera();

        Debug.Log($"[MONDE] {monde.corps.Count} corps enregistrés");
    }

    void PlacerCamera()
    {
        var cam = Camera.main;
        if (cam == null) return;
        cam.transform.position = new Vector3(10f, 8f, -10f);
        cam.transform.LookAt(Vector3.zero);
        cam.fieldOfView = 60f;
    }

    CustomRigidbody CreerMurPhysique(Vector3 centre, Vector3 taille, string nom)
    {
        GameObject go = new GameObject(nom);
        go.AddComponent<CustomCube>();
        CustomRigidbody rb = go.AddComponent<CustomRigidbody>();

        rb.mass = 100000f;
        rb.hasGravity = false;
        rb.estStatique = true;   // ★ invMass = 0, jamais intégré, jamais déplacé
        rb.initialPosition = new CustomVector3(centre.x, centre.y, centre.z);

        var cube = go.GetComponent<CustomCube>();
        float hx = taille.x * 0.5f, hy = taille.y * 0.5f, hz = taille.z * 0.5f;
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

        Debug.Log($"[MUR {nom}] mass={rb.mass}, invMass={rb.invMass:F6}, forme={rb.forme}");

        monde.EnregistrerCorps(rb);
        return rb;
    }

    void OnDrawGizmos() { }
}