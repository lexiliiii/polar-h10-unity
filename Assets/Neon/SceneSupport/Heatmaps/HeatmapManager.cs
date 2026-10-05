using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeatmapManager : MonoBehaviour
{
    [SerializeField] private HeatmapQuad[] heatmapQuads;

    public void ApplyHeatmapTextures() {
        foreach (HeatmapQuad quad in heatmapQuads) {
            quad.ApplyHeatmapTexture();
        }
    }
}
