using UnityEngine;
using System.Collections.Generic;

public class RenduSolide : MonoBehaviour
{
    public static RenduSolide Obtenir()
    {
        var go = GameObject.Find("RenduSolide");
        if (go == null) go = new GameObject("RenduSolide");
        var r = go.GetComponent<RenduSolide>();
        if (r == null) r = go.AddComponent<RenduSolide>();
        return r;
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

    static Material CreerMateriel(Color couleur, bool transparent = false)
    {
        var mat = new Material(TrouverShader());

        if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 0f);
        if (mat.HasProperty("_Mode")) mat.SetFloat("_Mode", 0f);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
        mat.SetInt("_ZWrite", 1);
        mat.renderQueue = -1;

        if (transparent)
        {
            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
            if (mat.HasProperty("_Mode")) mat.SetFloat("_Mode", 3f);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = 3000;
        }

        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", couleur);
        if (mat.HasProperty("_Color")) mat.color = couleur;
        mat.enableInstancing = true;
        return mat;
    }

    public void AjouterRigidbody(CustomRigidbody rb, Color couleur)
    {
        var go = new GameObject("Visuel_" + rb.name);
        go.transform.SetParent(transform, false);
        var mf = go.AddComponent<MeshFilter>();
        var mr = go.AddComponent<MeshRenderer>();
        mr.material = CreerMateriel(couleur);

        Mesh mesh = (rb.forme == FormeRigide.Sphere)
            ? CreerSphere(rb.rayon, 16, 12)
            : CreerCube(1f);
        mf.mesh = mesh;

        var suivi = go.AddComponent<SuiviRigidbody>();
        suivi.rb = rb;
    }

    public void AjouterParticules(SystemeParticules s, float rayonParticule, Color couleur)
    {
        var go = new GameObject("Visuel_Eau");
        go.transform.SetParent(transform, false);
        var mf = go.AddComponent<MeshFilter>();
        var mr = go.AddComponent<MeshRenderer>();
        var mat = CreerMateriel(couleur, false);
        mr.material = mat;

        var mesh = CreerSphere(rayonParticule, 10, 7);
        mf.mesh = mesh;

        var rendu = go.AddComponent<RenduParticules>();
        rendu.systeme = s;
        rendu.rayon = rayonParticule;
        rendu.mat = mat;
        rendu.mesh = mesh;
    }

    public void AjouterCubeFixe(Vector3 centre, Vector3 taille, Color couleur)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "Mur";
        go.transform.SetParent(transform, false);
        go.transform.position = centre;
        go.transform.localScale = taille;
        Destroy(go.GetComponent<Collider>());
        go.GetComponent<MeshRenderer>().material = CreerMateriel(couleur);
    }

    public void AjouterCubeFixeTransparent(Vector3 centre, Vector3 taille, Color couleur, float alpha)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "MurTransparent";
        go.transform.SetParent(transform, false);
        go.transform.position = centre;
        go.transform.localScale = taille;
        Destroy(go.GetComponent<Collider>());
        go.GetComponent<MeshRenderer>().material =
            CreerMateriel(new Color(couleur.r, couleur.g, couleur.b, alpha), true);
    }

    public static Mesh CreerSphere(float rayon, int segments, int rings)
    {
        var mesh = new Mesh();
        var verts = new List<Vector3>();
        var tris = new List<int>();

        for (int y = 0; y <= rings; y++)
        {
            float v = (float)y / rings;
            float phi = v * Mathf.PI;
            for (int x = 0; x <= segments; x++)
            {
                float u = (float)x / segments;
                float theta = u * Mathf.PI * 2f;
                verts.Add(new Vector3(
                    Mathf.Sin(phi) * Mathf.Cos(theta) * rayon,
                    Mathf.Cos(phi) * rayon,
                    Mathf.Sin(phi) * Mathf.Sin(theta) * rayon));
            }
        }

        for (int y = 0; y < rings; y++)
            for (int x = 0; x < segments; x++)
            {
                int a = y * (segments + 1) + x;
                int b = a + segments + 1;

                // Triangles orientés vers l'extérieur (normales visibles de l'extérieur)
                tris.Add(a); tris.Add(a + 1); tris.Add(b);
                tris.Add(a + 1); tris.Add(b + 1); tris.Add(b);
            }

        mesh.vertices = verts.ToArray();
        mesh.triangles = tris.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    public static Mesh CreerCube(float taille)
    {
        float h = taille * 0.5f;
        var mesh = new Mesh();
        var verts = new Vector3[]
        {
            new Vector3(-h,-h,-h), new Vector3( h,-h,-h), new Vector3( h,-h, h), new Vector3(-h,-h, h),
            new Vector3(-h, h,-h), new Vector3( h, h,-h), new Vector3( h, h, h), new Vector3(-h, h, h),
        };
        var tris = new int[]
        {
            // 6 faces, chaque face en ordre extérieur
            0,2,1, 0,3,2,     // bas
            4,5,6, 4,6,7,     // haut
            0,1,5, 0,5,4,     // avant
            1,2,6, 1,6,5,     // droite
            2,3,7, 2,7,6,     // arrière
            3,0,4, 3,4,7,     // gauche
        };
        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
}

public class SuiviRigidbody : MonoBehaviour
{
    public CustomRigidbody rb;
    private Vector3 dernierX = new Vector3(float.NaN, 0, 0);

    void Update()
    {
        if (rb == null) return;
        CustomVector3 x = rb.state.x;
        Vector3 cible = new Vector3(x.x, x.y, x.z);
        if (cible != dernierX) { transform.position = cible; dernierX = cible; }

        CustomMatrix3 R = rb.state.R;
        Matrix4x4 m = R.ToMatrix4x4(CustomVector3.zero);
        transform.rotation = Quaternion.LookRotation(
            new Vector3(m.m02, m.m12, m.m22),
            new Vector3(m.m01, m.m11, m.m21));
    }
}

public class RenduParticules : MonoBehaviour
{
    public SystemeParticules systeme;
    public float rayon = 0.05f;
    public Mesh mesh;
    public Material mat;
    private Matrix4x4[] matrices = new Matrix4x4[1023];

    void Update()
    {
        if (systeme == null || mesh == null || mat == null) return;
        int n = systeme.N;
        int traites = 0;
        while (traites < n)
        {
            int lot = Mathf.Min(1023, n - traites);
            for (int i = 0; i < lot; i++)
            {
                CustomVector3 p = systeme.p[traites + i].x;
                matrices[i] = Matrix4x4.TRS(
                    new Vector3(p.x, p.y, p.z),
                    Quaternion.identity,
                    Vector3.one);
            }
            Graphics.DrawMeshInstanced(mesh, 0, mat, matrices, lot);
            traites += lot;
        }
    }
}