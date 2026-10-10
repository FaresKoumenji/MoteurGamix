using UnityEngine;

public class EnvironmentProperties : MonoBehaviour
{
    [Header("Environment Constants")]
    [Tooltip("Gravitational acceleration vector in m/s² (e.g., Earth = -9.81 on Y-axis)")]
    public Vector3 gravity = new Vector3(0f, -9.81f, 0f);

    [Tooltip("Fluid/Air density in kg/m³ (e.g., Air = 1.225, Water = 1000)")]
    public float mediumDensity = 1.225f;
}