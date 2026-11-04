using System;

/// <summary>Schéma d'intégration (Ch.2 Partie 1).</summary>
public enum ModeIntegration
{
    Euler,                // Euler explicite : x += v.dt ; v += a.dt   (slides 9 à 11)
    EulerSemiImplicite,   // v += a.dt puis x += v.dt                 (code FreeFall, slide 12)
    RK4                   // Runge-Kutta d'ordre 4                    (slides 14 à 26)
}

/// <summary>
/// Solveur ODE (Ch.2 Partie 1, slide 33). Le système du second ordre  x'' = a(x, v)  est ramené
/// au premier ordre :  dx/dt = v ,  dv/dt = a(x, v)  (slides 20-21 et 30).
/// Pour RK4 on retrouve les k1..k4 du cours, avec k.x pour la position et k.v pour la vitesse :
///   k1 = f(y)           k2 = f(y + h/2 k1)      k3 = f(y + h/2 k2)      k4 = f(y + h k3)
///   y(t+h) = y(t) + h/6 (k1 + 2 k2 + 2 k3 + k4)
/// </summary>
public static class SolveurODE
{
    public static void Pas(SystemeParticules s, float h, ModeIntegration mode)
    {
        int n = s.N;
        for (int i = 0; i < n; i++) { s.x0[i] = s.p[i].x; s.v0[i] = s.p[i].v; }

        switch (mode)
        {
            case ModeIntegration.Euler:
                s.Accelerations(s.x0, s.v0, s.a1);
                for (int i = 0; i < n; i++)
                {
                    s.x0[i] = s.x0[i] + s.v0[i] * h;     // avec l'ancienne vitesse
                    s.v0[i] = s.v0[i] + s.a1[i] * h;
                }
                break;

            case ModeIntegration.EulerSemiImplicite:
                s.Accelerations(s.x0, s.v0, s.a1);
                for (int i = 0; i < n; i++)
                {
                    s.v0[i] = s.v0[i] + s.a1[i] * h;
                    s.x0[i] = s.x0[i] + s.v0[i] * h;     // avec la nouvelle vitesse
                }
                break;

            case ModeIntegration.RK4:
                float demi = h * 0.5f;

                // k1 : k1x = v0 ; k1v = a1
                s.Accelerations(s.x0, s.v0, s.a1);

                // k2 : état y + h/2 k1
                for (int i = 0; i < n; i++)
                {
                    s.xt[i] = s.x0[i] + s.v0[i] * demi;
                    s.vt[i] = s.v0[i] + s.a1[i] * demi;
                }
                s.Accelerations(s.xt, s.vt, s.a2);
                for (int i = 0; i < n; i++) s.k2x[i] = s.vt[i];

                // k3 : état y + h/2 k2
                for (int i = 0; i < n; i++)
                {
                    s.xt[i] = s.x0[i] + s.k2x[i] * demi;
                    s.vt[i] = s.v0[i] + s.a2[i] * demi;
                }
                s.Accelerations(s.xt, s.vt, s.a3);
                for (int i = 0; i < n; i++) s.k3x[i] = s.vt[i];

                // k4 : état y + h k3
                for (int i = 0; i < n; i++)
                {
                    s.xt[i] = s.x0[i] + s.k3x[i] * h;
                    s.vt[i] = s.v0[i] + s.a3[i] * h;
                }
                s.Accelerations(s.xt, s.vt, s.a4);

                // mise à jour : y(t+h) = y(t) + h/6 (k1 + 2 k2 + 2 k3 + k4)
                float sixieme = h / 6f;
                for (int i = 0; i < n; i++)
                {
                    CustomVector3 k4x = s.vt[i];
                    s.x0[i] = s.x0[i] + (s.v0[i] + s.k2x[i] * 2f + s.k3x[i] * 2f + k4x) * sixieme;
                    s.v0[i] = s.v0[i] + (s.a1[i] + s.a2[i] * 2f + s.a3[i] * 2f + s.a4[i]) * sixieme;
                }
                break;
        }

        // Recopie dans les particules. Particules fixes : inchangées.
        float vmax2 = s.vitesseMax * s.vitesseMax;
        for (int i = 0; i < n; i++)
        {
            if (s.p[i].invmass <= 0f) { s.p[i].v = CustomVector3.zero; continue; }

            CustomVector3 vi = s.v0[i];
            float sq = vi.sqrMagnitude;
            if (sq > vmax2) vi = vi * (s.vitesseMax / (float)Math.Sqrt(sq));   // garde-fou (Euler instable)

            s.p[i].x = s.x0[i];
            s.p[i].v = vi;
        }
        s.t += h;
    }
}
