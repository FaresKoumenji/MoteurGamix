using UnityEngine;

/// <summary>
/// Application 1 (Ch.4, slide 14) : balle rebondissante.
/// - Intégration RK4 (via CustomRigidbody.Integrer)
/// - Détection de contact par distance signée s = y - r
/// - Instant de contact tc par dichotomie (DetectionContinue)
/// - Réponse : vy <- -e * vy, frottement sur vx, vz
/// - Mise au repos quand le rebond devient trop faible
/// Aucun Rigidbody, Collider ni Physics.* de Unity.
/// </summary>
[RequireComponent(typeof(CustomRigidbody))]
public class BalleRebondissante : MonoBehaviour
{
    [Header("Paramètres")]
    public float rayon = 0.5f;
    public float restitution = 0.8f;      // e
    public float frottement = 0.05f;      // mu (au contact)
    public float seuilRepos = 0.3f;       // |vy| en dessous duquel on arrête

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
        if (auRepos) return;
        if (rb == null) return;

        float dt = Time.fixedDeltaTime;

        // 1) Intégration (CustomRigidbody gère gravité + intégration)
        rb.Integrer(dt);
        rb.MettreAJourVolume();
        rb.SynchroVisuelle();

        // 2) Détection : distance signée au sol y = 0
        float y0 = rb.state.x.y;
        float s0 = y0 - rayon;
        float vy = rb.velocity.y;

        // Si on est déjà au-dessus et qu'on monte, rien à faire
        if (s0 > 0f && vy >= 0f) return;

        // 3) Si pénétration (s0 < 0) ou contact, on cherche tc par dichotomie
        CustomVector3 c0 = new CustomVector3(rb.state.x.x, rb.state.x.y - vy * dt, rb.state.x.z);
        CustomVector3 v0 = new CustomVector3(rb.velocity.x, vy, rb.velocity.z);
        float tc = DetectionContinue.InstantImpact(c0, v0, rayon, dt);

        // 4) Replace au contact
        rb.state.x = c0 + v0 * tc;
        rb.state.x = new CustomVector3(rb.state.x.x, rayon, rb.state.x.z);

        // 5) Réponse : restitution + frottement
        float vyRebond = -restitution * v0.y;
        if (Mathf.Abs(vyRebond) < seuilRepos)
        {
            vyRebond = 0f;
            auRepos = true;
        }
        float vx = v0.x * (1f - frottement);
        float vz = v0.z * (1f - frottement);

        rb.state.P = new CustomVector3(vx, vyRebond, vz) * rb.Mass;
        rb.state.L = CustomVector3.zero;
        rb.SynchroVisuelle();
    }
}