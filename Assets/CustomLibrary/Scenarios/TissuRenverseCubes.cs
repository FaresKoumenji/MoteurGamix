using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Scénario : un tissu suspendu frappe une pile de 3 cubes en particules (shape matching).
/// Tout se passe dans le même SystemeParticules :
///   - le tissu est un groupe de particules reliées par des ressorts (ForceRessort) ;
///   - chaque cube est un réseau 3x3x3 (27 particules) maintenu rigide par ShapeMatching ;
///   - ContrainteParticuleParticule gère les collisions tissu <-> cubes et cube <-> cube ;
///   - la gravité et un sol plan complètent.
/// Ce fichier ne contient aucune physique : il instancie et branche les modules.
/// </summary>
public class TissuRenverseCubes : MonoBehaviour
{
    [Header("Tissu")]
    public int nxTissu = 14;
    public int nyTissu = 16;
    public float largeurTissu = 1.8f;
    public float hauteurTissu = 2.2f;
    public float masseTissu = 3f;
    public float raideurTissu = 800f;
    [Tooltip("Limité à 1.2 (stabilité, voir TissuPendant)")]
    public float amortissementTissu = 0.5f;
    public float seuilRuptureTissu = 0.5f;
    public CustomVector3 origineTissu = new CustomVector3(-0.9f, 3.5f, 0f);
    [Tooltip("NÉGATIF = le tissu part du côté opposé aux cubes et vient les frapper en balançant")]
    public float angleInitialTissu = -40f;
    public float trainee = 0.0005f;

    [Header("Cubes")]
    public int nbCubes = 3;
    public float tailleCube = 0.6f;
    public float masseCube = 1f;
    public CustomVector3 baseCubes = new CustomVector3(1.4f, 0f, 0f);
    public float raideurShapeMatching = 0.9f;
    [Tooltip("Doit être > moitié de l'espacement du réseau (tailleCube/2/2 = 0.15), sinon le tissu se faufile")]
    public float rayonContact = 0.2f;

    [Header("Intégration")]
    public int sousPas = 16;

    [Header("Affichage")]
    public bool dessinerRessorts = true;
    public bool placerCameraAutomatiquement = true;

    private const float AMORTISSEMENT_MAX = 1.2f;
    private const int PAR_ARETE = 3;                       // 3x3x3
    private const int PARTICULES_PAR_CUBE = PAR_ARETE * PAR_ARETE * PAR_ARETE; // 27

    private PhysicsWorld monde;
    private SystemeParticules systeme;
    private ForceRessort ressortsTissu;
    private ContrainteShapeMatching shapeMatching;
    private List<List<int>> groupesCubes = new List<List<int>>();

    void OnValidate()
    {
        amortissementTissu = Mathf.Clamp(amortissementTissu, 0f, AMORTISSEMENT_MAX);
        sousPas = Mathf.Max(1, sousPas);
        trainee = Mathf.Max(0f, trainee);
    }

    void Start()
    {
        monde = PhysicsWorld.Obtenir();
        amortissementTissu = Mathf.Min(amortissementTissu, AMORTISSEMENT_MAX);

        ConstruireSysteme();
        AjouterTissu();
        AjouterCubes();
        AjouterContraintes();

        // Enregistrement APRÈS la construction complète (comme TissuPendant)
        monde.EnregistrerSysteme(systeme);

        var rendu = RenduSolide.Obtenir();
        rendu.AjouterCubeFixe(new Vector3(0, -0.25f, 0),
                              new Vector3(20f, 0.5f, 20f),
                              new Color(0.7f, 0.7f, 0.7f));
        rendu.AjouterParticules(systeme, 0.05f, new Color(0.2f, 0.5f, 0.9f));

        if (placerCameraAutomatiquement) PlacerCamera();

        Debug.Log($"[TissuRenverseCubes] {systeme.N} particules, {groupesCubes.Count} cubes");
    }

    void ConstruireSysteme()
    {
        int particulesTissu = nxTissu * nyTissu;
        int particulesTotal = particulesTissu + nbCubes * PARTICULES_PAR_CUBE;

        systeme = new SystemeParticules(particulesTotal, "Tissu + Cubes");
        systeme.mode = ModeIntegration.EulerSemiImplicite;
        systeme.sousPas = sousPas;
        systeme.vitesseMax = 20f;

        for (int i = 0; i < particulesTotal; i++)
        {
            systeme.p[i].x = CustomVector3.zero;
            systeme.p[i].v = CustomVector3.zero;
            systeme.p[i].m = 1f;
            systeme.p[i].invmass = 1f;
            systeme.p[i].phase = 0;
        }
    }

    void AjouterTissu()
    {
        int n = nxTissu * nyTissu;
        float m = masseTissu / n;

        for (int j = 0; j < nyTissu; j++)
            for (int i = 0; i < nxTissu; i++)
            {
                int idx = j * nxTissu + i;
                systeme.p[idx].x = new CustomVector3(
                    origineTissu.x + i * largeurTissu / (nxTissu - 1),
                    origineTissu.y - j * hauteurTissu / (nyTissu - 1),
                    origineTissu.z);
                systeme.p[idx].m = m;
                systeme.p[idx].invmass = 1f / m;
                systeme.p[idx].phase = 0;
            }

        // 2 particules centrales de la rangée du haut accrochées
        int centre = nxTissu / 2;
        systeme.Fixer(centre - 1);
        systeme.Fixer(centre);

        // Inclinaison initiale autour du point d'accroche (les distances sont conservées)
        CustomVector3 pivot = (systeme.p[centre - 1].x + systeme.p[centre].x) * 0.5f;
        float ang = angleInitialTissu * Mathf.Deg2Rad;
        float cs = Mathf.Cos(ang), sn = Mathf.Sin(ang);
        for (int i = 0; i < n; i++)
        {
            if (systeme.p[i].invmass <= 0f) continue;
            float dx = systeme.p[i].x.x - pivot.x;
            float dy = systeme.p[i].x.y - pivot.y;
            systeme.p[i].x = new CustomVector3(
                pivot.x + cs * dx - sn * dy,
                pivot.y + sn * dx + cs * dy,
                systeme.p[i].x.z);
        }

        // Ressorts (ajoutés après la rotation : longueurs de repos correctes)
        ressortsTissu = new ForceRessort();
        ressortsTissu.seuilRupture = seuilRuptureTissu;

        for (int j = 0; j < nyTissu; j++)
            for (int i = 0; i < nxTissu; i++)
            {
                int a2 = j * nxTissu + i;

                if (i + 1 < nxTissu)
                    ressortsTissu.Ajouter(systeme, a2, a2 + 1, raideurTissu, amortissementTissu, ForceRessort.Structurel, true);
                if (j + 1 < nyTissu)
                    ressortsTissu.Ajouter(systeme, a2, a2 + nxTissu, raideurTissu, amortissementTissu, ForceRessort.Structurel, true);

                if (i + 1 < nxTissu && j + 1 < nyTissu)
                {
                    ressortsTissu.Ajouter(systeme, a2, a2 + nxTissu + 1, raideurTissu * 0.5f, amortissementTissu, ForceRessort.Cisaillement, true);
                    ressortsTissu.Ajouter(systeme, a2 + 1, a2 + nxTissu, raideurTissu * 0.5f, amortissementTissu, ForceRessort.Cisaillement, true);
                }
            }

        systeme.forces.Add(ressortsTissu);
    }

    void AjouterCubes()
    {
        int offset = nxTissu * nyTissu;
        float h = tailleCube * 0.5f;
        float pas = tailleCube / (PAR_ARETE - 1);      // espacement du réseau
        float m = masseCube / PARTICULES_PAR_CUBE;

        // Écart entre cubes = rayonContact : évite que deux cubes empilés démarrent en contact exact
        float hauteurPas = tailleCube + rayonContact;

        for (int c = 0; c < nbCubes; c++)
        {
            float centreY = baseCubes.y + c * hauteurPas + h;
            List<int> groupe = new List<int>(PARTICULES_PAR_CUBE);

            for (int k = 0; k < PARTICULES_PAR_CUBE; k++)
            {
                int ix = k % PAR_ARETE;
                int iy = (k / PAR_ARETE) % PAR_ARETE;
                int iz = k / (PAR_ARETE * PAR_ARETE);

                int idx = offset + c * PARTICULES_PAR_CUBE + k;
                systeme.p[idx].x = new CustomVector3(
                    baseCubes.x - h + ix * pas,
                    centreY - h + iy * pas,
                    baseCubes.z - h + iz * pas);
                systeme.p[idx].m = m;
                systeme.p[idx].invmass = 1f / m;
                systeme.p[idx].phase = c + 1;          // phases 1, 2, 3 : un cube = une phase
                groupe.Add(idx);
            }
            groupesCubes.Add(groupe);
        }
    }

    void AjouterContraintes()
    {
        systeme.forces.Add(new ForceGravite(new CustomVector3(0f, -9.81f, 0f)));
        systeme.forces.Add(new ForceTrainee(trainee));

        shapeMatching = new ContrainteShapeMatching();
        shapeMatching.raideur = raideurShapeMatching;
        for (int c = 0; c < groupesCubes.Count; c++)
            shapeMatching.AjouterGroupe(groupesCubes[c]);
        systeme.contraintes.Add(shapeMatching);

        // Collisions particule-particule (tissu <-> cubes, cube <-> cube)
        systeme.contraintes.Add(new ContrainteParticuleParticule(rayonContact));

        // Sol
        systeme.contraintes.Add(new ContraintePlanFluide(new CustomVector3(0f, 1f, 0f), CustomVector3.zero));
    }

    void PlacerCamera()
    {
        var cam = Camera.main;
        if (cam == null) return;
        cam.transform.position = new Vector3(0.5f, 2.5f, -7f);
        cam.transform.LookAt(new Vector3(0.5f, 1.5f, 0f));
        cam.fieldOfView = 60f;
    }

    // Gizmos (bouton "Gizmos" de la vue Game) : ressorts du tissu + arêtes des cubes
    void OnDrawGizmos()
    {
        if (systeme == null) return;

        if (dessinerRessorts && ressortsTissu != null)
        {
            Gizmos.color = new Color(0.3f, 0.9f, 0.4f);
            var liste = ressortsTissu.ressorts;
            for (int q = 0; q < liste.Count; q++)
            {
                Ressort r = liste[q];
                if (!r.actif || r.type != ForceRessort.Structurel) continue;
                Gizmos.DrawLine(systeme.p[r.i].x.ToUnity(), systeme.p[r.j].x.ToUnity());
            }
        }

        // Cubes : relier les 8 coins du réseau 3x3x3 (indices ix, iy, iz dans {0, 2})
        Gizmos.color = new Color(1f, 0.6f, 0.2f);
        int last = PAR_ARETE - 1;
        for (int c = 0; c < groupesCubes.Count; c++)
        {
            List<int> g = groupesCubes[c];
            for (int ix = 0; ix <= last; ix += last)
                for (int iy = 0; iy <= last; iy += last)
                    for (int iz = 0; iz <= last; iz += last)
                    {
                        Vector3 a = systeme.p[g[Coin(ix, iy, iz)]].x.ToUnity();
                        if (ix == 0) Gizmos.DrawLine(a, systeme.p[g[Coin(last, iy, iz)]].x.ToUnity());
                        if (iy == 0) Gizmos.DrawLine(a, systeme.p[g[Coin(ix, last, iz)]].x.ToUnity());
                        if (iz == 0) Gizmos.DrawLine(a, systeme.p[g[Coin(ix, iy, last)]].x.ToUnity());
                    }
        }
    }

    static int Coin(int ix, int iy, int iz)
    {
        return ix + PAR_ARETE * iy + PAR_ARETE * PAR_ARETE * iz;
    }
}