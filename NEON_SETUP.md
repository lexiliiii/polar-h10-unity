# Pupil Labs Neon integration

This project has two separate Neon paths. For ordinary Neon glasses and a desktop
Unity Game view, use the **gaze-only cursor**. The retained **Neon XR Core** scene is
only for the headset-mounted Neon XR workflow.

## Gaze-only desktop cursor (default)

### Data flow

```text
Neon glasses
  -> Neon Companion
  -> real-time gaze sensor (pupil_labs_realtime_api 1.9.0)
  -> Tools/NeonBridge/gaze_bridge.py
  -> JSON datagrams on localhost UDP 9053
  -> NeonGazeOnlyReceiver (background receive thread)
  -> NeonGazeOnlyCursor (Unity main thread)
  -> red Canvas cursor
```

The bridge requests gaze samples only. It does not request, decode, or forward the
scene/world-camera stream.

Each UDP message contains:

```json
{
  "timestamp": 0,
  "x": 800.0,
  "y": 600.0,
  "worn": true,
  "sourceWidth": 1600,
  "sourceHeight": 1200
}
```

`x` and `y` are raw Neon gaze positions in the top-left-origin 1600 x 1200 scene
camera coordinate system. Unity scales them to the current Canvas, converts the
origin to the Canvas centre, and flips Y. The cursor hides when samples become
invalid or stale. This is not a calibration between the glasses and a physical
monitor; accurate monitor alignment requires a separate calibration or mapping
procedure.

### Setup

1. Open the target scene outside Play mode.
2. Choose **Tools > Neon > Add Gaze-Only UI Cursor to Current Scene**.
3. Save the scene.
4. Put the computer and Companion device on the same reachable network and enable
   streaming in Neon Companion.
5. Enter Play mode and watch the Game view.

The menu creates one `Neon Gaze-Only Cursor` object containing the bridge process,
asynchronous UDP receiver, main-thread coordinate converter, Canvas, and red cursor.

### Bridge diagnostics

Unity normally starts `Tools/NeonBridge/gaze_bridge.py`. To run it manually:

```bash
cd Tools/NeonBridge
source .venv/bin/activate
python3 bridge.py
```

The environment must contain `pupil-labs-realtime-api==1.9.0`; see
`Tools/NeonBridge/requirements.txt`. Healthy output includes `gaze-only bridge
ready`, `Scene camera is not used`, `sensor=connected`, and increasing sample
counts. Stop a manually launched bridge before allowing Unity to start another.

### Troubleshooting

- If samples increase but no cursor appears, confirm that the scene was saved after
  running the setup menu and inspect `[NeonGaze]` Unity Console messages.
- `worn=False` hides the cursor by design; wear the glasses correctly.
- For no connection, check Companion streaming, LAN reachability, the firewall,
  and the Python environment.
- Only one Unity receiver should bind UDP `9053`, and only one bridge should run.
- Physical-screen cursor offset is expected without monitor calibration.

### Recording and export

Unity is responsible for live visualization and experiment events. For the complete
research dataset, record with Neon Companion and export through Pupil Cloud. That
preserves official timestamps, gaze fields, metadata, and related streams. Unity's
UDP cursor messages are not a replacement for the official recording/export path.

## Retained Neon XR Core scene

`Assets/_Scenes/EyeTrackingTestRoom.unity` and `Assets/Neon/SceneSupport` remain for
the separate, headset-mounted Neon XR workflow. They are not required for the
desktop cursor. Choose **Tools > Neon > Open XR Core Test Scene** to inspect that
path; it uses the official Unity provider and a 3D gaze ray with different mounting,
calibration, network, and build requirements.

## Key files

- `Tools/NeonBridge/gaze_bridge.py`: reads live gaze and sends JSON/UDP.
- `Tools/NeonBridge/bridge.py`: compatibility entry point for manual launch.
- `Assets/Neon/Runtime/NeonGazeBridgeProcess.cs`: starts and stops the bridge.
- `Assets/Neon/Runtime/NeonGazeOnlyReceiver.cs`: asynchronous UDP receiver.
- `Assets/Neon/Runtime/NeonGazeOnlyCursor.cs`: validation and Canvas conversion.
- `Assets/Neon/Editor/NeonGazeOnlyMenu.cs`: creates the receiver and UI cursor.
- `Assets/Neon/Tests/Editor/NeonGazeOnlyCursorTests.cs`: conversion checks.
