using UnityEngine;

public class Main : MonoBehaviour
{
    [Header("Force Configuration")]
    [Tooltip("Direction vector of the applied force.")]
    public CustomVector3 forceDirection = new CustomVector3(1f, 0.5f, 0f);

    [Tooltip("Magnitude (strength) of the force.")]
    public float forceMagnitude = 10f;

    [Header("Off-Center Lever Arm")]
    [Tooltip("Offset relative to the Center of Mass. Set to (0,0,0) for pure linear push, or shift to create spin.")]
    public CustomVector3 leverArmOffset = new CustomVector3(0f, 0.5f, 0f);

    [Header("Force Mode")]
    public CustomForceMode forceMode = CustomForceMode.Impulse;

    [Header("Gizmos Visualization")]
    public bool showGizmos = true;
    public float forceVectorGizmoScale = 0.2f;

    private CustomRigidbody customRigidbody;

    void Start()
    {
        customRigidbody = GetComponent<CustomRigidbody>();

        if (customRigidbody != null)
        {
            // 1. Calculate explicit force vector: Direction * Magnitude
            CustomVector3 forceVector = forceDirection.normalized * forceMagnitude;

            // 2. Derive point of application automatically from Center of Mass + Lever Arm
            CustomVector3 impactPoint = customRigidbody.WorldCenterOfMass + leverArmOffset;

            // 3. Apply force at the calculated location
            customRigidbody.ApplyForceAtPosition(forceVector, impactPoint, forceMode);
        }
        else
        {
            Debug.LogWarning("No CustomRigidbody component attached to " + gameObject.name);
        }
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        // Fetch or temporarily reference CustomRigidbody for scene preview
        if (customRigidbody == null)
            customRigidbody = GetComponent<CustomRigidbody>();

        if (customRigidbody == null) return;

        // Convert CustomVector3 to UnityEngine.Vector3 for Gizmos
        CustomVector3 comCustom = customRigidbody.WorldCenterOfMass;
        Vector3 comWorld = new Vector3(comCustom.x, comCustom.y, comCustom.z);

        CustomVector3 impactCustom = comCustom + leverArmOffset;
        Vector3 impactWorld = new Vector3(impactCustom.x, impactCustom.y, impactCustom.z);

        CustomVector3 forceDirCustom = forceDirection.normalized;
        Vector3 forceDirWorld = new Vector3(forceDirCustom.x, forceDirCustom.y, forceDirCustom.z);

        // 1. Draw Center of Mass (Yellow Sphere)
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(comWorld, 0.1f);

        // 2. Draw Lever Arm Vector r (Cyan Line from CoM to Impact Point)
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(comWorld, impactWorld);
        Gizmos.DrawSphere(impactWorld, 0.05f);

        // 3. Draw Force Vector F (Red Arrow starting at Impact Point)
        Gizmos.color = Color.red;
        Vector3 forceEnd = impactWorld + (forceDirWorld * forceMagnitude * forceVectorGizmoScale);
        Gizmos.DrawLine(impactWorld, forceEnd);

        // Draw simple arrowhead at the end of Force Vector
        Vector3 right = Quaternion.LookRotation(forceDirWorld == Vector3.zero ? Vector3.forward : forceDirWorld) * Quaternion.Euler(0, 200, 0) * Vector3.forward;
        Vector3 left = Quaternion.LookRotation(forceDirWorld == Vector3.zero ? Vector3.forward : forceDirWorld) * Quaternion.Euler(0, 160, 0) * Vector3.forward;
        Gizmos.DrawRay(forceEnd, right * 0.25f);
        Gizmos.DrawRay(forceEnd, left * 0.25f);
    }
}