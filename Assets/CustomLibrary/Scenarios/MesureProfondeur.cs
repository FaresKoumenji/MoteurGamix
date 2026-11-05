using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Mesure et affiche la position/profondeur de corps rigides dans l'eau (lecture seule, aucune physique).
/// Profondeur = niveauEau - y du point le plus bas du corps (centre - rayon).
/// Affichages (tous dans OnGUI) :
///   - tableau en haut à gauche (profondeur actuelle / maximale)
///   - jauge verticale à droite : surface, fond, une colonne par boule avec son marqueur et son max
///   - étiquette flottante au-dessus de chaque boule dans la vue 3D
/// </summary>
public class MesureProfondeur
{
    public float niveauEau;
    public float yFond;
    public float yHaut;

    private readonly IList<CustomRigidbody> corps;
    private readonly string[] noms;
    private readonly Color[] couleurs;
    private readonly float[] maxProf;

    private GUIStyle styleCentre, styleGauche;

    // Surface réelle estimée à partir des particules d'eau (si fournies)
    public SystemeParticules eau;
    public bool surfaceAutomatique = true;
    private float[] tampon;
    private float chrono;

    public MesureProfondeur(float niveauEau, IList<CustomRigidbody> corps, string[] noms, Color[] couleurs,
                            float yFond, float yHaut)
    {
        this.niveauEau = niveauEau;
        this.corps = corps;
        this.noms = noms;
        this.couleurs = couleurs;
        this.yFond = yFond;
        this.yHaut = yHaut;
        maxProf = new float[corps.Count];
    }

    Color Couleur(int i)
    {
        return (couleurs != null && couleurs.Length > 0) ? couleurs[i % couleurs.Length] : Color.white;
    }

    /// <summary>Profondeur actuelle du point le plus bas du corps (négatif = au-dessus de l'eau).</summary>
    public float Profondeur(int i)
    {
        CustomRigidbody rb = corps[i];
        return niveauEau - (rb.state.x.y - rb.rayon);
    }

    public float ProfondeurMax(int i) { return maxProf[i]; }

    public void Reinitialiser()
    {
        for (int i = 0; i < maxProf.Length; i++) maxProf[i] = 0f;
    }

    public void Mettre(float dt)
    {
        EstimerSurface(dt);
        for (int i = 0; i < corps.Count; i++)
        {
            float p = Profondeur(i);
            if (p > maxProf[i]) maxProf[i] = p;
        }
    }

    /// <summary>Surface = 90e centile de la hauteur des particules d'eau (ignore les éclaboussures).</summary>
    void EstimerSurface(float dt)
    {
        if (!surfaceAutomatique || eau == null || eau.N < 50) return;
        chrono += dt;
        if (chrono < 0.25f) return;
        chrono = 0f;

        if (tampon == null || tampon.Length != eau.N) tampon = new float[eau.N];
        for (int i = 0; i < eau.N; i++) tampon[i] = eau.p[i].x.y;
        System.Array.Sort(tampon);
        float cible = tampon[(int)(0.9f * (eau.N - 1))];
        niveauEau = Mathf.Lerp(niveauEau, cible, 0.3f);
    }

    // ------------------------------------------------------------------ GUI

    static void Rempl(Rect r, Color c)
    {
        Color ancien = GUI.color;
        GUI.color = c;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = ancien;
    }

    void PreparerStyles()
    {
        if (styleCentre != null) return;
        styleCentre = new GUIStyle(GUI.skin.label);
        styleCentre.alignment = TextAnchor.MiddleCenter;
        styleCentre.fontStyle = FontStyle.Bold;
        styleCentre.fontSize = 13;
        styleGauche = new GUIStyle(GUI.skin.label);
        styleGauche.alignment = TextAnchor.MiddleRight;
        styleGauche.fontSize = 11;
    }

    string Nom(int i) { return (noms != null && i < noms.Length) ? noms[i] : "Boule " + i; }

    string Texte(float p) { return p > 0f ? p.ToString("F2") + " m" : "hors de l'eau"; }

    public void DessinerGUI()
    {
        PreparerStyles();
        DessinerTableau();
        DessinerJauge();
        DessinerEtiquettes();
    }

    void DessinerTableau()
    {
        float h = 30f + corps.Count * 22f + 34f;
        GUI.Box(new Rect(10, 10, 380, h), "Profondeur (point le plus bas de la boule)");

        for (int i = 0; i < corps.Count; i++)
        {
            GUI.color = Couleur(i);
            GUI.Label(new Rect(20, 34 + i * 22, 370, 22),
                      Nom(i) + " :  actuelle " + Texte(Profondeur(i)) + "   |   max " + maxProf[i].ToString("F2") + " m");
        }
        GUI.color = Color.white;

        if (GUI.Button(new Rect(20, 34 + corps.Count * 22 + 2, 140, 24), "Remettre max à 0"))
            Reinitialiser();
    }

    void DessinerJauge()
    {
        int n = corps.Count;
        float colW = 44f;
        float largeur = 50f + n * colW;
        float hauteur = Mathf.Min(Screen.height - 120f, 480f);
        float gx = Screen.width - largeur - 20f;
        float gy = 60f;
        float x0 = gx + 50f;                      // début des colonnes

        float plage = Mathf.Max(0.001f, yHaut - yFond);
        System.Func<float, float> Y = y => gy + (yHaut - y) / plage * hauteur;

        GUI.Box(new Rect(gx - 6, gy - 34, largeur + 12, hauteur + 44), "Position");

        // Zone d'eau
        float ySurf = Mathf.Clamp(Y(niveauEau), gy, gy + hauteur);
        Rempl(new Rect(x0, ySurf, n * colW, gy + hauteur - ySurf), new Color(0.2f, 0.55f, 1f, 0.35f));
        // Cadre
        Rempl(new Rect(x0, gy, n * colW, 1), new Color(1, 1, 1, 0.4f));
        Rempl(new Rect(x0, gy + hauteur, n * colW, 2), new Color(1, 1, 1, 0.8f));
        // Surface
        Rempl(new Rect(x0 - 6, ySurf - 1, n * colW + 6, 3), new Color(0.5f, 0.85f, 1f, 1f));
        GUI.color = new Color(0.5f, 0.85f, 1f);
        GUI.Label(new Rect(gx - 8, ySurf - 10, 56, 20), "surface", styleGauche);

        // Graduation tous les mètres sous la surface
        GUI.color = Color.white;
        for (int m = 1; niveauEau - m >= yFond; m++)
        {
            float ym = Y(niveauEau - m);
            Rempl(new Rect(x0 - 5, ym, 5, 1), new Color(1, 1, 1, 0.8f));
            Rempl(new Rect(x0, ym, n * colW, 1), new Color(1, 1, 1, 0.12f));
            GUI.Label(new Rect(gx - 8, ym - 10, 56, 20), "-" + m + " m", styleGauche);
        }
        GUI.Label(new Rect(gx - 8, gy + hauteur - 10, 56, 20), "fond", styleGauche);

        // Une colonne par boule : trait = profondeur max, carré = position actuelle (centre)
        for (int i = 0; i < n; i++)
        {
            float cx = x0 + i * colW + colW * 0.5f;
            Color c = Couleur(i);

            float yMax = Mathf.Clamp(Y(niveauEau - maxProf[i] + corps[i].rayon), gy, gy + hauteur);
            Rempl(new Rect(cx - 17, yMax - 1, 34, 3), new Color(c.r, c.g, c.b, 0.9f));

            float yc = Mathf.Clamp(Y(corps[i].state.x.y), gy, gy + hauteur);
            Rempl(new Rect(cx - 11, yc - 11, 22, 22), Color.black);
            Rempl(new Rect(cx - 9, yc - 9, 18, 18), c);

            GUI.color = Color.black;
            GUI.Label(new Rect(cx - 11, yc - 11, 22, 22), i.ToString(), styleCentre);
            GUI.color = Color.white;

            // Nom en bas de colonne
            GUI.color = c;
            GUI.Label(new Rect(cx - 20, gy + hauteur + 2, 40, 20), "B" + i, styleCentre);
            GUI.color = Color.white;
        }
    }

    void DessinerEtiquettes()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        for (int i = 0; i < corps.Count; i++)
        {
            CustomRigidbody rb = corps[i];
            Vector3 pos = new Vector3(rb.state.x.x, rb.state.x.y + rb.rayon + 0.15f, rb.state.x.z);
            Vector3 sp = cam.WorldToScreenPoint(pos);
            if (sp.z < 0f) continue;

            float p = Profondeur(i);
            string txt = "B" + i + "  " + (p > 0f ? p.ToString("F2") + " m" : "hors de l'eau");
            Rect r = new Rect(sp.x - 60, Screen.height - sp.y - 22, 120, 22);

            Rempl(r, new Color(0f, 0f, 0f, 0.65f));
            Rempl(new Rect(r.x, r.y, 4, r.height), Couleur(i));
            GUI.color = Color.white;
            GUI.Label(r, txt, styleCentre);
        }
    }

    // --------------------------------------------------------------- Gizmos

    public void DessinerGizmos(float demiCote)
    {
        Gizmos.color = new Color(0.3f, 0.7f, 1f);
        Vector3 a = new Vector3(-demiCote, niveauEau, -demiCote);
        Vector3 b = new Vector3(demiCote, niveauEau, -demiCote);
        Vector3 c = new Vector3(demiCote, niveauEau, demiCote);
        Vector3 d = new Vector3(-demiCote, niveauEau, demiCote);
        Gizmos.DrawLine(a, b); Gizmos.DrawLine(b, c); Gizmos.DrawLine(c, d); Gizmos.DrawLine(d, a);

        if (corps == null) return;

        for (int i = 0; i < corps.Count; i++)
        {
            float y = niveauEau - maxProf[i];
            Gizmos.color = Couleur(i);
            float z = demiCote - 0.05f * (i + 1);
            Gizmos.DrawLine(new Vector3(-demiCote, y, z), new Vector3(demiCote, y, z));
        }
    }
}