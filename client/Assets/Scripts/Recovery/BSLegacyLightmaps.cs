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

using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public class BSLegacyLightmaps : MonoBehaviour
{
    [Tooltip("Baked lightmaps of this scene, in their original order (index 0 = m_LightmapIndex 0).")]
    public Texture2D[] lightmapsFar;

    [Tooltip("Unity 4 near lightmaps (dual lightmapping). The 4.1.0 export only ships far maps.")]
    public Texture2D[] lightmapsNear;

    [Tooltip("Names of the renderers that were lightmapped. Scene-local component references " +
             "({fileID}) are dropped when Unity imports these Unity 4 scenes, so the binder matches " +
             "renderers by name + world position instead.")]
    public string[] renderNames;

    [Tooltip("World position of each entry in `renderNames` (used to tell same-named objects apart).")]
    public Vector3[] renderPositions;

    [Tooltip("Mesh name of each entry in `renderNames`; the final tie-breaker when name and position " +
             "are identical (the de-batched meshes are unique per renderer).")]
    public string[] renderMeshes;

    [Tooltip("m_LightmapIndex of each entry in `renderNames`.")]
    public int[] lightmapIndices;

    [Tooltip("m_LightmapTilingOffset of each entry in `renderNames` (scale.xy, offset.zw).")]
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
    /// <summary>Restores the baked lighting of every renderer listed in
    /// `renderNames`/`renderPositions`. The map is delivered through a
    /// MaterialPropertyBlock, because Unity overwrites Renderer.lightmapIndex
    /// outside play mode for scenes it considers "not baked" — which, without a
    /// LightingData asset, is every scene of this export.</summary>
    private void ApplyRenderers(bool bound)
    {
        if (renderNames == null || renderPositions == null || lightmapIndices == null ||
            lightmapScaleOffsets == null)
        {
            return;
        }
        int count = Mathf.Min(renderNames.Length,
            Mathf.Min(renderPositions.Length, Mathf.Min(lightmapIndices.Length, lightmapScaleOffsets.Length)));
        if (count == 0)
        {
            return;
        }
        int available = bound && lightmapsFar != null ? lightmapsFar.Length : 0;

        if (!bound)
        {
            if (!warnedUnbound)
            {
                warnedUnbound = true;
                Debug.LogWarning("[BS Lightmaps] baked lightmaps are not bound (missing texture?) — " +
                                 "renderers stay unlit instead of black. Scene: " + gameObject.scene.name);
            }
            return;
        }
        warnedUnbound = false;

        if (block == null)
        {
            block = new MaterialPropertyBlock();
        }

        Dictionary<string, List<Renderer>> byName = new Dictionary<string, List<Renderer>>();
        Renderer[] all = FindObjectsOfType<Renderer>();
        for (int i = 0; i < all.Length; i++)
        {
            List<Renderer> list;
            if (!byName.TryGetValue(all[i].name, out list))
            {
                list = new List<Renderer>();
                byName[all[i].name] = list;
            }
            list.Add(all[i]);
        }

        HashSet<Renderer> taken = new HashSet<Renderer>();
        int applied = 0;
        for (int i = 0; i < count; i++)
        {
            List<Renderer> candidates;
            if (!byName.TryGetValue(renderNames[i], out candidates))
            {
                continue;
            }
            string wantedMesh = renderMeshes != null && i < renderMeshes.Length ? renderMeshes[i] : null;

            Renderer best = null;
            float bestDistance = 0.05f;      // 5 cm is far below the size of any map object
            for (int c = 0; c < candidates.Count; c++)
            {
                Renderer candidate = candidates[c];
                if (taken.Contains(candidate))
                {
                    continue;
                }
                if (!string.IsNullOrEmpty(wantedMesh) && MeshNameOf(candidate) != wantedMesh)
                {
                    continue;
                }
                float distance = Vector3.Distance(candidate.transform.position, renderPositions[i]);
                if (distance <= bestDistance)
                {
                    bestDistance = distance;
                    best = candidate;
                }
            }
            if (best == null)
            {
                continue;
            }
            taken.Add(best);

            int index = lightmapIndices[i];
            if (index < 0 || index >= available || lightmapsFar[index] == null)
            {
                continue;
            }

            best.GetPropertyBlock(block);
            block.SetTexture("_BSLightmap", lightmapsFar[index]);
            block.SetVector("_BSLightmapST", lightmapScaleOffsets[i]);
            best.SetPropertyBlock(block);

            // Also try the official path; harmless when Unity ignores it.
            if (best.lightmapIndex != index)
            {
                best.lightmapIndex = index;
            }
            if (best.lightmapScaleOffset != lightmapScaleOffsets[i])
            {
                best.lightmapScaleOffset = lightmapScaleOffsets[i];
            }
            applied++;
        }

        if (applied != lastApplied)
        {
            lastApplied = applied;
            Debug.Log("[BS Lightmaps] " + gameObject.scene.name + ": baked lighting applied to " +
                      applied + "/" + count + " renderer(s).");
        }
    }

    private int lastApplied = -1;

    private static string MeshNameOf(Renderer renderer)
    {
        MeshFilter filter = renderer.GetComponent<MeshFilter>();
        if (filter != null && filter.sharedMesh != null)
        {
            return filter.sharedMesh.name;
        }
        SkinnedMeshRenderer skinned = renderer as SkinnedMeshRenderer;
        if (skinned != null && skinned.sharedMesh != null)
        {
            return skinned.sharedMesh.name;
        }
        return string.Empty;
    }
}
