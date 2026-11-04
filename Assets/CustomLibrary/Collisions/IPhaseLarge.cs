using System.Collections.Generic;

/// <summary>
/// Interface de phase large (Ch.4, slide 72). Doit rester indépendante
/// pour pouvoir comparer ForceBrute / SAP / Grille.
/// </summary>
public interface IPhaseLarge
{
    /// <summary>Remplit paires avec les paires (i, j), i &lt; j, candidates à un test étroit.</summary>
    void TrouverPaires(List<CustomRigidbody> corps, List<(int i, int j)> paires);
}