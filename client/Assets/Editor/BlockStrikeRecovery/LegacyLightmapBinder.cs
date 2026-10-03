// Block Strike 608 recovery: re-bind the Unity 4 baked lightmap that Unity 5.6 dropped.
//
// Unity 4 stored lightmap links in each scene and renderer. The recovered Unity 5.6 scenes
// have no LightingData asset, so this component restores the runtime/editor state from the
// original LightmapFar-0.png and the renderer IDs recorded in the recovery manifests.
//
// The geometry-recovery pass has already put each renderer's atlas rectangle into UV2 and
// sets renderer.lightmapScaleOffset to identity. Do not apply the original atlas transform
// here as well: that would scale/offset the UVs twice.
//
// No bake is performed. This component only assigns the source lightmap texture and the
// renderers that used it. Written for the Unity 5.6 C# compiler.
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
[DisallowMultipleComponent]
[AddComponentMenu("Block Strike Recovery/Legacy Lightmap Binder")]
public class LegacyLightmapBinder : MonoBehaviour
{
    [Tooltip("The recovered LightmapFar-0.png texture for this scene.")]
    public Texture2D lightmapColor;

    [Tooltip("Renderers which used lightmap index 0 in the original 608 scene. The recovered meshes already contain the atlas transform in UV2.")]
    public Renderer[] renderers = new Renderer[0];

    [Tooltip("Name of the source geometry manifest (informational).")]
    public string sourceManifest = "";

    const int NoLightmap = 65535;
    static readonly Vector4 IdentityScaleOffset = new Vector4(1f, 1f, 0f, 0f);

    void OnEnable()
    {
        Apply();
#if UNITY_EDITOR
        // Unity may apply scene settings after OnEnable while opening a scene. Re-apply once
        // on the next editor tick so the recovered binding is the final edit-mode state.
        EditorApplication.delayCall += DelayedApply;
#endif
    }

    void Start()
    {
        Apply();
    }

    void OnDisable()
    {
        Clear();
    }

    void OnValidate()
    {
#if UNITY_EDITOR
        // Avoid touching the render pipeline while Unity is validating serialized fields.
        EditorApplication.delayCall += DelayedApply;
#endif
    }

#if UNITY_EDITOR
    void DelayedApply()
    {
        if (this == null || !isActiveAndEnabled) return;
        Apply();
    }
#endif

    [ContextMenu("Apply legacy lightmap")]
    public void Apply()
    {
        if (lightmapColor == null || renderers == null || renderers.Length == 0) return;

        LightmapData data = new LightmapData();
        data.lightmapColor = lightmapColor;
        LightmapSettings.lightmapsMode = LightmapsMode.NonDirectional;
        LightmapSettings.lightmaps = new LightmapData[] { data };

        int missing = 0;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
            {
                missing++;
                continue;
            }
            renderer.lightmapIndex = 0;
            renderer.lightmapScaleOffset = IdentityScaleOffset;
        }

        if (missing > 0)
            Debug.LogWarning("[BS608 Lightmaps] " + missing + " renderer reference(s) are missing in " + gameObject.scene.name + ".", this);
    }

    [ContextMenu("Clear legacy lightmap")]
    public void Clear()
    {
        if (renderers != null)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer != null) renderer.lightmapIndex = NoLightmap;
            }
        }
        // LightmapSettings is global. Do not wipe another additive scene's map if it has
        // already replaced ours; clear the global slot only when this component still owns it.
        LightmapData[] current = LightmapSettings.lightmaps;
        if (current != null && current.Length == 1 && current[0] != null &&
            current[0].lightmapColor == lightmapColor)
            LightmapSettings.lightmaps = new LightmapData[0];
    }
}
