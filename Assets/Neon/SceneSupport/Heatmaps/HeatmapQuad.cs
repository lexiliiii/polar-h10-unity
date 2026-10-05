using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeatmapQuad : MonoBehaviour {

    [SerializeField] private Renderer mRenderer;
    [SerializeField] private int textureResolutionWidth = 1024;
    [SerializeField] private int textureResolutionHeight = 1024;
    [SerializeField] private Gradient colorGradient;
    private int[,] hitCounts;

    void Awake() => ResetHitCounts();

    public void PaintPixels(float uv_x, float uv_y, int radius) {
        int x = Mathf.FloorToInt(uv_x * (textureResolutionWidth - 1));
        int y = Mathf.FloorToInt(uv_y * (textureResolutionHeight - 1));

        int r = radius;
        int r2 = r * r;
        for (int dx = -r; dx <= r; dx++) {
            for (int dy = -r; dy <= r; dy++) {
                int px = x + dx;
                int py = y + dy;

                if (px >= 0 && px < textureResolutionWidth && py >= 0 && py < textureResolutionHeight) {
                    float dist2 = dx * dx + dy * dy;
                    if (dist2 <= r2) {
                        float dist = Mathf.Sqrt(dist2);
                        float weight = 1f - (dist / r);

                        hitCounts[px, py] += Mathf.RoundToInt(weight * 10);
                    }
                }
            }
        }
    }

    public void ApplyHeatmapTexture() {
        int maxVal = 0;
        foreach (int val in hitCounts) {
            if (val > maxVal) maxVal = val;
        }
        if (maxVal == 0) maxVal = 1;

        Texture2D heatmapTex = new Texture2D(textureResolutionWidth, textureResolutionHeight, TextureFormat.RGBA32, false);
        for (int x = 0; x < textureResolutionWidth; x++) {
            for (int y = 0; y < textureResolutionHeight; y++) {
                float norm = (float) hitCounts[x, y] / maxVal;
                Color color = colorGradient.Evaluate(norm);
                heatmapTex.SetPixel(x, y, color);
            }
        }
        heatmapTex.Apply();

        mRenderer.enabled = true;
        MaterialPropertyBlock mpb = new();
        mRenderer.GetPropertyBlock(mpb);
        mpb.SetTexture("_BaseMap", heatmapTex);
        mRenderer.SetPropertyBlock(mpb);

        ResetHitCounts();
    }

    public void ResetHitCounts() {
        hitCounts = new int[textureResolutionWidth, textureResolutionHeight];
    }
}