// BS-decomp / Block Strike 4.1.0 — legacy lightmap binder.
//
// The exported scenes still carry Unity 4's lightmap data:
//
//   * LightmapSettings:  m_Lightmaps / m_IndirectLightmap / m_BakedColorSpace
//   * every Renderer:    m_LightmapIndex + m_LightmapTilingOffset
//
// Unity 5 moved the baked array out of the scene into the LightingData asset
// and widened the index sentinels, so a modern editor
//   1. reads no lightmap array from the scene (`LightmapSettings.lightmaps`
//      stays empty), and
//   2. treats the scene as "never baked", which also drops the per-renderer
//      `lightmapIndex` / `lightmapScaleOffset` that the file still contains.
//
// Both halves are restored here at load time — the only supported way to use a
// pre-baked lightmap set without a LightingData asset. Data is generated from
// the scenes themselves by `tools/install_lightmap_binder.py` and checked by
// `tools/verify_lightmaps.py`.
//
// `ExecuteAlways`, so the Scene view shows the same lighting as the game, and
// builds keep working because the data is serialised in the scene.

using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public class BSLegacyLightmaps : MonoBehaviour
{
    [Tooltip("Baked lightmaps of this scene, in their original order (index 0 = m_LightmapIndex 0).")]
    public Texture2D[] lightmapsFar;

    [Tooltip("Unity 4 near lightmaps (dual lightmapping). The 4.1.0 export only ships far maps.")]
    public Texture2D[] lightmapsNear;

    [Tooltip("Renderers that were lightmapped in the original scene.")]
    public Renderer[] renderers;

    [Tooltip("m_LightmapIndex of each entry in `renderers`.")]
    public int[] lightmapIndices;

    [Tooltip("m_LightmapTilingOffset of each entry in `renderers` (scale.xy, offset.zw).")]
    public Vector4[] lightmapScaleOffsets;

    private void OnEnable()
    {
        Apply();
    }

    private void Start()
    {
        Apply();
    }

    public void Apply()
    {
        ApplyLightmapArray();
        ApplyRenderers();
    }

    private void ApplyLightmapArray()
    {
        if (lightmapsFar == null || lightmapsFar.Length == 0)
        {
            return;
        }

        LightmapData[] current = LightmapSettings.lightmaps;
        bool needsUpdate = current == null || current.Length != lightmapsFar.Length;
        if (!needsUpdate)
        {
            for (int i = 0; i < current.Length; i++)
            {
                if (current[i] == null || current[i].lightmapColor != lightmapsFar[i])
                {
                    needsUpdate = true;
                    break;
                }
            }
        }
        if (!needsUpdate)
        {
            return;
        }

        LightmapData[] data = new LightmapData[lightmapsFar.Length];
        for (int i = 0; i < lightmapsFar.Length; i++)
        {
            LightmapData entry = new LightmapData();
            entry.lightmapColor = lightmapsFar[i];
            data[i] = entry;
        }
        LightmapSettings.lightmaps = data;
        LightmapSettings.lightmapsMode = LightmapsMode.NonDirectional;
    }

    private void ApplyRenderers()
    {
        if (renderers == null || lightmapIndices == null || lightmapScaleOffsets == null)
        {
            return;
        }
        int count = Mathf.Min(renderers.Length, Mathf.Min(lightmapIndices.Length, lightmapScaleOffsets.Length));
        for (int i = 0; i < count; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }
            if (renderer.lightmapIndex != lightmapIndices[i])
            {
                renderer.lightmapIndex = lightmapIndices[i];
            }
            if (renderer.lightmapScaleOffset != lightmapScaleOffsets[i])
            {
                renderer.lightmapScaleOffset = lightmapScaleOffsets[i];
            }
        }
    }
}
