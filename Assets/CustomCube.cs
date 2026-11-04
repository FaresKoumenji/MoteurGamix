using UnityEngine;

/// <summary>
/// Wireframe cube drawn with Gizmos only (no mesh, no 3D object).
/// It "moves" because we transform its 8 vertices with our own matrix.
/// The GameObject's Transform is never modified.
/// </summary>
public class CustomCube : MonoBehaviour
{
    [Header("Bottom Face (local space)")]
    public Vector3 vertexA = new Vector3(-0.5f, -0.5f, -0.5f);
    public Vector3 vertexB = new Vector3(0.5f, -0.5f, -0.5f);
    public Vector3 vertexC = new Vector3(0.5f, -0.5f, 0.5f);
    public Vector3 vertexD = new Vector3(-0.5f, -0.5f, 0.5f);

    [Header("Top Face (local space)")]
    public Vector3 vertexE = new Vector3(-0.5f, 0.5f, -0.5f);
    public Vector3 vertexF = new Vector3(0.5f, 0.5f, -0.5f);
    public Vector3 vertexG = new Vector3(0.5f, 0.5f, 0.5f);
    public Vector3 vertexH = new Vector3(-0.5f, 0.5f, 0.5f);

    public float GravityCenter;

    private Vector3[] worldVertices;

    private Vector3[] LocalVertices()
    {
        return new[] { vertexA, vertexB, vertexC, vertexD, vertexE, vertexF, vertexG, vertexH };
    }

    public void ApplyTransformation(Matrix4x4 m)
    {
        Vector3[] local = LocalVertices();
        worldVertices = new Vector3[8];

        for (int i = 0; i < 8; i++)
        {
            Vector3 v = local[i];
            worldVertices[i] = new Vector3(
                m.m00 * v.x + m.m01 * v.y + m.m02 * v.z + m.m03,
                m.m10 * v.x + m.m11 * v.y + m.m12 * v.z + m.m13,
                m.m20 * v.x + m.m21 * v.y + m.m22 * v.z + m.m23);
        }
    }

    private void OnDrawGizmos()
    {
        // Désactivé en Play : RenduSolide affiche les meshes 3D
        if (Application.isPlaying) return;

        Gizmos.color = Color.green;

        Vector3[] p = LocalVertices();

        for (int i = 0; i < 4; i++)
        {
            Gizmos.DrawLine(p[i], p[(i + 1) % 4]);
            Gizmos.DrawLine(p[4 + i], p[4 + (i + 1) % 4]);
            Gizmos.DrawLine(p[i], p[4 + i]);
        }
    }
}