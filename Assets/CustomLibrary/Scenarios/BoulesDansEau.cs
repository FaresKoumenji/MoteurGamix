using System.Collections.Generic;
using UnityEngine;

public class BoulesDansEau : MonoBehaviour
{
    [Header("Récipient")]
    public float cote = 6f;
    public int nbParticulesEau = 0;
    [Tooltip("Si coché, le nombre de particules est calculé pour que l'eau soit continue (voir espacementEau)")]
    public bool nbParticulesAuto = true;
    [Tooltip("Distance entre particules à l'état initial. Doit rester proche de hLissage (<= 0.8 x h), sinon l'eau ne forme pas un fluide continu")]
    public float espacementEau = 0.28f;
    [Tooltip("Hauteur d'eau = cote x cette fraction")]
    [Range(0.2f, 0.9f)] public float hauteurRemplissage = 0.55f;
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

    [Header("Affichage de l'eau")]
    public bool afficherEau = true;
    public Color couleurEau = new Color(0.15f, 0.4f, 0.9f);
    [Tooltip("Couleur des particules en contact avec une boule")]
    public Color couleurContact = new Color(0.7f, 0.95f, 1f);
    [Tooltip("Murs du récipient translucides pour voir l'eau et les boules")]
    public bool mursTransparents = true;

    [Header("Caméra")]
    public bool placerCameraAutomatiquement = true;

    // Épaisseur des murs PHYSIQUES (épais : évite que le centre d'une boule se retrouve dans le mur)
    const float epPhys = 2f;
    // Épaisseur des murs affichés
    const float epRendu = 0.2f;

    private PhysicsWorld monde;
    private Fluide eau;
    private float d;
    private MesureProfondeur mesure;

    void Start()
    {
        monde = PhysicsWorld.Obtenir();
        d = cote * 0.5f;

        if (nbParticulesEau < 0) nbParticulesEau = 0;
        if (nbParticulesAuto)
        {
            nbParticulesEau = Fluide.NbParticulesPourEspacement(cote, hauteurRemplissage, espacementEau);
            Debug.Log($"[EAU] {nbParticulesEau} particules");
        }

        var fond = new ContraintePlanFluide(new CustomVector3(0f, 1f, 0f), new CustomVector3(0f, -d, 0f));
        var murGauche = new ContraintePlanFluide(new CustomVector3(1f, 0f, 0f), new CustomVector3(-d, 0f, 0f));
        var murDroite = new ContraintePlanFluide(new CustomVector3(-1f, 0f, 0f), new CustomVector3(d, 0f, 0f));
        var murArriere = new ContraintePlanFluide(new CustomVector3(0f, 0f, 1f), new CustomVector3(0f, 0f, -d));
        var murAvant = new ContraintePlanFluide(new CustomVector3(0f, 0f, -1f), new CustomVector3(0f, 0f, d));

        CustomVector3 origine = new CustomVector3(-d, -d, -d);
        CustomVector3 taille = new CustomVector3(cote, cote * hauteurRemplissage, cote);

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
            float masse = CouplageFluideSolide.MasseSphere(rho, rayonBoule);

            Debug.Log($"[BOULE {i}] rho={rho}, rayon={rayonBoule}, masse={masse:F2}");

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
        Mur(rendu, new Vector3(-d - rh, 0, 0), new Vector3(r, cote, cote));
        Mur(rendu, new Vector3(d + rh, 0, 0), new Vector3(r, cote, cote));
        Mur(rendu, new Vector3(0, 0, -d - rh), new Vector3(cote, cote, r));
        Mur(rendu, new Vector3(0, 0, d + rh), new Vector3(cote, cote, r));

        if (afficherEau && nbParticulesEau > 0)
        {
            var renduEau = new GameObject("RenduEau").AddComponent<RenduEauInteractif>();
            renduEau.Init(eau.systeme, eau.couplage.solides, eau.h, couleurEau, couleurContact);
        }

        var couleurs = new Color[] { new Color(1f, 0.4f, 0.4f), new Color(1f, 0.9f, 0.3f), new Color(0.4f, 0.9f, 0.4f) };
        for (int i = 0; i < eau.couplage.solides.Count; i++)
            rendu.AjouterRigidbody(eau.couplage.solides[i], couleurs[i % couleurs.Length]);

        // Mesure de profondeur : niveau d'eau initial = fond + hauteur de remplissage (cote * 0.55)
        float niveauEau = Fluide.NiveauSurface(cote, hauteurRemplissage);

        var noms = new string[eau.couplage.solides.Count];
        for (int i = 0; i < noms.Length; i++)
            noms[i] = "Boule " + i + " (rho " + (i < densites.Length ? densites[i] : 0f) + ")";
        mesure = new MesureProfondeur(niveauEau, eau.couplage.solides, noms, couleurs, -d, d);
        mesure.eau = eau.systeme;   // la surface affichée suit l'eau réelle


        if (placerCameraAutomatiquement) PlacerCamera();

        Debug.Log($"[MONDE] {monde.corps.Count} corps enregistrés");
    }

    void Update()
    {
        if (mesure != null) mesure.Mettre(Time.deltaTime);
    }

    void OnGUI()
    {
        if (mesure != null) mesure.DessinerGUI();
    }

    void Mur(RenduSolide rendu, Vector3 centre, Vector3 taille)
    {
        Color c = new Color(0.6f, 0.6f, 0.6f);
        if (mursTransparents) rendu.AjouterCubeFixeTransparent(centre, taille, c, 0.25f);
        else rendu.AjouterCubeFixe(centre, taille, c);
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

    void OnDrawGizmos()
    {
        if (mesure != null) mesure.DessinerGizmos(d);
    }
}