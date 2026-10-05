

using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Displays a red marker at the current Neon gaze ray hit in the XR camera view.</summary>
public sealed class NeonGazeDot : MonoBehaviour
{
    [SerializeField] private float maxDistance = 20f;
    [SerializeField] private float noHitDistance = 3f;
    [SerializeField] private float diameter = 0.045f;
    [SerializeField] private float staleAfterSeconds = 0.5f;

    private readonly NeonXRStream stream = new NeonXRStream();
    private Material dotMaterial;
    private Camera referenceCamera;
    private Transform dot;


    private void Awake()
    {
        var dotObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        dotObject.name = "Neon Gaze Dot (runtime)";
        dotObject.transform.SetParent(transform, false);
        dotObject.transform.localScale = Vector3.one * diameter;

        var collider = dotObject.GetComponent<Collider>();
        if (collider != null) Destroy(collider);

        var renderer = dotObject.GetComponent<Renderer>();
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
        if (shader != null)
        {
            var material = new Material(shader);
            dotMaterial = material;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.red);
            if (material.HasProperty("_Color")) material.SetColor("_Color", Color.red);
            renderer.material = material;
        }

        dot = dotObject.transform;
        dotObject.SetActive(false);
    }

    private void OnEnable() => stream.Refresh();

    private void LateUpdate()
    {
        stream.Refresh();
        if (dot == null) return;
        if (referenceCamera == null) referenceCamera = Camera.main;
        if (!stream.TryGetWorldRay(referenceCamera, staleAfterSeconds, out Ray worldRay))
        {
            dot.gameObject.SetActive(false);
            return;
        }
        Vector3 position;
        if (Physics.Raycast(worldRay, out RaycastHit hit, maxDistance, ~0, QueryTriggerInteraction.Ignore))
            position = hit.point + hit.normal * 0.012f;
        else
            position = worldRay.origin + worldRay.direction * noHitDistance;

        dot.position = position;
        dot.localScale = Vector3.one * diameter;
        dot.gameObject.SetActive(true);
    }

    private void OnDisable()
    {
        stream.Dispose();
        if (dot != null) dot.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (dotMaterial != null) Destroy(dotMaterial);
    }
}

