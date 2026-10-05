import struct
import unittest
from unittest.mock import patch
from bridge_health import check_sdk
from pupil_labs.realtime_api.streaming.base import RTSPData, RTSPRawStreamer
from pupil_labs.realtime_api.streaming.gaze import RTSPGazeStreamer


class GazeCompatibilityTests(unittest.IsolatedAsyncioTestCase):
    async def decode(self, raw, timestamp=1791008017.4908113):
        async def source(_self):
            yield RTSPData(raw, timestamp)
        with patch.object(RTSPRawStreamer, "receive", source):
            # Exercise the SDK's length dispatch, not just its data-class parser.
            return [g async for g in RTSPGazeStreamer("rtsp://127.0.0.1/test").receive()]

    async def test_105_byte_gaze_and_monocular_fields(self):
        raw = struct.pack("!ffB24f", 835.2, 409.1, 255, *range(24))
        self.assertEqual(len(raw), 105)
        gaze, = await self.decode(raw)
        self.assertAlmostEqual(gaze.x, 835.2, places=3)
        self.assertAlmostEqual(gaze.y, 409.1, places=3)
        self.assertTrue(gaze.worn)
        self.assertEqual(gaze.mono_left_x, 20)
        self.assertEqual(gaze.mono_right_y, 23)
        self.assertEqual(gaze.timestamp_unix_seconds, 1791008017.4908113)

    async def test_reported_packet(self):
        raw = (b'DP\xdd\x97C\xcc\x8b4\x00@\xb5\xa3\xc0\xc1\xd4\xfa\xc5A\x0b\xf0\xab\xc2E\xf7J=uI\xdb>\x85E\xfb?v\xb3\x03@\x9eY\x8dB\x0c\x14\x85@\xabQ(\xc2\\\x05\xdd\xbe\xb7\xc4\x9b>~j\xa3?fR9'
               + bytes(24) + b'D;\x80zC\x9c6\x03Dpt\xf7D\x0cCW')
        self.assertEqual(len(raw), 105)
        gaze, = await self.decode(raw)
        self.assertAlmostEqual(gaze.x, 835.4623413085938)
        self.assertAlmostEqual(gaze.y, 409.0875244140625)
        self.assertFalse(gaze.worn)

    async def test_legacy_packet_sizes(self):
        for fmt, count in (("!ffB", 0), ("!ffB14f", 14), ("!ffB20f", 20), ("!ffB4f", 4)):
            with self.subTest(format=fmt):
                gaze, = await self.decode(struct.pack(fmt, 12., 34., 255, *([0.] * count)))
                self.assertEqual((gaze.x, gaze.y, gaze.worn), (12., 34., True))

    def test_sdk_preflight(self):
        self.assertEqual(check_sdk(), "1.9.0")
if __name__ == "__main__":
    unittest.main()
