using System;

public enum CustomForceMode
{
    Force,         // continuous force: dP/dt += F
    Impulse,       // instantaneous: P += J
    Acceleration,  // continuous acceleration (ignores mass): F = m.a
    VelocityChange // instantaneous velocity change (ignores mass): P += m.dv
}

/// <summary>
/// A force applied at a point. It no longer touches velocities: it only gives the rigid body
/// the quantities of the course:  F (-> dP/dt)  and  tau = (r - x) ^ F  (-> dL/dt).
/// </summary>
public class CustomForce
{
    public CustomVector3 Vector;
    public CustomVector3 ApplicationPoint;
    public bool HasApplicationPoint;
    public CustomForceMode Mode;

    /// <summary>Force applied at the center of mass (no torque).</summary>
    public CustomForce(CustomVector3 vector, CustomForceMode mode = CustomForceMode.Force)
    {
        Vector = vector;
        ApplicationPoint = CustomVector3.zero;
        HasApplicationPoint = false;
        Mode = mode;
    }

    /// <summary>Force applied at a world-space point.</summary>
    public CustomForce(CustomVector3 vector, CustomVector3 applicationPoint, CustomForceMode mode = CustomForceMode.Force)
    {
        Vector = vector;
        ApplicationPoint = applicationPoint;
        HasApplicationPoint = true;
        Mode = mode;
    }

    public bool IsInstantaneous => Mode == CustomForceMode.Impulse || Mode == CustomForceMode.VelocityChange;

    /// <summary>Force (or impulse) vector in physical units, after taking the mode into account.</summary>
    public CustomVector3 CalculateEffectiveVector(float mass)
    {
        float m = Math.Max(0.0001f, mass);
        switch (Mode)
        {
            case CustomForceMode.Force: return Vector;
            case CustomForceMode.Impulse: return Vector;
            case CustomForceMode.Acceleration: return Vector * m;
            case CustomForceMode.VelocityChange: return Vector * m;
            default: return CustomVector3.zero;
        }
    }

    /// <summary>tau = (r - x) ^ F, with x the center of mass (slide 37).</summary>
    public CustomVector3 CalculateTorque(CustomVector3 centerOfMass, float mass)
    {
        if (!HasApplicationPoint) return CustomVector3.zero;
        CustomVector3 leverArm = ApplicationPoint - centerOfMass;
        return CustomVector3.Cross(leverArm, CalculateEffectiveVector(mass));
    }

    public void Execute(CustomRigidbody rb)
    {
        if (rb == null) return;

        CustomVector3 f = CalculateEffectiveVector(rb.Mass);
        CustomVector3 tau = CalculateTorque(rb.WorldCenterOfMass, rb.Mass);
        rb.AddForceAndTorque(f, tau, IsInstantaneous);
    }
}