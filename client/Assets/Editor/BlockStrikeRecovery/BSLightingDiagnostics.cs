// BS-decomp / Block Strike 4.1.0 — lighting diagnostics.
//
// Tools > Block Strike > Diagnose lighting
//
// Prints everything that decides whether a baked lightmap reaches the screen,
// for the scene that is currently open (works in edit mode and in play mode):
// the bound lightmap array, the binder, per-renderer indices, the shader of the
// lightmapped materials, whether that shader has the LIGHTMAP_ON variant, the
// UV1 channel of the meshes and the project's colour space / lightmap encoding.
//
// It changes nothing — it only reports, so a broken map can be described
// precisely instead of "it looks flat".

using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class BSLightingDiagnostics
{
    [MenuItem("Tools/Block Strike/Diagnose lighting")]
    public static void Run()
    {
        StringBuilder log = new StringBuilder();
        log.AppendLine("=== Block Strike lighting diagnostics ===");
        log.AppendLine("scene: " + (Application.isPlaying ? Application.loadedLevelName : "edit mode"));
        log.AppendLine("colour space: " + PlayerSettings.colorSpace +
                       " | lightmap encoding (Android): " +
                       PlayerSettings.GetLightmapEncodingQualityForPlatformGroup(BuildTargetGroup.Android));

        LightmapData[] maps = LightmapSettings.lightmaps;
        log.AppendLine("LightmapSettings.lightmaps: " + (maps == null ? 0 : maps.Length) +
                       " | mode: " + LightmapSettings.lightmapsMode);
        if (maps != null)
        {
            for (int i = 0; i < maps.Length; i++)
            {
                Texture tex = maps[i] != null ? maps[i].lightmapColor : null;
                log.AppendLine("  [" + i + "] " + (tex == null ? "<null>" : tex.name + " " +
                    (tex is Texture2D ? ((Texture2D)tex).width + "x" + ((Texture2D)tex).height : "?")));
            }
        }

        BSLegacyLightmaps binder = Object.FindObjectOfType<BSLegacyLightmaps>();
        if (binder == null)
        {
            log.AppendLine("binder: MISSING — run tools/install_lightmap_binder.py --apply");
        }
        else
        {
            log.AppendLine("binder: " + (binder.lightmapsFar == null ? 0 : binder.lightmapsFar.Length) +
                           " texture(s), " + (binder.renderers == null ? 0 : binder.renderers.Length) +
                           " renderer(s) recorded");
            if (binder.lightmapsFar != null)
            {
                for (int i = 0; i < binder.lightmapsFar.Length; i++)
                {
                    log.AppendLine("  far[" + i + "] = " +
                                   (binder.lightmapsFar[i] == null ? "<missing reference>" : binder.lightmapsFar[i].name));
                }
            }
        }

        Renderer[] renderers = Object.FindObjectsOfType<Renderer>();
        int lit = renderers.Count(r => r.lightmapIndex >= 0 && r.lightmapIndex < 65534);
        log.AppendLine("renderers in scene: " + renderers.Length + " | with a lightmap index: " + lit);

        HashSet<string> shaders = new HashSet<string>();
        Renderer sample = null;
        foreach (Renderer r in renderers)
        {
            if (r.lightmapIndex < 0 || r.lightmapIndex >= 65534)
            {
                continue;
            }
            if (sample == null)
            {
                sample = r;
            }
            foreach (Material m in r.sharedMaterials)
            {
                if (m != null && m.shader != null)
                {
                    shaders.Add(m.shader.name);
                }
            }
        }
        log.AppendLine("shaders used by lightmapped renderers: " +
                       (shaders.Count == 0 ? "<none>" : string.Join(", ", shaders.ToArray())));

        foreach (string shaderName in shaders)
        {
            Shader shader = Shader.Find(shaderName);
            if (shader == null)
            {
                log.AppendLine("  " + shaderName + ": NOT FOUND");
                continue;
            }
            bool hasKeyword = shader.keywordSpace.keywordNames.Any(k => k == "LIGHTMAP_ON");
            log.AppendLine("  " + shaderName + ": LIGHTMAP_ON variant " + (hasKeyword ? "present" : "MISSING") +
                           ", passes " + shader.passCount);
        }

        if (sample != null)
        {
            log.AppendLine("sample lightmapped renderer: " + sample.name +
                           " | index " + sample.lightmapIndex +
                           " | scaleOffset " + sample.lightmapScaleOffset);
            MeshFilter filter = sample.GetComponent<MeshFilter>();
            if (filter != null && filter.sharedMesh != null)
            {
                Mesh mesh = filter.sharedMesh;
                log.AppendLine("  mesh " + mesh.name + ": " + mesh.vertexCount + " verts, uv2(lightmap) " +
                               (mesh.uv2 != null && mesh.uv2.Length > 0 ? "present" : "MISSING"));
            }
        }
        else
        {
            log.AppendLine("sample lightmapped renderer: none — every renderer reports 'no lightmap'. " +
                           "If the scene file has m_LightmapIndex: 0 entries, Unity dropped them on import " +
                           "and the binder has to restore them (it runs in Awake/OnEnable).");
        }

        Debug.Log(log.ToString());
        EditorUtility.DisplayDialog("Block Strike lighting",
            "Отчёт в консоли. Коротко:\n\n" +
            "lightmaps bound: " + (maps == null ? 0 : maps.Length) + "\n" +
            "renderers with lightmap: " + lit + "\n" +
            "binder: " + (binder == null ? "нет" : "есть") + "\n" +
            "shaders: " + (shaders.Count == 0 ? "-" : string.Join(", ", shaders.ToArray())),
            "OK");
    }
}
