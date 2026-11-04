using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Application 3 (Ch.4, slide 32) : billard 3D.
/// - Intègre chaque sphère (CustomRigidbody en mode Sphere).
/// - Teste toutes les paires (force brute).
/// - Résout par impulsion élastique (e = 1), séparation au prorata de 1/m.
/// - Affiche Σ m·v pour vérifier la conservation de la quantité de mouvement.
/// Aucun Rigidbody, Collider ni Physics.* de Unity.
/// </summary>
public class Billiard : MonoBehaviour
{
    [Header("Paramètres")]
    public float rayon = 0.5f;
    public float restitution = 1.0f;

    List<CustomRigidbody> boules = new List<CustomRigidbody>();
    List<Contact> contacts = new List<Contact>();

    void Start()
    {
        var toutes = FindObjectsOfType<CustomRigidbody>();
        foreach (var rb in toutes)
        {
            if (rb.forme == FormeRigide.Sphere)
                boules.Add(rb);
        }
        Debug.Log("Billiard : " + boules.Count + " boules");
    }

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        // 1) Intégration
        for (int i = 0; i < boules.Count; i++)
        {
            boules[i].Integrer(dt);
            boules[i].MettreAJourVolume();
        }

        // 2) Détection (force brute, slide 28)
        contacts.Clear();
        for (int i = 0; i < boules.Count; i++)
        {
            for (int j = i + 1; j < boules.Count; j++)
            {
                if (PhaseEtroite.SphereSphere(boules[i].CentreMonde, rayon,
                                              boules[j].CentreMonde, rayon,
                                              out Contact c))
                {
                    c.indiceA = i;
                    c.indiceB = j;
                    contacts.Add(c);
                }
            }
        }

        // 3) Réponse
        for (int k = 0; k < contacts.Count; k++)
        {
            Contact c = contacts[k];
            CustomRigidbody A = boules[c.indiceA];
            CustomRigidbody B = boules[c.indiceB];

            float wA = A.invMass, wB = B.invMass;
            float wSum = wA + wB;
            if (wSum <= 0f) continue;

            // Séparation
            CustomVector3 corr = c.normale * (c.profondeur / wSum);
            A.state.x = A.state.x - corr * wA;
            B.state.x = B.state.x + corr * wB;

            // Impulsion élastique
            CustomVector3 vRel = B.velocity - A.velocity;
            float vn = CustomVector3.Dot(vRel, c.normale);
            if (vn >= 0f) continue;

            float jImp = -(1f + restitution) * vn / wSum;
            CustomVector3 imp = c.normale * jImp;
            A.AppliquerImpulsion(-imp, c.point);
            B.AppliquerImpulsion( imp, c.point);
        }

        // 4) Synchro visuelle
        for (int i = 0; i < boules.Count; i++)
            boules[i].SynchroVisuelle();
    }

    void OnGUI()
    {
        // Σ m·v (quantité de mouvement totale)
        CustomVector3 P = CustomVector3.zero;
        float Ec = 0f;
        for (int i = 0; i < boules.Count; i++)
        {
            P += boules[i].state.P;
            Ec += 0.5f * boules[i].Mass * boules[i].velocity.sqrMagnitude;
        }
        GUI.Box(new Rect(10, 250, 360, 60),
            $"BILLIARD\nΣm·v = ({P.x:F3}, {P.y:F3}, {P.z:F3})\nEc = {Ec:F3} J");
    }
}