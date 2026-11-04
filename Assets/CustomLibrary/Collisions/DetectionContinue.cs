using System;

/// <summary>
/// Détection continue par dichotomie (Ch.4, slides 9–14) :
/// on cherche tc ∈ [t0, t0+dt] tel que la distance signée s(tc) = 0.
/// On utilise s(t) = y(t) - r pour une sphère tombant sur un plan y = 0.
/// </summary>
public static class DetectionContinue
{
    /// <summary>
    /// Cherche tc par dichotomie sur f(t) entre a et b, sachant f(a)*f(b) < 0.
    /// Algorithme 3 (slide 12).
    /// </summary>
    public static float Dichotomie(Func<float, float> f, float a, float b, float epsilon = 1e-5f, int maxIter = 60)
    {
        float fa = f(a), fb = f(b);
        if (fa * fb > 0f) return a;

        for (int k = 0; k < maxIter && Math.Abs(b - a) > epsilon; k++)
        {
            float m = 0.5f * (a + b);
            float fm = f(m);
            if (fm * fa < 0f) { b = m; fb = fm; }
            else              { a = m; fa = fm; }
        }
        return 0.5f * (a + b);
    }

    /// <summary>
    /// Instant tc d'impact d'une sphère (centre c0, rayon r, vitesse v) avec le plan y = 0,
    /// sachant qu'à t0 la distance signée est positive et qu'à t0+dt elle est négative.
    /// La position est supposée rectiligne entre t0 et t0+dt (approximation acceptable
    /// pour une balle avec faible gravité sur un seul pas ; le prof demande cette méthode).
    /// </summary>
    public static float InstantImpact(CustomVector3 c0, CustomVector3 v, float r, float dt)
    {
        Func<float, float> s = t =>
        {
            float y = c0.y + v.y * t;
            return y - r;
        };
        return Dichotomie(s, 0f, dt);
    }
}