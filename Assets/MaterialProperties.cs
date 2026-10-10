using UnityEngine;

public class MaterialProperties : MonoBehaviour
{
    [Header("Physical Characteristics")]
    [Tooltip("Density in kg/m³ (e.g., Wood = 600, Aluminum = 2700, Steel = 7850)")]
    public float density = 1000f; // Default density of water/human body (~1000 kg/m³)

    /// <summary>
    /// Calculates total mass: Mass = Volume * Density
    /// </summary>
    public float CalculateMass(float volume)
    {
        return Mathf.Max(0.0001f, volume * density);
    }
}