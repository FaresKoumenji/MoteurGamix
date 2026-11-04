using System;

/// <summary>
/// Données de contact (Ch.4, slide 31) : normale n unitaire de A vers B,
/// profondeur de pénétration δ, point de contact p.
/// </summary>
public struct Contact
{
    public CustomVector3 point;       // p
    public CustomVector3 normale;     // n (unitaire, de A vers B)
    public float profondeur;          // δ

    public int indiceA, indiceB;      // indices dans PhysicsWorld.corps

    public Contact(CustomVector3 point, CustomVector3 normale, float profondeur,
                   int indiceA = -1, int indiceB = -1)
    {
        this.point = point;
        this.normale = normale;
        this.profondeur = profondeur;
        this.indiceA = indiceA;
        this.indiceB = indiceB;
    }
}