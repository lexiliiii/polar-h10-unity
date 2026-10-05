using UnityEngine;

public static class GeneralUtils {

    /// <returns> Absolute distance from 'a' to 'b'; </returns>
    public static float Distance(this float a, float b) {
        return Mathf.Abs(a - b);
    }

    /// <returns> Quotient of 'a' and 'b', or the provided fallback parameter (Mathf.Infinity by default) if 'b' is 0 </returns>
    public static float SafeDivide(this float a, float b, float fallback = Mathf.Infinity) {
        return b == 0 ? fallback : a / b;
    }

    /// <returns> Quotient of 'a' and 'b', or zero if 'b' is zero </returns>
    public static float SafeDivideZero(this float a, float b) {
        return b == 0 ? 0 : a / b;
    }

    public static float InverseLerp3D(Vector3 start, Vector3 end, Vector3 value) {
        Vector3 se = end - start;
        Vector3 sv = value - start;

        float seSq = Vector3.Dot(se, se);

        if (seSq == 0f)
            return 0f;

        return Vector3.Dot(sv, se) / seSq;
    }
}