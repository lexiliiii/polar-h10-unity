using System.Collections.Generic;
using UnityEngine;

public class ExosuitSpawner : MonoBehaviour
{
    public ExosuitMaterialSet materialSet;

    public void ApplyMaterials()
    {
        if (materialSet == null)
        {
            Debug.LogWarning("ExosuitSpawner skipped: no material set assigned.");
            return;
        }

        Transform modelRoot = transform.Find("Model");
        if (modelRoot == null)
        {
            Debug.LogError("Model object not found under Worker.");
            return;
        }

        foreach (var meshName in GetTargetMeshes(materialSet.suitType))
        {
            Transform mesh = modelRoot.Find(meshName);
            if (mesh == null)
            {
                Debug.LogWarning($"Mesh {meshName} not found under Model.");
                continue;
            }

            GameObject copy = Instantiate(mesh.gameObject, modelRoot);
            copy.name = meshName + "_Exosuit";

            Material overrideMat = materialSet.GetMaterialForMesh(meshName);

            if (overrideMat == null)
            {
                Debug.LogWarning("No material provided for " + meshName);
                continue;
            }

            var skinnedRenderer = copy.GetComponent<SkinnedMeshRenderer>();
            if (skinnedRenderer != null)
            {
                skinnedRenderer.material = overrideMat;
                continue;
            }

            var meshRenderer = copy.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                meshRenderer.material = overrideMat;
            }
            else
            {
                Debug.LogWarning("Material NOT applied to " + copy.name + " — missing renderer.");
            }
        }
    }

    private List<string> GetTargetMeshes(ExosuitType type)
    {
        return type switch
        {
            ExosuitType.FullBody => new List<string> { "BODY_mesh" },
            ExosuitType.Wrist => new List<string> { "BODY_mesh", "HAND_mesh" },
            ExosuitType.Neck => new List<string> { "BODY_mesh", "FACE_mesh" },
            ExosuitType.Shoulder => new List<string> { "BODY_mesh" },
            ExosuitType.Back => new List<string> { "BODY_mesh" },
            _ => new List<string>()
        };
    }
}