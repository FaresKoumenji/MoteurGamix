using System;
using System.Collections.Generic;

/// <summary>
/// Réponse aux collisions (Ch.4, slides 34 et 68) :
///   1) séparation des positions au prorata de 1/m (le plus léger bouge le plus) ;
///   2) impulsion normale  j = -(1+e) vrel / (wA + wB)  ;
///   3) frottement tangentiel de Coulomb  jT  clampé à  μ |j|.
/// Les impulsions sont appliquées à P et L (donc les chocs hors-centre
/// font tourner le corps via le couple (p - x) ∧ j).
/// </summary>
public static class ReponseCollision
{
    public static int nbContactsResolus;

    public static void ResoudreContacts(List<Contact> contacts, List<CustomRigidbody> corps,
                                        float dt, float restitution, float frottement)
    {
        nbContactsResolus = contacts.Count;

        for (int k = 0; k < contacts.Count; k++)
        {
            Contact c = contacts[k];
            if (c.indiceA < 0 || c.indiceB < 0) continue;

            CustomRigidbody A = corps[c.indiceA];
            CustomRigidbody B = corps[c.indiceB];

            if (A.invMass <= 0f && B.invMass <= 0f) continue;

            // ---------- 1) Séparation positionnelle ----------
            float wA = A.invMass;
            float wB = B.invMass;
            float wSum = wA + wB;
            if (wSum > 0f)
            {
                CustomVector3 correction = c.normale * (c.profondeur / wSum);
                A.Translater(-correction * wA);
                B.Translater( correction * wB);
            }

            // ---------- 2) Impulsion normale ----------
            CustomVector3 vA = A.VitesseAuPoint(c.point);
            CustomVector3 vB = B.VitesseAuPoint(c.point);
            CustomVector3 vRel = vB - vA;

            float vn = CustomVector3.Dot(vRel, c.normale);
            if (vn >= 0f) continue;             // s'éloignent déjà

            float e = restitution;
            float j = -(1f + e) * vn / wSum;

            CustomVector3 impulsion = c.normale * j;
            A.AppliquerImpulsion(-impulsion, c.point);
            B.AppliquerImpulsion( impulsion, c.point);

            // ---------- 3) Frottement tangentiel ----------
            if (frottement <= 0f) continue;

            vA = A.VitesseAuPoint(c.point);
            vB = B.VitesseAuPoint(c.point);
            vRel = vB - vA;

            CustomVector3 vT = vRel - c.normale * CustomVector3.Dot(vRel, c.normale);
            float vtm = vT.magnitude;
            if (vtm < 1e-6f) continue;

            CustomVector3 t = vT / vtm;
            float jT = -vtm / wSum;
            float maxT = frottement * Math.Abs(j);
            if (jT < -maxT) jT = -maxT;
            if (jT >  maxT) jT =  maxT;

            CustomVector3 impT = t * jT;
            A.AppliquerImpulsion(-impT, c.point);
            B.AppliquerImpulsion( impT, c.point);
        }
    }
}