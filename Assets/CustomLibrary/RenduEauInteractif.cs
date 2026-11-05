using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Rendu des particules d'eau pour montrer l'interaction avec les solides (aucune physique ici).
///  - chaque particule est dessinée (instancing GPU, aucune allocation par image) ;
///  - les particules en contact avec une boule (dans la zone d'action du couplage) sont en couleur claire ;
///  - la position AFFICHÉE est légèrement lissée dans le temps pour supprimer le scintillement visuel
///    (la simulation n'est pas modifiée).
/// </summary>
public class RenduEauInteractif : MonoBehaviour
{
    [Tooltip("Rayon affiché = facteur x espacement des particules")]
    public float facteurRayon = 0.45f;
    [Tooltip("Marge autour d'une boule où une particule est considérée en contact (x h)")]
    public float margeContact = 0.5f;
    [Tooltip("Lissage de l'affichage (1/s). Plus grand = plus fidèle à la simulation, plus petit = plus calme")]
    public float lissage = 25f;

    private SystemeParticules eau;
    private IList<CustomRigidbody> solides;
    private float h;
    private float rayonRendu;
    private Mesh mesh;
    private Material matNormal, matContact;
    private Vector3[] affiche;
    private readonly Matrix4x4[] lotNormal = new Matrix4x4[1023];
    private readonly Matrix4x4[] lotContact = new Matrix4x4[1023];
    private int nbContact;
    private int nbInvalides;
    private float yMin, yMax;

    public void Init(SystemeParticules eau, IList<CustomRigidbody> solides, float h, Color normal, Color contact)
    {
        this.eau = eau;
        this.solides = solides;
        this.h = h;
        float espacement = h / 2.2f;                 // Fluide impose h = 2.2 x espacement
        rayonRendu = espacement * facteurRayon;

        mesh = RenduSolide.CreerSphere(1f, 10, 7);
        matNormal = CreerMateriel(normal);
        matContact = CreerMateriel(contact);

        affiche = new Vector3[eau.N];
        for (int i = 0; i < eau.N; i++) affiche[i] = eau.p[i].x.ToUnity();
    }

    static Shader TrouverShader()
    {
        var srp = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
        if (srp != null)
        {
            var s = Shader.Find("Universal Render Pipeline/Lit");
            if (s != null) return s;
            s = Shader.Find("HDRP/Lit");
            if (s != null) return s;
        }
        var std = Shader.Find("Standard");
        if (std != null) return std;
        return Shader.Find("Sprites/Default");
    }

    static Material CreerMateriel(Color c)
    {
        var m = new Material(TrouverShader());
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color")) m.color = c;
        m.enableInstancing = true;
        return m;
    }

    void LateUpdate()
    {
        if (eau == null || mesh == null) return;

        int ns = solides != null ? solides.Count : 0;
        Vector3[] cs = new Vector3[ns];
        float[] rs = new float[ns];
        for (int k = 0; k < ns; k++)
        {
            cs[k] = solides[k].CentreMonde.ToUnity();
            rs[k] = solides[k].rayon + margeContact * h;
        }

        float a = 1f - Mathf.Exp(-lissage * Time.deltaTime);
        Vector3 echelle = Vector3.one * rayonRendu;
        int nN = 0, nC = 0;
        nbContact = 0;
        nbInvalides = 0;
        yMin = float.MaxValue; yMax = float.MinValue;

        for (int i = 0; i < eau.N; i++)
        {
            Vector3 reel = eau.p[i].x.ToUnity();
            if (float.IsNaN(reel.x) || float.IsNaN(reel.y) || float.IsNaN(reel.z) ||
                float.IsInfinity(reel.x) || float.IsInfinity(reel.y) || float.IsInfinity(reel.z))
            {
                nbInvalides++;      // simulation divergée : on n'affiche pas cette particule
                continue;
            }
            if (reel.y < yMin) yMin = reel.y;
            if (reel.y > yMax) yMax = reel.y;
            affiche[i] = Vector3.Lerp(affiche[i], reel, a);
            Vector3 pos = affiche[i];

            bool contact = false;
            for (int k = 0; k < ns; k++)
                if ((pos - cs[k]).sqrMagnitude < rs[k] * rs[k]) { contact = true; break; }

            if (contact)
            {
                lotContact[nC++] = Matrix4x4.TRS(pos, Quaternion.identity, echelle);
                nbContact++;
                if (nC == 1023) { Graphics.DrawMeshInstanced(mesh, 0, matContact, lotContact, nC); nC = 0; }
            }
            else
            {
                lotNormal[nN++] = Matrix4x4.TRS(pos, Quaternion.identity, echelle);
                if (nN == 1023) { Graphics.DrawMeshInstanced(mesh, 0, matNormal, lotNormal, nN); nN = 0; }
            }
        }
        if (nN > 0) Graphics.DrawMeshInstanced(mesh, 0, matNormal, lotNormal, nN);
        if (nC > 0) Graphics.DrawMeshInstanced(mesh, 0, matContact, lotContact, nC);
    }

    void OnGUI()
    {
        if (eau == null) return;
        GUI.Box(new Rect(10, Screen.height - 70, 380, 60), "");
        GUI.Label(new Rect(18, Screen.height - 66, 370, 54),
                  "Particules : " + eau.N + "  (invalides : " + nbInvalides + ")   hauteur y : " +
                  yMin.ToString("F2") + " à " + yMax.ToString("F2") +
                  "\nEn contact avec une boule : " + nbContact + "  (clair = interaction eau <-> solide)");
    }
}