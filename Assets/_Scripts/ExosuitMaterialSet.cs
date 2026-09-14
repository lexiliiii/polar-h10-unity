using UnityEngine;

[CreateAssetMenu(fileName = "ExosuitMaterialSet", menuName = "Exosuits/Material Set")]
public class ExosuitMaterialSet : ScriptableObject
{
    public ExosuitType suitType;

    public Material bodyMaterial; // used for BODY_mesh
    public Material handMaterial; // used for HAND_mesh
    public Material faceMaterial; // used for FACE_mesh

    public Material GetMaterialForMesh(string meshName)
    {
        Material result = meshName switch
        {
            "BODY_mesh" => bodyMaterial,
            "HAND_mesh" => handMaterial,
            "FACE_mesh" => faceMaterial,
            _ => null
        };

        if (result == null)
            Debug.LogWarning($"[ExosuitMaterialSet] No material assigned for mesh: {meshName}");

        return result;
    }
}