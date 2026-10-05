using NUnit.Framework;
using UnityEngine;

namespace Module3.Neon.Tests
{
    public sealed class NeonGazeOnlyCursorTests
    {
        [TestCase(800, 600, 0, 0)]
        [TestCase(0, 0, -960, 540)]
        [TestCase(1600, 1200, 960, -540)]
        public void ConvertsTopLeftNeonPixelsToCenteredCanvas(float x, float y, float expectedX, float expectedY)
        {
            Assert.True(NeonGazeOnlyCursor.TryConvert(x, y, 1600, 1200,
                new Rect(-960, -540, 1920, 1080), out Vector2 result));
            Assert.AreEqual(expectedX, result.x, 0.001f);
            Assert.AreEqual(expectedY, result.y, 0.001f);
        }

        [Test]
        public void RejectsInvalidAndOutOfBoundsSamples()
        {
            var canvas = new Rect(-960, -540, 1920, 1080);
            Assert.False(NeonGazeOnlyCursor.TryConvert(-1, 20, 1600, 1200, canvas, out _));
            Assert.False(NeonGazeOnlyCursor.TryConvert(20, 1201, 1600, 1200, canvas, out _));
            Assert.False(NeonGazeOnlyCursor.TryConvert(float.NaN, 20, 1600, 1200, canvas, out _));
            Assert.False(NeonGazeOnlyCursor.TryConvert(20, 20, 0, 1200, canvas, out _));
        }
    }
}
