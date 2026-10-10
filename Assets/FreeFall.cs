using UnityEngine;

[RequireComponent(typeof(CustomCube))]
[RequireComponent(typeof(MaterialProperties))]
public class FreeFall : MonoBehaviour
{
    [Header("References")]
    public CustomCube cube;
    public MaterialProperties materialProps;
    public EnvironmentProperties environment;

    [Header("State")]
    public Vector3 velocity = Vector3.zero;

    private float mass;

    private void Awake()
    {
        GameObject envObj = GameObject.FindWithTag("Environment");
        if (envObj != null)
        {
            environment = envObj.GetComponent<EnvironmentProperties>();
        }
    }

    private void Start()
    {
        // 1. Calculate bounding volume of the CustomCube
        float volume = CalculateCubeVolume();

        // 2. Compute mass based on calculated volume and material density
        mass = materialProps.CalculateMass(volume);
    }

    private void FixedUpdate()
    {
        if (environment == null) return;

        // 3. F = m * g
        Vector3 gravityForce = mass * environment.gravity;

        // 4. Acceleration = Force / Mass -> a = g
        Vector3 acceleration = gravityForce / mass;

        // 5. Numerical integration (v = v0 + a*dt, pos = pos0 + v*dt)
        velocity += acceleration * Time.fixedDeltaTime;
        transform.position += velocity * Time.fixedDeltaTime;
    }

    /// <summary>
    /// Approximates volume using bounding box dimensions derived from the 8 custom vertices.
    /// </summary>
    private float CalculateCubeVolume()
    {
        Vector3[] vertices = new Vector3[]
        {
            cube.vertexA, cube.vertexB, cube.vertexC, cube.vertexD,
            cube.vertexE, cube.vertexF, cube.vertexG, cube.vertexH
        };

        Bounds bounds = new Bounds(vertices[0], Vector3.zero);
        for (int i = 1; i < vertices.Length; i++)
        {
            bounds.Encapsulate(vertices[i]);
        }

        Vector3 size = bounds.size;
        return size.x * size.y * size.z;
    }
}