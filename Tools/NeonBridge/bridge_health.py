"""Small compatibility helpers shared by the gaze-only bridge."""
from importlib.metadata import version


def sensor_summary(sensor):
    if sensor is None:
        return "not_reported"
    return ("connected" if sensor.connected else "disconnected") + (
        "/stream_error" if sensor.stream_error else ""
    )


def check_sdk():
    try:
        from pupil_labs.realtime_api.streaming import gaze
        from pupil_labs.realtime_api.simple import Device  # noqa: F401
        if not hasattr(gaze, "EyestateEyelidDualMonoGazeData"):
            raise ImportError("SDK has no 105-byte gaze support")
    except ImportError as exc:
        raise RuntimeError(
            "Incompatible Neon SDK. Reinstall requirements.txt and restart. " + str(exc)
        ) from exc
    return version("pupil-labs-realtime-api")
