using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pile de corps rigides (sphères) dans un bac. UN SEUL SCRIPT, aucune modification de la librairie.
/// Réglages : 2 sous-pas, 2 itérations de contacts par sous-pas, budget de 4 ms par frame.
///
/// Utilisation : scène vide -> GameObject vide -> ajouter ce composant -> Play.
/// Le script crée tout seul : sol, murs, sphères, rendu, caméra, lumière.
/// Il désactive le stepping automatique de PhysicsWorld et fait sa propre boucle
/// (intégration -> volumes -> phase large SAP -> phase étroite -> N passes de réponse).
/// </summary>
public class PileRigide : MonoBehaviour
{
    [Header("Solveur")]
    public int sousPas = 2;
    public int iterations = 2;
    public float budgetMs = 4f;
    public float restitution = 0.1f;
    public float frottement = 0.5f;

    [Header("Pile")]
    public int nx = 6;
    public int ny = 6;
    public int nz = 6;
    public float rayon = 0.25f;
    public float masse = 1f;
    public float hauteurDepart = 0.8f;
    public float jitter = 0.12f;          // fraction du rayon, casse la symétrie
    public int graine = 12345;

    [Header("Bac")]
    public float demiLargeur = 2f;
    public float hauteurMurs = 2.5f;
    public float epaisseurMur = 0.3f;
    public float epaisseurSol = 0.5f;

    PhysicsWorld monde;
    readonly List<CustomRigidbody> corps = new List<CustomRigidbody>();
    readonly List<CustomRigidbody> dynamiques = new List<CustomRigidbody>();
    readonly List<(int i, int j)> paires = new List<(int, int)>(1024);
    readonly List<Contact> contacts = new List<Contact>(1024);
    readonly List<Contact> contactsSansPenetration = new List<Contact>(1024);
    readonly IPhaseLarge phaseLarge = new PhaseLargeSAP();

    readonly System.Diagnostics.Stopwatch chrono = new System.Diagnostics.Stopwatch();
    float msMoyen;

    void Start()
    {
        monde = PhysicsWorld.Obtenir();
        monde.enabled = false;            // on fait le pas de physique nous-mêmes
        monde.afficherStats = false;

        CreerBac();
        CreerPile();
        PreparerCameraEtLumiere();

        Debug.Log($"[PileRigide] {dynamiques.Count} sphères, sous-pas={sousPas}, itérations={iterations}, budget={budgetMs} ms");
    }

    // ---------------- PAS DE PHYSIQUE ----------------
    void FixedUpdate()
    {
        chrono.Reset(); chrono.Start();

        int nbSousPas = Mathf.Max(1, sousPas);
        int nbIterations = Mathf.Max(1, iterations);
        float h = Time.fixedDeltaTime / nbSousPas;

        for (int s = 0; s < nbSousPas; s++)
        {
            // 1) Intégration + volumes (les corps statiques ne bougent pas)
            for (int k = 0; k < dynamiques.Count; k++)
            {
                dynamiques[k].Integrer(h);
                dynamiques[k].MettreAJourVolume();
            }

            // 2) Phase large
            paires.Clear();
            phaseLarge.TrouverPaires(corps, paires);

            // 3) Phase étroite
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

            // Copie à profondeur nulle : les passes suivantes ne repoussent plus les positions
            contactsSansPenetration.Clear();
            for (int k = 0; k < contacts.Count; k++)
            {
                Contact c = contacts[k];
                c.profondeur = 0f;
                contactsSansPenetration.Add(c);
            }

            // 4) Réponse : passe 1 avec séparation, passes suivantes en impulsions seulement
            ReponseCollision.ResoudreContacts(contacts, corps, h, restitution, frottement);
            for (int it = 1; it < nbIterations; it++)
                ReponseCollision.ResoudreContacts(contactsSansPenetration, corps, h, restitution, frottement);
        }

        for (int k = 0; k < dynamiques.Count; k++) dynamiques[k].SynchroVisuelle();

        chrono.Stop();
        float ms = (float)chrono.Elapsed.TotalMilliseconds;
        msMoyen = msMoyen <= 0f ? ms : Mathf.Lerp(msMoyen, ms, 0.05f);
    }

    // ---------------- CONSTRUCTION DE LA SCÈNE ----------------
    void CreerBac()
    {
        var rendu = RenduSolide.Obtenir();

        float L = demiLargeur + epaisseurMur;
        float ht = epaisseurMur * 0.5f;
        float hh = hauteurMurs * 0.5f;
        float hs = epaisseurSol * 0.5f;

        // Sol (dessus à y = 0)
        CreerBoiteStatique("Sol", new CustomVector3(0f, -hs, 0f), new CustomVector3(L, hs, L));
        rendu.AjouterCubeFixe(new Vector3(0f, -hs, 0f),
                              new Vector3(2f * L, epaisseurSol, 2f * L),
                              new Color(0.7f, 0.7f, 0.7f));

        Color murs = new Color(0.6f, 0.75f, 0.9f);
        for (int sgn = -1; sgn <= 1; sgn += 2)
        {
            float c = sgn * (demiLargeur + ht);

            CreerBoiteStatique("MurX" + sgn, new CustomVector3(c, hh, 0f), new CustomVector3(ht, hh, L));
            rendu.AjouterCubeFixeTransparent(new Vector3(c, hh, 0f),
                new Vector3(epaisseurMur, hauteurMurs, 2f * L), murs, 0.25f);

            CreerBoiteStatique("MurZ" + sgn, new CustomVector3(0f, hh, c), new CustomVector3(demiLargeur, hh, ht));
            rendu.AjouterCubeFixeTransparent(new Vector3(0f, hh, c),
                new Vector3(2f * demiLargeur, hauteurMurs, epaisseurMur), murs, 0.25f);
        }
    }

    void CreerBoiteStatique(string nom, CustomVector3 centre, CustomVector3 demi)
    {
        GameObject go = new GameObject(nom);
        var cube = go.AddComponent<CustomCube>();
        CustomRigidbody rb = go.AddComponent<CustomRigidbody>();

        rb.mass = 100000f;
        rb.hasGravity = false;
        rb.estStatique = true;
        rb.showStateOnScreen = false;
        rb.initialPosition = centre;

        float hx = demi.x, hy = demi.y, hz = demi.z;
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
        corps.Add(rb);                    // statique : jamais intégré, volume calculé une fois
    }

    void CreerPile()
    {
        var rendu = RenduSolide.Obtenir();
        var rng = new System.Random(graine);

        float pas = rayon * 2f * 1.12f;
        float jit = rayon * jitter;
        float x0 = -(nx - 1) * pas * 0.5f;
        float z0 = -(nz - 1) * pas * 0.5f;
        int total = nx * ny * nz;
        int n = 0;

        for (int iy = 0; iy < ny; iy++)
            for (int iz = 0; iz < nz; iz++)
                for (int ix = 0; ix < nx; ix++)
                {
                    float jx = ((float)rng.NextDouble() * 2f - 1f) * jit;
                    float jy = ((float)rng.NextDouble() * 2f - 1f) * jit;
                    float jz = ((float)rng.NextDouble() * 2f - 1f) * jit;

                    var pos = new CustomVector3(
                        x0 + ix * pas + jx,
                        hauteurDepart + rayon + iy * pas + jy,
                        z0 + iz * pas + jz);

                    CustomRigidbody rb = CreerSphere("Sphere_" + n, pos);
                    rendu.AjouterRigidbody(rb, Color.HSVToRGB(0.02f + 0.9f * n / Mathf.Max(1, total), 0.65f, 0.95f));
                    n++;
                }
    }

    CustomRigidbody CreerSphere(string nom, CustomVector3 pos)
    {
        GameObject go = new GameObject(nom);
        var cube = go.AddComponent<CustomCube>();
        CustomRigidbody rb = go.AddComponent<CustomRigidbody>();

        // Le volume englobant est calculé à partir des sommets du cube : on les aligne sur le rayon
        float r = rayon;
        cube.vertexA = new Vector3(-r, -r, -r);
        cube.vertexB = new Vector3(r, -r, -r);
        cube.vertexC = new Vector3(r, -r, r);
        cube.vertexD = new Vector3(-r, -r, r);
        cube.vertexE = new Vector3(-r, r, -r);
        cube.vertexF = new Vector3(r, r, -r);
        cube.vertexG = new Vector3(r, r, r);
        cube.vertexH = new Vector3(-r, r, r);

        rb.mass = masse;
        rb.hasGravity = true;
        rb.drag = 0.1f;
        rb.angularDrag = 0.3f;
        rb.showStateOnScreen = false;
        rb.initialPosition = pos;

        rb.Reinitialiser();
        rb.forme = FormeRigide.Sphere;
        rb.rayon = r;

        corps.Add(rb);
        dynamiques.Add(rb);
        return rb;
    }

    void PreparerCameraEtLumiere()
    {
        var cam = Camera.main;
        if (cam == null)
        {
            var goCam = new GameObject("Main Camera");
            goCam.tag = "MainCamera";
            cam = goCam.AddComponent<Camera>();
            goCam.AddComponent<AudioListener>();
        }
        cam.transform.position = new Vector3(0f, 4.5f, -8f);
        cam.transform.LookAt(new Vector3(0f, 1.2f, 0f));
        cam.fieldOfView = 50f;

        if (FindFirstObjectByType<Light>() == null)
        {
            var goLum = new GameObject("Directional Light");
            var lum = goLum.AddComponent<Light>();
            lum.type = LightType.Directional;
            goLum.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }
    }

    // ---------------- AFFICHAGE ----------------
    void OnGUI()
    {
        bool ok = msMoyen <= budgetMs;
        Color ancienne = GUI.contentColor;
        GUI.contentColor = ok ? new Color(0.4f, 1f, 0.4f) : new Color(1f, 0.4f, 0.4f);
        GUI.Box(new Rect(10, 10, 480, 90),
            "PILE RIGIDE\n" +
            $"Sphères : {dynamiques.Count}   Paires : {paires.Count}   Contacts : {contacts.Count}\n" +
            $"Sous-pas : {Mathf.Max(1, sousPas)}   Itérations : {Mathf.Max(1, iterations)}\n" +
            $"Pas de physique (moy.) : {msMoyen:F2} ms   Budget : {budgetMs:F1} ms   " + (ok ? "OK" : "DÉPASSÉ"));
        GUI.contentColor = ancienne;
    }
}
