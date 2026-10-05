# ExoConstruction

Unity 2022.3.56f1 project containing two independent live-sensor integrations:

- **Pupil Labs Neon:** live gaze-only samples drive a red UI cursor in the Unity
  Game view. Scene/world-camera video is not requested.
- **Polar H10:** live EXG RMS values drive the body-region dashboard.

## Neon quick start

1. Open the target scene outside Play mode.
2. Choose **Tools > Neon > Add Gaze-Only UI Cursor to Current Scene** and save.
3. Put the Neon Companion device and computer on the same reachable network and
   enable streaming in Neon Companion.
4. Enter Play mode. Unity starts the local Python bridge automatically and listens
   on UDP port `9053`.

The cursor represents raw gaze coordinates in Neon's 1600 x 1200 scene-camera
coordinate space, scaled to the Canvas with the Y axis inverted. It is a live
visualization, not a calibrated physical-monitor mapping. See
[Neon integration](NEON_SETUP.md) for setup, data flow, troubleshooting, and the
separate XR test scene.

## Polar H10 quick start

Start the supplied bridge:

```bash
node Tools/PolarH10Bridge/server.mjs
```

Open `http://localhost:8080`, connect the H10, enable EXG, and then choose
**Tools > Polar H10 > Open Dashboard Scene** in Unity. See
[Polar H10 integration](POLAR_H10_INTEGRATION.md) for the complete workflow.

The two integration documents above are the canonical project instructions.
README files under `node_modules`, imported packages, or Asset Store assets belong
to their dependencies and are intentionally left unchanged.
