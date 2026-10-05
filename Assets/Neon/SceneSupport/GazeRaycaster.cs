using UnityEngine;

public class GazeRaycaster : MonoBehaviour
{
    [SerializeField] private float maxDistance = 100f;
    [SerializeField] private float staleAfterSeconds = 0.5f;
    [SerializeField] private Camera referenceCamera;
    private readonly NeonXRStream stream = new NeonXRStream();

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
        if (!stream.TryGetWorldRay(referenceCamera, staleAfterSeconds, out Ray ray))
        {
            if (neonExpected) return;
            // Retain head-forward interactions only in legacy scenes without Neon.
            ray = referenceCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        }
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, ~0, QueryTriggerInteraction.Ignore)
            && hit.collider.TryGetComponent(out HeatingTarget target)) target.PingObject();
    }

    private void OnDisable() => stream.Dispose();
}

