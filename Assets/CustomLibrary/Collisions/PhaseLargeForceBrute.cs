using System.Collections.Generic;

/// <summary>
/// Phase large par force brute (Ch.4, slide 28) : n(n-1)/2 tests AABB.
/// Référence pour mesurer les gains des autres méthodes (slide 52).
/// </summary>
public class PhaseLargeForceBrute : IPhaseLarge
{
    public void TrouverPaires(List<CustomRigidbody> corps, List<(int i, int j)> paires)
    {
        for (int i = 0; i < corps.Count; i++)
        {
            AABB bi = corps[i].Volume;
            for (int j = i + 1; j < corps.Count; j++)
            {
                if (bi.Recouvre(corps[j].Volume))
                    paires.Add((i, j));
            }
        }
    }
}