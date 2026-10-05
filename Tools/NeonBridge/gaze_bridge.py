"""Forward Neon gaze-only samples to Unity over localhost UDP.

This intentionally subscribes only to the Companion gaze stream. It never opens,
decodes, or waits for the scene/world-camera stream.
"""
import argparse
import json
import math
import socket
import time
import uuid

from bridge_health import check_sdk, sensor_summary


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--ip", help="Companion IP; otherwise discover on the local network")
    parser.add_argument("--port", type=int, default=9053, help="Unity UDP port (default: 9053)")
    parser.add_argument("--check", action="store_true", help="Check SDK compatibility and exit")
    args = parser.parse_args()

    sdk_version = check_sdk()
    print(f"Neon SDK {sdk_version}: gaze-only bridge ready.", flush=True)
    if args.check:
        return

    from pupil_labs.realtime_api.simple import Device, discover_one_device

    device = Device(address=args.ip, port=8080) if args.ip else discover_one_device(
        max_search_duration_seconds=10
    )
    if device is None:
        raise SystemExit(
            "No Neon found. Put the Companion and computer on the same LAN, "
            "enable Streaming in Companion, or pass --ip."
        )

    stream_id = uuid.uuid4().hex
    sequence = 0
    last_timestamp = 0
    last_diagnostic = 0.0
    try:
        print(
            f"Connected to Neon gaze sensor: {sensor_summary(device.gaze_sensor())}. "
            "Scene camera is not used. Ctrl+C stops this bridge.",
            flush=True,
        )
        with socket.socket(socket.AF_INET, socket.SOCK_DGRAM) as sender:
            while True:
                gaze = device.receive_gaze_datum(timeout_seconds=1)
                now = time.monotonic()
                if gaze is None:
                    if now - last_diagnostic >= 5:
                        print(
                            "diagnostic: gaze=waiting, sensor="
                            + sensor_summary(device.gaze_sensor()),
                            flush=True,
                        )
                        last_diagnostic = now
                    continue

                if not hasattr(gaze, "x") or not hasattr(gaze, "y"):
                    continue
                if gaze.timestamp_unix_ns <= last_timestamp:
                    continue
                if not all(math.isfinite(value) for value in (gaze.x, gaze.y)):
                    continue

                last_timestamp = gaze.timestamp_unix_ns
                sequence += 1
                payload = {
                    "version": 1,
                    "type": "neon_raw_gaze",
                    "streamId": stream_id,
                    "sequence": sequence,
                    "timestampNs": str(gaze.timestamp_unix_ns),
                    "gazeX": float(gaze.x),
                    "gazeY": float(gaze.y),
                    "worn": bool(gaze.worn),
                    "sourceWidth": 1600,
                    "sourceHeight": 1200,
                    "hostSentMs": time.time_ns() / 1e6,
                }
                sender.sendto(
                    json.dumps(payload, allow_nan=False, separators=(",", ":")).encode(),
                    ("127.0.0.1", args.port),
                )
                if now - last_diagnostic >= 5:
                    print(
                        f"diagnostic: gaze=streaming, samples={sequence}, "
                        f"raw=({gaze.x:.1f}, {gaze.y:.1f}), worn={bool(gaze.worn)}",
                        flush=True,
                    )
                    last_diagnostic = now
    finally:
        device.close()


if __name__ == "__main__":
    try:
        main()
    except KeyboardInterrupt:
        pass
    except (ConnectionError, OSError, RuntimeError) as exc:
        raise SystemExit(f"Neon gaze connection failed: {exc}") from exc
