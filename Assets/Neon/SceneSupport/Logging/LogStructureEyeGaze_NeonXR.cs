using UnityEngine;
using UnityEngine.XR;
using PupilLabs;

public class LogStructureEyeGaze_NeonXR : LogStructure
{
    [SerializeField] private NeonGazeDataProvider gaze;
    [SerializeField] private float staleAfterSeconds = 0.5f;
    private readonly NeonXRStream stream = new NeonXRStream();
    private void OnEnable() => stream.Refresh(gaze);
    private void Update() => stream.Refresh(gaze);
    private void OnDisable() => stream.Dispose();
    public override string[] GetHeaderColumns()
    {
        return new string[]
        {
            "UserId", "SessionType", "Timestamp",
            "HeadPos.x", "HeadPos.y", "HeadPos.z",
            "HeadRot.x", "HeadRot.y", "HeadRot.z",
            "EyePos.x", "EyePos.y", "EyePos.z",
            "EyeRot.x", "EyeRot.y", "EyeRot.z",
            "Eye Data Available",
            "LookedTarget", "TargetPos.x", "TargetPos.y", "TargetPos.z",
            "Pupil Diameter Left", "Pupil Diameter Right",
            "Eyeball Center Left X", "Eyeball Center Left Y", "Eyeball Center Left Z",
            "Eyeball Center Right X", "Eyeball Center Right Y", "Eyeball Center Right Z",
            "Optical Axis Left X", "Optical Axis Left Y", "Optical Axis Left Z",
            "Optical Axis Right X", "Optical Axis Right Y", "Optical Axis Right Z",
            "Eyelid Data Available",
            "Eyelid Angle Top Left", "Eyelid Angle Top Right",
            "Eyelid Angle Bottom Left", "Eyelid Angle Bottom Right",
            "Eyelid Aperture Left", "Eyelid Aperture Right"
};
    }

    public override object[] GetData(string inputType, string inputStatus)
    {
        stream.Refresh(gaze);
        bool fresh = isActiveAndEnabled && stream.IsFresh(staleAfterSeconds);
        bool eyeStateAvailable = fresh && stream.EyeStateAvailable;
        bool eyelidStateAvailable = fresh && stream.EyelidAvailable;
        EyeState lastEyeState = eyeStateAvailable ? stream.EyeState : default;
        Eyelid lastEyelidState = eyelidStateAvailable ? stream.Eyelid : default;
        // Head tracking
        InputDevice head = InputDevices.GetDeviceAtXRNode(XRNode.Head);
        head.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 headPos);
        head.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion headRot);

        // Eye tracking
        InputDevice eye = InputDevices.GetDeviceAtXRNode(XRNode.CenterEye);
        eye.TryGetFeatureValue(CommonUsages.centerEyePosition, out Vector3 eyePos);
        eye.TryGetFeatureValue(CommonUsages.centerEyeRotation, out Quaternion eyeRot);

        GameObject target = null;
        // Neon XR Core's GazeRay is camera-local. Match the Pupil Labs visualizer's
        // convention before querying world-space scene colliders.
        Camera referenceCamera = Camera.main;
        if (fresh && stream.TryGetWorldRay(referenceCamera, staleAfterSeconds, out Ray worldGazeRay)) {
            if (Physics.Raycast(worldGazeRay, out RaycastHit hitInfo))
                target = hitInfo.collider.gameObject;
        }
        Vector3 targetPos = target ? target.transform.position : Vector3.zero;

        object[] data = new object[]
        {
            // Head
            headPos.x, headPos.y, headPos.z,
            headRot.eulerAngles.x, headRot.eulerAngles.y, headRot.eulerAngles.z,

            // Eye
            eyePos.x, eyePos.y, eyePos.z,
            eyeRot.eulerAngles.x, eyeRot.eulerAngles.y, eyeRot.eulerAngles.z,

            // Eye State
            eyeStateAvailable ? "Yes" : "No",

            // Target
            target ? target.name : "None",
            targetPos.x, targetPos.y, targetPos.z,
            
            // Eye Tracking
            lastEyeState.pupilDiameterLeft, lastEyeState.pupilDiameterRight,
            lastEyeState.eyeballCenterLeft.x, lastEyeState.eyeballCenterLeft.y, lastEyeState.eyeballCenterLeft.z,
            lastEyeState.eyeballCenterRight.x, lastEyeState.eyeballCenterRight.y, lastEyeState.eyeballCenterRight.z,
            lastEyeState.opticalAxisLeft.x, lastEyeState.opticalAxisLeft.y, lastEyeState.opticalAxisLeft.z,
            lastEyeState.opticalAxisRight.x, lastEyeState.opticalAxisRight.y, lastEyeState.opticalAxisRight.z,

            // Eyelid State
            eyelidStateAvailable ? "Yes" : "No",
            lastEyelidState.eyelidAngleTopLeft, lastEyelidState.eyelidAngleTopRight,
            lastEyelidState.eyelidAngleBottomLeft, lastEyelidState.eyelidAngleBottomRight,
            lastEyelidState.eyelidApertureLeft, lastEyelidState.eyelidApertureRight
        };

        return data;
    }
}

