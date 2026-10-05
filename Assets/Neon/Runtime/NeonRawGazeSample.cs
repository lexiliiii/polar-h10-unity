using System;

namespace Module3.Neon
{
    [Serializable]
    public sealed class NeonRawGazeSample
    {
        public int version;
        public string type;
        public string streamId;
        public long sequence;
        public string timestampNs;
        public float gazeX;
        public float gazeY;
        public bool worn;
        public int sourceWidth;
        public int sourceHeight;
        public double hostSentMs;
    }
}
