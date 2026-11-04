using UnityEngine;

/// <summary>
/// Conversion CustomVector3 -> Vector3, UNIQUEMENT pour l'affichage (Gizmos).
/// La physique n'utilise que nos types ; Unity ne sert qu'à dessiner.
/// </summary>
public static class ConversionsUnity
{
    public static Vector3 ToUnity(this CustomVector3 c)
    {
        return new Vector3(c.x, c.y, c.z);
    }
}
