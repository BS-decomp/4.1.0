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

    private bool warnedUnbound;
    private MaterialPropertyBlock block;

#if UNITY_EDITOR
    private void Update()
    {
        // In edit mode Unity's lighting system clears LightmapSettings.lightmaps
        // whenever it decides the scene is "not baked", so keep re-applying.
        if (!Application.isPlaying)
        {
            Apply();
        }
    }
#endif

    public void Apply()
    {
        bool bound = ApplyLightmapArray();
        ApplyRenderers(bound);
    }

    /// <summary>Returns true when the baked textures really are bound.</summary>
    private bool ApplyLightmapArray()
    {
        if (lightmapsFar == null || lightmapsFar.Length == 0)
        {
            return false;
        }
        for (int i = 0; i < lightmapsFar.Length; i++)
        {
            if (lightmapsFar[i] == null)
            {
                return false;   // a missing texture would paint everything black
            }
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
        if (needsUpdate)
        {
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

        LightmapData[] check = LightmapSettings.lightmaps;
        return check != null && check.Length == lightmapsFar.Length &&
               check[0] != null && check[0].lightmapColor != null;
    }

    /// <summary>Restores lightmapIndex/lightmapScaleOffset. When the textures are
    /// NOT bound, the indices are cleared instead: a renderer that claims a
    /// lightmap the engine does not have samples black, which is exactly how a
    /// map turns pitch black. Unlit-but-correct beats black.</summary>
    private void ApplyRenderers(bool bound)
    {
        if (renderers == null || lightmapIndices == null || lightmapScaleOffsets == null)
        {
            return;
        }
        int count = Mathf.Min(renderers.Length, Mathf.Min(lightmapIndices.Length, lightmapScaleOffsets.Length));
        int available = bound && lightmapsFar != null ? lightmapsFar.Length : 0;

        if (!bound && !warnedUnbound)
        {
            warnedUnbound = true;
            Debug.LogWarning("[BS Lightmaps] baked lightmaps are not bound (missing texture reference?) — " +
                             "renderers keep rendering unlit instead of black. Scene: " + gameObject.scene.name);
        }
        if (bound)
        {
            warnedUnbound = false;
        }

        if (block == null)
        {
            block = new MaterialPropertyBlock();
        }

        for (int i = 0; i < count; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }
            int index = lightmapIndices[i];

            // Primary path: hand the baked map to the renderer ourselves.
            // Unity overwrites Renderer.lightmapIndex in the editor whenever it
            // rebuilds lighting for a scene it considers "not baked" (no
            // LightingData asset) — which is every scene of this export — so the
            // official binding alone silently does nothing outside play mode.
            // A MaterialPropertyBlock is never touched by that logic and works
            // identically in edit mode, play mode and builds.
            if (bound && index >= 0 && index < available && lightmapsFar[index] != null)
            {
                renderer.GetPropertyBlock(block);
                block.SetTexture("_BSLightmap", lightmapsFar[index]);
                block.SetVector("_BSLightmapST", lightmapScaleOffsets[i]);
                renderer.SetPropertyBlock(block);
            }
            if (index >= available)
            {
                if (renderer.lightmapIndex != 65535)
                {
                    renderer.lightmapIndex = 65535;
                }
                continue;
            }
            if (renderer.lightmapIndex != index)
            {
                renderer.lightmapIndex = index;
            }
            if (renderer.lightmapScaleOffset != lightmapScaleOffsets[i])
            {
                renderer.lightmapScaleOffset = lightmapScaleOffsets[i];
            }
        }
    }
}
