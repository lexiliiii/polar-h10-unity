using UnityEngine;

public class HeatmapCaster : MonoBehaviour
{
    [SerializeField] private int paintRadius = 8;
    [SerializeField] private float maxDistance = 100f;
    [SerializeField] private float staleAfterSeconds = 0.5f;
    [SerializeField] private Camera referenceCamera;
    private readonly NeonXRStream stream = new NeonXRStream();
    private long lastPaintedSample;

    private bool neonExpected;
    private void OnEnable()
    {
        neonExpected = FindObjectOfType<PupilLabs.GazeDataProvider>(true) != null;
        stream.Refresh();
    }

    private void LateUpdate()
    {
        stream.Refresh();
        neonExpected |= stream.Provider != null;
        if (!referenceCamera) referenceCamera = Camera.main;
        if (!referenceCamera) return;
        if (stream.TryGetWorldRay(referenceCamera, staleAfterSeconds, out Ray ray))
        {
            if (stream.SampleCount == lastPaintedSample) return;
            lastPaintedSample = stream.SampleCount;
        }
        else
        {
            if (neonExpected) return;
            ray = referenceCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        }
        int layer = LayerMask.NameToLayer("Heatmaps");
        if (layer < 0) return;
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, 1 << layer)
            && hit.collider.TryGetComponent(out HeatmapQuad heatmap))
            heatmap.PaintPixels(hit.textureCoord.x, hit.textureCoord.y, paintRadius);
    }

    private void OnDisable() => stream.Dispose();
}

