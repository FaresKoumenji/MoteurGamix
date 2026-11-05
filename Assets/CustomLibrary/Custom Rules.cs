using UnityEngine;

[RequireComponent(typeof(CustomRigidbody))]
public class BalleRebondissante : MonoBehaviour
{
    [Header("Paramètres")]
    public float rayon = 0.5f;
    public float restitution = 0.8f;
    public float frottement = 0.05f;
    public float seuilRepos = 0.3f;

    CustomRigidbody rb;
    bool auRepos = false;

    void Start()
    {
        rb = GetComponent<CustomRigidbody>();
        rb.forme = FormeRigide.Sphere;
        rb.rayon = rayon;
    }

    void FixedUpdate()
    {
        if (auRepos || rb == null) return;

        // L'intégration est faite par PhysicsWorld.Step → ici on ne fait que la réponse au sol

        float y = rb.state.x.y;
        float s0 = y - rayon;
        if (s0 > 0f) return;              // pas en contact

        // Contact : replacer au contact exact + restitution + frottement
        rb.state.x = new CustomVector3(rb.state.x.x, rayon, rb.state.x.z);

        float vy = rb.velocity.y;
        float vyRebond = -restitution * vy;
        if (Mathf.Abs(vyRebond) < seuilRepos) { vyRebond = 0f; auRepos = true; }
        float vx = rb.velocity.x * (1f - frottement);
        float vz = rb.velocity.z * (1f - frottement);

        rb.state.P = new CustomVector3(vx, vyRebond, vz) * rb.Mass;
        rb.state.L = CustomVector3.zero;
        rb.SynchroVisuelle();
    }
}