using UnityEngine;

[ExecuteInEditMode]
public class RenderModelBehindModal : MonoBehaviour
{
    [SerializeField] private Material material;
    [SerializeField] private int renderQueue = 3001;

    void Awake()
    {
        if (material != null)
        {
            material.renderQueue = renderQueue;
        }
        else
        {
            Debug.LogWarning("No material assigned to RenderModelBehindModal on " + gameObject.name);
        }
    }
}
