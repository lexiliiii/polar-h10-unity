"""Default Neon bridge entry point: gaze only, no scene/world video."""
from gaze_bridge import main


if __name__ == "__main__":
    try:
        main()
    except KeyboardInterrupt:
        pass
    except (ConnectionError, OSError, RuntimeError) as exc:
        raise SystemExit(str(exc)) from exc
