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
                       " | active target: " + EditorUserBuildSettings.activeBuildTarget);

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
                           " texture(s), " + (binder.renderNames == null ? 0 : binder.renderNames.Length) +
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
        MaterialPropertyBlock probe = new MaterialPropertyBlock();
        int viaBlock = 0;
        foreach (Renderer r in renderers)
        {
            if (!r.HasPropertyBlock())
            {
                continue;
            }
            r.GetPropertyBlock(probe);
            if (probe.GetVector("_BSLightmapST") != Vector4.zero)
            {
                viaBlock++;
            }
        }
        log.AppendLine("renderers in scene: " + renderers.Length + " | with a lightmap index: " + lit +
                       " | fed through a property block: " + viaBlock);

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
            // The recovery path delivers the bake through a
            // MaterialPropertyBlock, so what matters is that the shader
            // declares _BSLightmap (an undeclared property is a silent no-op).
            bool declaresRecovery = false;
            try
            {
                int count = shader.GetPropertyCount();
                for (int p = 0; p < count; p++)
                {
                    if (shader.GetPropertyName(p) == "_BSLightmap")
                    {
                        declaresRecovery = true;
                        break;
                    }
                }
            }
            catch { }
            log.AppendLine("  " + shaderName + ": recovery lightmap property " +
                           (declaresRecovery ? "present" : "MISSING") + ", passes " + shader.passCount);
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

        // Per-renderer detail: this is what tells apart "no texture", "no UV1",
        // "no lightmap" and "shader does not sample it".
        int shown = 0;
        foreach (Renderer r in renderers)
        {
            if (r.lightmapIndex < 0 || r.lightmapIndex >= 65534)
            {
                continue;
            }
            Material mat = r.sharedMaterial;
            MeshFilter filter = r.GetComponent<MeshFilter>();
            Mesh mesh = filter != null ? filter.sharedMesh : null;
            probe.Clear();
            if (r.HasPropertyBlock())
            {
                r.GetPropertyBlock(probe);
            }
            Vector4 st = probe.GetVector("_BSLightmapST");
            Texture blockTex = probe.GetTexture("_BSLightmap");

            log.AppendLine("renderer \"" + r.name + "\"");
            log.AppendLine("  material: " + (mat == null ? "<none>" : mat.name) +
                           " | shader: " + (mat == null || mat.shader == null ? "<none>" : mat.shader.name) +
                           " | mainTexture: " + (mat == null || mat.mainTexture == null
                               ? "NOT SET" : mat.mainTexture.name));
            log.AppendLine("  block: _BSLightmap " + (blockTex == null ? "NOT SET" : blockTex.name) +
                           " | _BSLightmapST " + st);
            log.AppendLine("  renderer.lightmapIndex " + r.lightmapIndex +
                           " | scaleOffset " + r.lightmapScaleOffset);
            if (mesh == null)
            {
                log.AppendLine("  mesh: <none>");
            }
            else
            {
                Vector2[] uv2 = mesh.uv2;
                log.AppendLine("  mesh " + mesh.name + ": verts " + mesh.vertexCount +
                               " | uv2 " + (uv2 == null ? 0 : uv2.Length) +
                               (uv2 != null && uv2.Length > 0 ? " first " + uv2[0] : "") +
                               " | uv " + (mesh.uv == null ? 0 : mesh.uv.Length));
                if (uv2 != null && uv2.Length > 0)
                {
                    Vector2 mapped = new Vector2(uv2[0].x * st.x + st.z, uv2[0].y * st.y + st.w);
                    log.AppendLine("  lightmap UV of vertex 0: " + mapped);
                }
            }
            if (++shown >= 3)
            {
                break;
            }
        }

        log.AppendLine("RenderSettings: fog " + RenderSettings.fog + " (" + RenderSettings.fogMode +
                       ", colour " + RenderSettings.fogColor + ") | ambient " + RenderSettings.ambientLight);

        Debug.Log(log.ToString());
        EditorUtility.DisplayDialog("Block Strike lighting",
            "Отчёт в консоли. Коротко:\n\n" +
            "lightmaps bound: " + (maps == null ? 0 : maps.Length) + "\n" +
            "renderers with lightmap: " + lit + " (index) / " + viaBlock + " (property block)\n" +
            "binder: " + (binder == null ? "нет" : "есть") + "\n" +
            "shaders: " + (shaders.Count == 0 ? "-" : string.Join(", ", shaders.ToArray())),
            "OK");
    }

    [MenuItem("Tools/Block Strike/Lighting: force rebind")]
    public static void ForceRebind()
    {
        BSLegacyLightmaps[] binders = Object.FindObjectsOfType<BSLegacyLightmaps>();
        foreach (BSLegacyLightmaps binder in binders)
        {
            binder.Apply();
        }
        Debug.Log("[BS Lightmaps] re-applied " + binders.Length + " binder(s). " +
                  "LightmapSettings.lightmaps = " +
                  (LightmapSettings.lightmaps == null ? 0 : LightmapSettings.lightmaps.Length));
        EditorUtility.DisplayDialog("Block Strike lighting",
            "Пересвязано биндеров: " + binders.Length, "OK");
    }

    [MenuItem("Tools/Block Strike/Lighting: safe mode (unbind)")]
    public static void SafeMode()
    {
        int touched = 0;
        foreach (Renderer renderer in Object.FindObjectsOfType<Renderer>())
        {
            if (renderer.lightmapIndex >= 0 && renderer.lightmapIndex < 65534)
            {
                renderer.lightmapIndex = 65535;
                touched++;
            }
        }
        foreach (BSLegacyLightmaps binder in Object.FindObjectsOfType<BSLegacyLightmaps>())
        {
            binder.enabled = false;
        }
        Debug.LogWarning("[BS Lightmaps] safe mode: " + touched +
                         " renderer(s) switched to 'no lightmap' and the binder disabled. " +
                         "Nothing is saved — reopen the scene to undo.");
        EditorUtility.DisplayDialog("Block Strike lighting",
            "Безопасный режим: лайтмапы отвязаны у " + touched + " рендереров.\n" +
            "Ничего не сохранено — переоткрой сцену, чтобы вернуть.", "OK");
    }

    [MenuItem("Tools/Block Strike/Lighting: debug view (cycle)")]
    public static void CycleDebugView()
    {
        int mode = (Mathf.RoundToInt(Shader.GetGlobalFloat("_BSDebugMode")) + 1) % 4;
        Shader.SetGlobalFloat("_BSDebugMode", mode);
        string[] names = { "normal", "albedo only (_MainTex)", "lightmap only", "UV1 as colour" };
        Debug.Log("[BS Lightmaps] debug view: " + names[mode]);
        EditorUtility.DisplayDialog("Block Strike lighting", "Режим показа: " + names[mode] +
            "\n\nПрощёлкай по кругу: normal -> albedo -> lightmap -> UV1.", "OK");
        SceneView.RepaintAll();
    }
}
