using System;
using UnityEngine;

[Serializable]
public struct RigidBodyState
{
    public CustomVector3 x;
    public CustomMatrix3 R;
    public CustomVector3 P;
    public CustomVector3 L;
}

public enum FormeRigide { AABB, Sphere, OBB }

[RequireComponent(typeof(CustomCube))]
public class CustomRigidbody : MonoBehaviour
{
    [Header("Conditions initiales")]
    public CustomVector3 initialPosition = CustomVector3.zero;
    public CustomVector3 initialEulerAngles = CustomVector3.zero;

    [Header("Masse")]
    public float mass = 1f;
    public bool hasGravity = true;

    [Header("Résistance")]
    public float drag = 0.05f;
    public float angularDrag = 0.05f;

    [Header("Centre de masse")]
    public CustomVector3 centerOfMassOffset = CustomVector3.zero;

    [Header("Gravité")]
    public CustomVector3 gravityForceVector = new CustomVector3(0f, -9.81f, 0f);

    [Header("Forme de collision")]
    public FormeRigide forme = FormeRigide.AABB;
    public float rayon = 0.5f;

    [Header("VECTEUR D'ETAT  X(t) = (x, R, P, L)")]
    public RigidBodyState state;

    [Header("Dérivés")]
    public CustomVector3 velocity;
    public CustomVector3 angularVelocity;

    [Header("Debug")]
    public bool showStateOnScreen = true;

    private CustomCube cube;
    private CustomVector3 invIbodyDiag;
    private CustomVector3 forceAccumulator;
    private CustomVector3 torqueAccumulator;
    private AABB volume;

    public float Mass => Math.Max(0.0001f, mass);
    public float invMass => 1f / Mass;
    public CustomVector3 CentreMonde => Application.isPlaying ? state.x : initialPosition;
    public CustomVector3 WorldCenterOfMass => CentreMonde;
    public AABB Volume => volume;

    public OBB OBBMonde
    {
        get
        {
            float a = Math.Abs(cube.vertexG.x - cube.vertexA.x) * 0.5f;
            float b = Math.Abs(cube.vertexG.y - cube.vertexA.y) * 0.5f;
            float c = Math.Abs(cube.vertexG.z - cube.vertexA.z) * 0.5f;
            return new OBB(state.x, state.R, new CustomVector3(a, b, c));
        }
    }

    private void Awake()
    {
        cube = GetComponent<CustomCube>();

        float a = Math.Abs(cube.vertexG.x - cube.vertexA.x);
        float b = Math.Abs(cube.vertexG.y - cube.vertexA.y);
        float c = Math.Abs(cube.vertexG.z - cube.vertexA.z);
        float m = Mass;
        float ix = m / 12f * (b * b + c * c);
        float iy = m / 12f * (a * a + c * c);
        float iz = m / 12f * (a * a + b * b);
        invIbodyDiag = new CustomVector3(
            1f / Math.Max(1e-6f, ix),
            1f / Math.Max(1e-6f, iy),
            1f / Math.Max(1e-6f, iz));

        state.x = initialPosition;
        state.R = CustomMatrix3.FromEulerDegrees(initialEulerAngles);
        state.P = CustomVector3.zero;
        state.L = CustomVector3.zero;

        RefreshDerived();
        MettreAJourVolume();
    }

    private void Start()
    {
        SynchroVisuelle();
        var monde = PhysicsWorld.Obtenir();
        if (monde != null) monde.EnregistrerCorps(this);
    }

    private void RefreshDerived()
    {
        velocity = state.P / Mass;

        CustomVector3 lBody = state.R.MultiplyTranspose(state.L);
        CustomVector3 wBody = new CustomVector3(
            lBody.x * invIbodyDiag.x,
            lBody.y * invIbodyDiag.y,
            lBody.z * invIbodyDiag.z);
        angularVelocity = state.R.Multiply(wBody);
    }

    public void Integrer(float dt)
    {
        if (invMass <= 0f) return;   // corps fixe : n'intègre pas

        RefreshDerived();

        CustomVector3 F = forceAccumulator;
        CustomVector3 tau = torqueAccumulator;

        if (hasGravity)
            F += gravityForceVector * Mass;

        F += velocity * (-drag);
        tau += angularVelocity * (-angularDrag);

        CustomVector3 dx = velocity;
        CustomMatrix3 dR = state.R.SkewTimes(angularVelocity);
        CustomVector3 dP = F;
        CustomVector3 dL = tau;

        state.x = state.x + dx * dt;
        state.R = state.R + dR * dt;
        state.P = state.P + dP * dt;
        state.L = state.L + dL * dt;

        state.R = state.R.Orthonormalized();

        forceAccumulator = CustomVector3.zero;
        torqueAccumulator = CustomVector3.zero;

        RefreshDerived();
    }

    public void MettreAJourVolume()
    {
        Vector3[] locaux = {
            cube.vertexA, cube.vertexB, cube.vertexC, cube.vertexD,
            cube.vertexE, cube.vertexF, cube.vertexG, cube.vertexH
        };

        CustomVector3[] monde = new CustomVector3[8];
        for (int i = 0; i < 8; i++)
        {
            CustomVector3 v = new CustomVector3(locaux[i].x, locaux[i].y, locaux[i].z);
            monde[i] = state.R.Multiply(v) + state.x;
        }

        volume = AABB.Englobant(monde, 8);
    }

    public void SynchroVisuelle()
    {
        CustomVector3 t = state.x - state.R.Multiply(centerOfMassOffset);
        cube.ApplyTransformation(state.R.ToMatrix4x4(t));
    }

    public void Translater(CustomVector3 d)
    {
        if (invMass <= 0f) return;
        state.x = state.x + d;
    }

    public void AppliquerImpulsion(CustomVector3 j, CustomVector3 p)
    {
        if (invMass <= 0f) return;
        state.P = state.P + j;
        CustomVector3 r = p - state.x;
        state.L = state.L + CustomVector3.Cross(r, j);
        RefreshDerived();
    }

    public CustomVector3 VitesseAuPoint(CustomVector3 p)
    {
        return velocity + CustomVector3.Cross(angularVelocity, p - state.x);
    }

    public void AddForceAndTorque(CustomVector3 force, CustomVector3 torque, bool instantaneous)
    {
        if (instantaneous)
        {
            state.P += force;
            state.L += torque;
            RefreshDerived();
        }
        else
        {
            forceAccumulator += force;
            torqueAccumulator += torque;
        }
    }

    public void ApplyForce(CustomForce force) { force.Execute(this); }

    public void ApplyForce(CustomVector3 forceVector, CustomForceMode mode = CustomForceMode.Force)
    {
        new CustomForce(forceVector, mode).Execute(this);
    }

    public void ApplyForceAtPosition(CustomVector3 forceVector, CustomVector3 worldPosition,
                                     CustomForceMode mode = CustomForceMode.Force)
    {
        new CustomForce(forceVector, worldPosition, mode).Execute(this);
    }

    private void OnGUI()
    {
        if (!showStateOnScreen || !Application.isPlaying) return;

        string txt =
            "VECTEUR D'ETAT  X(t) = ( x , R , P , L )\n" +
            $"x = ({state.x.x:F3}, {state.x.y:F3}, {state.x.z:F3})\n" +
            $"P = ({state.P.x:F3}, {state.P.y:F3}, {state.P.z:F3})\n" +
            $"L = ({state.L.x:F3}, {state.L.y:F3}, {state.L.z:F3})\n" +
            $"v = P/M       = ({velocity.x:F3}, {velocity.y:F3}, {velocity.z:F3})\n" +
            $"w = I^-1 . L  = ({angularVelocity.x:F3}, {angularVelocity.y:F3}, {angularVelocity.z:F3})";

        GUI.Box(new Rect(10, 130, 430, 110), txt);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        CustomVector3 com = CentreMonde;
        Gizmos.DrawSphere(new Vector3(com.x, com.y, com.z), 0.15f);

        if (volume.min == volume.max) return;

        Gizmos.color = Color.white;
        Vector3 mn = new Vector3(volume.min.x, volume.min.y, volume.min.z);
        Vector3 mx = new Vector3(volume.max.x, volume.max.y, volume.max.z);
        Gizmos.DrawWireCube((mn + mx) * 0.5f, mx - mn);
    }
}