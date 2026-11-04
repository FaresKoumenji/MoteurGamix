using System;
using System.Collections.Generic;
using UnityEngine;

public static class ReponseCollision
{
    public static int nbContactsResolus;

    // Correction positionnelle : tolérance + fraction corrigée par passe (réduit le jitter)
    const float slop = 0.01f;
    const float pourcentage = 0.8f;

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

            float wA = A.invMass;
            float wB = B.invMass;
            float wSum = wA + wB;

            if (wSum <= 1e-6f) continue;

            // 1) Séparation positionnelle (avec slop)
            float prof = Math.Max(c.profondeur - slop, 0f) * pourcentage;
            CustomVector3 correction = c.normale * (prof / wSum);
            if (wA > 0f) A.Translater(-correction * wA);
            if (wB > 0f) B.Translater(correction * wB);

            // 2) Impulsion normale
            CustomVector3 vA = A.VitesseAuPoint(c.point);
            CustomVector3 vB = B.VitesseAuPoint(c.point);
            CustomVector3 vRel = vB - vA;
            float vn = CustomVector3.Dot(vRel, c.normale);
            if (vn >= 0f) continue;

            float e = restitution;
            float j = -(1f + e) * vn / wSum;
            if (float.IsNaN(j) || float.IsInfinity(j)) continue;

            CustomVector3 impulsion = c.normale * j;
            if (wA > 0f) A.AppliquerImpulsion(-impulsion, c.point);
            if (wB > 0f) B.AppliquerImpulsion(impulsion, c.point);

            // 3) Frottement
            if (frottement > 0f)
            {
                vA = A.VitesseAuPoint(c.point);
                vB = B.VitesseAuPoint(c.point);
                vRel = vB - vA;
                CustomVector3 vT = vRel - c.normale * CustomVector3.Dot(vRel, c.normale);
                float vtm = vT.magnitude;
                if (vtm > 1e-6f)
                {
                    CustomVector3 t = vT / vtm;
                    float jT = -vtm / wSum;
                    float maxT = frottement * Math.Abs(j);
                    if (jT < -maxT) jT = -maxT;
                    if (jT > maxT) jT = maxT;

                    CustomVector3 impT = t * jT;
                    if (wA > 0f) A.AppliquerImpulsion(-impT, c.point);
                    if (wB > 0f) B.AppliquerImpulsion(impT, c.point);
                }
            }
        }

        // 4) Correction post-séparation : annule la vitesse qui pénètre encore
        StopperVitesseNormale(contacts, corps);
    }

    /// <summary>
    /// Pour chaque contact, si un corps a une vitesse qui pointe vers l'autre corps,
    /// on annule cette composante. Les corps statiques (invMass = 0) sont ignorés.
    /// </summary>
    static void StopperVitesseNormale(List<Contact> contacts, List<CustomRigidbody> corps)
    {
        for (int k = 0; k < contacts.Count; k++)
        {
            Contact c = contacts[k];
            if (c.indiceA < 0 || c.indiceB < 0) continue;

            CustomRigidbody A = corps[c.indiceA];
            CustomRigidbody B = corps[c.indiceB];

            // A est poussé vers B (normale de A vers B)
            if (A.invMass > 0f)
            {
                float vnA = CustomVector3.Dot(A.velocity, c.normale);
                if (vnA > 0f)
                {
                    CustomVector3 delta = c.normale * (vnA * A.Mass);
                    A.state.P = A.state.P - delta;
                    A.ForcerRefresh();
                }
            }
            // B est poussé vers A
            if (B.invMass > 0f)
            {
                float vnB = CustomVector3.Dot(B.velocity, c.normale);
                if (vnB < 0f)
                {
                    CustomVector3 delta = c.normale * (vnB * B.Mass);
                    B.state.P = B.state.P - delta;
                    B.ForcerRefresh();
                }
            }
        }
    }
}