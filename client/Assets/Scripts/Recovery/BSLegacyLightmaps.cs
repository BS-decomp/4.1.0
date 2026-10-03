// BS-decomp / Block Strike 4.1.0 — legacy lightmap binder.
//
// Why this component exists
// -------------------------
// The exported scenes still carry Unity 4's `LightmapSettings`:
//
//     m_Lightmaps:
//     - m_Lightmap: {fileID: 2800000, guid: ..., type: 3}
//       m_IndirectLightmap: {fileID: 0}
//     m_LightmapsMode: 0
//     m_BakedColorSpace: 0
//     m_UseDualLightmapsInForward: 0
//
// Unity 5 moved the baked lightmap array out of the scene and into the
// LightingData asset, and renamed the remaining fields, so a modern editor
// silently ignores that block: `LightmapSettings.lightmaps` ends up empty, the
// LIGHTMAP_ON keyword is never enabled and every map renders flat — even though
// each renderer still has a valid `m_LightmapIndex` and `m_LightmapTilingOffset`
// and the baked textures are all present in the project.
//
// This component re-binds them at load time, which is the only supported way to
// use pre-baked lightmaps without a LightingData asset. It runs in the editor
// too (`ExecuteAlways`), so the Scene view looks like the game.
//
// The data is generated from the scenes themselves by
// `tools/install_lightmap_binder.py`; `tools/verify_lightmaps.py` checks it.

using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public class BSLegacyLightmaps : MonoBehaviour
{
    [Tooltip("Baked lightmaps of this scene, in their original order (index 0 = m_LightmapIndex 0).")]
    public Texture2D[] lightmapsFar;

    [Tooltip("Unity 4 near lightmaps (dual lightmapping). The 4.1.0 export only ships far maps.")]
    public Texture2D[] lightmapsNear;

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
        if (lightmapsFar == null || lightmapsFar.Length == 0)
        {
            return;
        }

        LightmapData[] data = new LightmapData[lightmapsFar.Length];
        for (int i = 0; i < lightmapsFar.Length; i++)
        {
            LightmapData entry = new LightmapData();
            entry.lightmapColor = lightmapsFar[i];
            if (lightmapsNear != null && i < lightmapsNear.Length && lightmapsNear[i] != null)
            {
                // Unity 4 "near" maps have no modern equivalent; keeping the
                // reference here documents that the slot existed.
                entry.lightmapDir = null;
            }
            data[i] = entry;
        }

        LightmapSettings.lightmaps = data;
        LightmapSettings.lightmapsMode = LightmapsMode.NonDirectional;
    }
}
