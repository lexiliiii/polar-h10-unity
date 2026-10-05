# Polar H10 focused 3D visualization

## What was inspected

- Unity 2022.3.56f1; existing URP 14.0.11. No packages were added or upgraded.
- `Tools/PolarH10Visualizer/src/PolarH10VisualizerRow.ts` acquires Bluetooth EXG, applies the selected filter and calculates rolling RMS: square each filtered sample, maintain a 26-sample window at 130 Hz, then take the square root. Warmup values are not forwarded as valid readings.
- `LslBridge.ts` sends schema and samples over WebSocket to the local Node adapter (`Tools/PolarH10Bridge/server.mjs`, default port 8765). Despite the transport class name, this Unity dashboard does not use the legacy `ReceiveLSL` component.
- `protocol.mjs` accepts high-pass EXG RMS with a 200 ms window and one of four region IDs. It sends versioned JSON on localhost UDP 9050.
- `PolarRmsReceiver.cs` validates metadata, timestamps and sequence, preserves numerical RMS, clears old assignments when a sensor moves, and expires readings after two seconds. Its normalization only affects display sensitivity. This file and the acquisition/bridge code were not modified.
- Previously, `PolarRmsDashboard.cs` generated a flat silhouette and covered a 1280 × 800 logical canvas with duplicate readouts.
- The browser also contains accelerometer calculations, but the existing Unity adapter does not transport those samples. EXG amplitude cannot specify joint angles, displacement, direction or whether a joint is moving.

## Changes and removal scope

Modified:
- `Assets/PolarH10/Runtime/PolarRmsDashboard.cs`: compact 344 × 274 logical-pixel panel anchored 12 pixels from the top-right; four RMS values/statuses/bars; collapsible calibration and panel. Scales down only when needed to fit the window.
- `Assets/PolarH10/Editor/PolarDashboardMenu.cs`: repeatable **Configure 3D Dashboard** command uses the existing FBX and URP shaders, saves material references, lighting and the scene, and enables only the Polar scene in Build Settings.
- `Assets/PolarH10/Scenes/PolarRmsDashboard.unity`: wired model/material references and lighting.
- `ProjectSettings/EditorBuildSettings.asset`: legacy training scenes disabled; Polar is the only enabled scene.
- `ProjectSettings/ProjectSettings.asset`: background processing enabled so focusing the acquisition browser does not pause the Unity player.
- `POLAR_H10_INTEGRATION.md`: links to these updated instructions.

Added:
- `Assets/PolarH10/Runtime/PolarBodyVisualization.cs` and meta: instantiates the existing human, disables imported animation/cameras/lights, hides four imported twist-helper meshes, creates exactly four signal markers, and frames a posterior camera responsively.
- `Assets/PolarH10/PolarBody.mat`, `PolarMarker.mat` and metas: neutral URP body material and unlit colored markers; serialized shader references avoid relying on runtime shader discovery in builds.
- `Assets/PolarH10/Tests/Editor/PolarBodyTests.cs` and meta: real Play Mode validation of model loading, four independent amplitude levels, live zero and stale marker behavior.
- This document.

Removed from the application: procedural 2D silhouette, full-screen opaque dashboard, duplicate large region cards, and legacy construction/training scene flow. No asset files were deleted. Legacy scripts/assets/packages remain because deleting packages alone would break their references. Their scenes are excluded from the focused build. Pre-existing `.gitignore` edits and untracked Asset Store content were preserved.

## Human model and mapping

Reuses `Assets/Art/Humans/WorkerTPose.fbx`. No new model is needed. The FBX has a generic Biped skeleton; no Humanoid conversion or animation rigging dependency is required.

| Incoming region ID | Existing bone anchor |
| --- | --- |
| LShoulder | Bip01 L UpperArm (shoulder joint) |
| RShoulder | Bip01 R UpperArm (shoulder joint) |
| MiddleSpine | Bip01 Spine1 |
| LowerSpine | Bip01 Spine |

The camera derives posterior orientation from anatomical left/right. Markers retain each bone's projected position and are offset toward the camera onto the posterior bounding plane for visibility; these are region indicators, not skin deformation or precise electrode placement. Bone names can be changed in the visualization component if replacing the model. A missing model/bone produces a visible configuration message rather than inventing an anatomy asset.

Live markers grow and change from teal to orange according to the existing normalized RMS. Gray means waiting/stale. Live zero is teal at minimum size. The model stays in its reference pose; no bones are rotated and no synthetic body animation is introduced. All limbs are context only, not tracking outputs.

## Run

1. Open this project in **Unity 2022.3.56f1**.
2. Choose **Tools → Polar H10 → Open Dashboard Scene**. Scene references are already configured; no manual asset assignment/import is needed. **Configure 3D Dashboard** recreates the setup if necessary (it also disables other build scenes).
3. From the project root, run `node Tools/PolarH10Bridge/server.mjs`.
4. Open `http://localhost:8080` in a browser supporting Web Bluetooth, connect the H10, enable EXG, choose **highpass**, **200 ms RMS**, and the correct Bodypart: LShoulder, RShoulder, MiddleSpine or LowerSpine. Keep the acquisition tab open. Recording is not required.
5. Press Play in Unity. The human is the main view; the monitoring panel stays top-right. Expand **Range** to edit sensitivity or view receiver status. The default range is 0–120 µV; it is a display range, not a physiological threshold.
6. One sensor supplies one location at a time. Four simultaneous regions require four devices with distinct body-part assignments. Use one acquisition tab and one receiver/player.
7. Disconnect or stop EXG: the corresponding region should become gray within two seconds. Reassign a sensor: its previous region must clear. Resize Game view to check landscape/portrait framing. Very small windows shrink panel text, so use at least 640 × 480 for practical monitoring.
8. Build through standard Build Settings (only Polar is enabled), or **Tools → Polar H10 → Build Windows Dashboard** when the Windows build module is installed. No player build was verified during this work.

## Verification and limitations

Unity 2022.3.56f1 compiled successfully. All four Unity tests and all seven bridge/browser tests passed. A GPU-rendered preview of the reused human and markers was inspected; physical hardware and a standalone build were not tested. Receiver tests cover validation, duplicates, unsupported locations, normalization and reassignment. The new test enters Play Mode and uses synthetic versioned packets; it does not claim hardware acquisition verification. Tests are under **Window → General → Test Runner → EditMode**, filter `Module3.PolarH10.Tests`.

The bridge suite runs with `node --test test/*.test.mjs` inside `Tools/PolarH10Bridge`. The copied visualizer `node_modules` contains Windows esbuild; on macOS its upstream tests require a matching macOS executable or a platform-correct install. Validation used a temporary matching macOS executable without changing project dependencies.

Physical Bluetooth hardware, electrode placement and clinical meaning of EXG were not verified. This dashboard displays signal amplitude and cannot establish movement direction or full-body pose. The human is a clothed worker asset, not an anatomical model. Existing legacy packages may still emit import/native-plugin warnings; they were not removed blindly.
