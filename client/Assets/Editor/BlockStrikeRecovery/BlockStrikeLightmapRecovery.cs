// Editor-only recovery for the 608 lightmaps omitted by the Unity 4.7.2 -> 5.6 export.
// Preflights all maps before making changes, backs up scene and texture-importer metadata
// outside Assets, adds a runtime binder to each lightmapped map, and supports full revert.
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class BlockStrikeLightmapRecovery
{
    const string DataFolder = "Assets/Editor/BlockStrikeRecovery/MapGeometry";
    const string BackupFolder = "RecoveryBackups/LightmapBinding";
    const string ReceiptRelativePath = BackupFolder + "/last.json";
    const string BinderObjectName = "BS608_LegacyLightmaps";
    const int ExpectedManifestCount = 56;
    const int ExpectedMapCount = 54;
    const int ExpectedLightmappedRendererCount = 3196;

    [Serializable]
    class Manifest
    {
        public int version;
        public string sceneName;
        public string scenePath;
        public Record[] renderers;
    }

    [Serializable]
    class Record
    {
        public int rendererId;
        public int filterId;
        public int lightmapIndex;
        public string objectName;
    }

    [Serializable]
    class ReceiptEntry
    {
        public string sceneName;
        public string scenePath;
        public string backupScene;
        public string backupSceneMeta;
        public string texturePath;
        public string backupTextureMeta;
        public string manifestFile;
        public int renderers;
    }

    [Serializable]
    class Receipt
    {
        public string token;
        public string backupRoot;
        public string status;
        public string previousReceiptBackup;
        public ReceiptEntry[] scenes;
    }

    class MapInfo
    {
        public Manifest manifest;
        public string manifestFile;
        public string texturePath;
        public List<Record> lightmappedRecords;
        public ReceiptEntry receiptEntry;
    }

    class ResolvedRenderer
    {
        public MeshRenderer renderer;
        public MeshFilter filter;
        public Mesh recoveredMesh;
        public MeshAtlas meshAtlas;
    }

    class ResolvedMap
    {
        public Scene scene;
        public Texture2D texture;
        public Renderer[] renderers;
        public ResolvedRenderer[] geometry;
    }

    static string ProjectRoot
    {
        get { return Directory.GetParent(Application.dataPath).FullName; }
    }

    static string ReceiptPath
    {
        get { return Path.Combine(ProjectRoot, ReceiptRelativePath); }
    }

    static string ProjectFile(string path)
    {
        if (Path.IsPathRooted(path)) return path;
        return Path.Combine(ProjectRoot, path);
    }

    static void Require(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
    }

    static string SafeName(string value)
    {
        char[] chars = value.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            char c = chars[i];
            bool allowed = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') ||
                           (c >= '0' && c <= '9') || c == ' ' || c == '_' || c == '-' || c == '.';
            if (!allowed) chars[i] = '_';
        }
        return new string(chars);
    }

    static string LightmapPath(string scenePath)
    {
        string directory = Path.GetDirectoryName(scenePath);
        string sceneFolder = Path.GetFileNameWithoutExtension(scenePath);
        return Path.Combine(Path.Combine(directory, sceneFolder), "LightmapFar-0.png").Replace('\\', '/');
    }

    static Manifest LoadManifest(string path)
    {
        Manifest manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(path));
        Require(manifest != null && manifest.version == 1 && manifest.sceneName != null &&
                manifest.scenePath != null && manifest.renderers != null,
            "Invalid lightmap manifest: " + path);
        return manifest;
    }

    static List<MapInfo> LoadMaps()
    {
        string folder = ProjectFile(DataFolder);
        Require(Directory.Exists(folder), "Geometry manifests are not installed: " + DataFolder +
            ". Install the recovery tools before binding lightmaps.");
        string[] files = Directory.GetFiles(folder, "*.json");
        Array.Sort(files, StringComparer.Ordinal);
        Require(files.Length == ExpectedManifestCount,
            "Expected " + ExpectedManifestCount + " geometry manifests, found " + files.Length + ".");

        var maps = new List<MapInfo>();
        int totalRenderers = 0;
        var scenePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var texturePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < files.Length; i++)
        {
            Manifest manifest = LoadManifest(files[i]);
            Require(scenePaths.Add(manifest.scenePath), "Duplicate scene path in manifests: " + manifest.scenePath);
            var records = new List<Record>();
            var ids = new HashSet<int>();
            foreach (Record record in manifest.renderers)
            {
                if (record.lightmapIndex >= 254) continue;
                Require(record.lightmapIndex == 0,
                    manifest.sceneName + ": expected only original lightmap index 0, found " + record.lightmapIndex + ".");
                Require(record.rendererId > 0 && ids.Add(record.rendererId),
                    manifest.sceneName + ": invalid/duplicate renderer ID " + record.rendererId + ".");
                Require(record.filterId > 0,
                    manifest.sceneName + ": invalid MeshFilter ID " + record.filterId +
                    " for renderer " + record.rendererId + ".");
                records.Add(record);
            }
            if (records.Count == 0) continue;

            string scenePath = manifest.scenePath.Replace('\\', '/');
            string texturePath = LightmapPath(scenePath);
            Require(File.Exists(ProjectFile(scenePath)), "Scene is missing: " + scenePath);
            Require(File.Exists(ProjectFile(scenePath + ".meta")), "Scene meta is missing: " + scenePath + ".meta");
            Require(File.Exists(ProjectFile(texturePath)), "Legacy lightmap is missing: " + texturePath);
            Require(File.Exists(ProjectFile(texturePath + ".meta")), "Lightmap meta is missing: " + texturePath + ".meta");
            Require(texturePaths.Add(texturePath), "Two scenes unexpectedly use the same lightmap path: " + texturePath);

            TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            Require(importer != null, "Unity cannot load the texture importer for " + texturePath);
            Require(AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath) != null,
                "Unity did not import the lightmap texture: " + texturePath);

            MapInfo info = new MapInfo();
            info.manifest = manifest;
            info.manifestFile = Path.GetFileName(files[i]);
            info.texturePath = texturePath;
            info.lightmappedRecords = records;
            maps.Add(info);
            totalRenderers += records.Count;
        }
        Require(maps.Count == ExpectedMapCount,
            "Expected " + ExpectedMapCount + " lightmapped scenes, found " + maps.Count + ".");
        Require(totalRenderers == ExpectedLightmappedRendererCount,
            "Expected " + ExpectedLightmappedRendererCount + " lightmapped renderers, found " + totalRenderers + ".");
        return maps;
    }

    static long LocalId(Object obj)
    {
        SerializedObject serialized = new SerializedObject(obj);
        PropertyInfo mode = typeof(SerializedObject).GetProperty("inspectorMode",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        Require(mode != null, "This editor does not expose serialized local IDs.");
        mode.SetValue(serialized, Enum.ToObject(mode.PropertyType, 1), null);
        SerializedProperty property = serialized.FindProperty("m_LocalIdentfierInFile");
        if (property == null) property = serialized.FindProperty("m_LocalIdentifierInFile");
        Require(property != null, "Cannot read local ID for " + obj.name);
        return property.longValue;
    }

    static Dictionary<int, string> IndexSerializedMeshGuids(string sceneText, string sceneName)
    {
        var result = new Dictionary<int, string>();
        MatchCollection filters = Regex.Matches(sceneText,
            @"(?ms)^--- !u!33 &(\d+)\s*\r?\n(.*?)(?=^--- !u!|\z)");
        foreach (Match filterBlock in filters)
        {
            int filterId = int.Parse(filterBlock.Groups[1].Value);
            Match meshReference = Regex.Match(filterBlock.Groups[2].Value,
                @"(?m)^\s*m_Mesh:\s*\{fileID:\s*\d+,\s*guid:\s*([0-9a-fA-F]{32}),\s*type:\s*\d+\s*\}");
            if (!meshReference.Success) continue;
            Require(!result.ContainsKey(filterId), sceneName +
                ": duplicate serialized MeshFilter local ID " + filterId + ".");
            result.Add(filterId, meshReference.Groups[1].Value);
        }
        return result;
    }

    static bool IsRecoveredMeshPath(string meshPath, Manifest manifest, int rendererId)
    {
        string expectedSuffix = "/" + SafeName(manifest.sceneName) + "/Renderer-" + rendererId + ".asset";
        return meshPath.IndexOf("/RecoveredGeometry/MapGeometry-", StringComparison.Ordinal) >= 0 &&
               meshPath.EndsWith(expectedSuffix, StringComparison.Ordinal);
    }

    static Dictionary<int, ResolvedRenderer> IndexRenderers(Scene scene, Manifest manifest)
    {
        var wanted = new Dictionary<int, Record>();
        foreach (Record record in manifest.renderers)
            if (record.lightmapIndex < 254) wanted.Add(record.rendererId, record);

        // MeshAtlas has ExecuteInEditMode and assigns a HideAndDontSave mesh clone to
        // MeshFilter.sharedMesh on scene open. Read each serialized m_Mesh GUID from the
        // scene file so preflight can still verify the persistent recovered asset.
        string sceneText = File.ReadAllText(ProjectFile(manifest.scenePath));
        Dictionary<int, string> serializedMeshGuids = IndexSerializedMeshGuids(sceneText, manifest.sceneName);
        var result = new Dictionary<int, ResolvedRenderer>();
        foreach (GameObject root in scene.GetRootGameObjects())
        foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
        {
            MeshRenderer renderer = transform.GetComponent<MeshRenderer>();
            if (renderer == null) continue;
            long id = LocalId(renderer);
            if (id <= 0 || id > int.MaxValue || !wanted.ContainsKey((int)id)) continue;
            Require(!result.ContainsKey((int)id),
                manifest.sceneName + ": invalid/duplicate MeshRenderer local ID " + id + ".");

            MeshFilter filter = transform.GetComponent<MeshFilter>();
            Require(filter != null, manifest.sceneName + ": missing MeshFilter on " + transform.name + ".");
            Record record = wanted[(int)id];
            long filterId = LocalId(filter);
            Require(filterId == record.filterId,
                manifest.sceneName + "/" + transform.name + ": expected MeshFilter local ID " +
                record.filterId + ", found " + filterId + ".");

            string meshGuid = null;
            Require(filterId <= int.MaxValue && serializedMeshGuids.TryGetValue((int)filterId, out meshGuid),
                manifest.sceneName + "/" + transform.name + ": serialized MeshFilter local ID " +
                filterId + " has no persistent mesh GUID in the scene file.");
            string serializedMeshPath = AssetDatabase.GUIDToAssetPath(meshGuid);
            if (serializedMeshPath == null) serializedMeshPath = "";
            serializedMeshPath = serializedMeshPath.Replace('\\', '/');
            Require(IsRecoveredMeshPath(serializedMeshPath, manifest, (int)id),
                manifest.sceneName + "/" + transform.name +
                ": the serialized MeshFilter reference must resolve to its recovered Renderer-" + id +
                " asset, found '" + serializedMeshPath +
                "'. Run Tools > Block Strike Recovery > Repair ALL scene geometry first.");
            Mesh recoveredMesh = AssetDatabase.LoadAssetAtPath<Mesh>(serializedMeshPath);
            Require(recoveredMesh != null,
                manifest.sceneName + "/" + transform.name + ": recovered mesh asset could not be loaded: " +
                serializedMeshPath + ".");

            Mesh liveMesh = filter.sharedMesh;
            string liveMeshPath = liveMesh == null ? "" : AssetDatabase.GetAssetPath(liveMesh).Replace('\\', '/');
            MeshAtlas meshAtlas = transform.GetComponent<MeshAtlas>();
            if (meshAtlas != null && meshAtlas.originalMesh != recoveredMesh)
                Require(!meshAtlas.meshSettings,
                    manifest.sceneName + "/" + transform.name +
                    ": MeshAtlas.meshSettings is enabled. The lightmap tool will not retarget this component " +
                    "because its transform settings could be applied twice to recovered geometry.");
            if (meshAtlas == null)
            {
                Require(liveMesh == recoveredMesh && liveMeshPath == serializedMeshPath,
                    manifest.sceneName + "/" + transform.name +
                    ": expected its recovered Renderer-" + id + " mesh asset, found '" + liveMeshPath +
                    "'. Run Tools > Block Strike Recovery > Repair ALL scene geometry first.");
            }
            else if (liveMesh != recoveredMesh)
            {
                bool atlasOwnsLiveMesh = liveMesh == null || liveMesh == meshAtlas.originalMesh ||
                                         string.IsNullOrEmpty(liveMeshPath);
                Require(atlasOwnsLiveMesh,
                    manifest.sceneName + "/" + transform.name + ": MeshAtlas is present, but the live mesh '" +
                    liveMeshPath + "' is neither the serialized recovered mesh nor its source/temporary clone.");
            }

            Require(renderer.name == transform.name,
                manifest.sceneName + ": renderer name mismatch for local ID " + id + ".");
            ResolvedRenderer resolved = new ResolvedRenderer();
            resolved.renderer = renderer;
            resolved.filter = filter;
            resolved.recoveredMesh = recoveredMesh;
            resolved.meshAtlas = meshAtlas;
            result.Add((int)id, resolved);
        }
        foreach (Record record in manifest.renderers)
        {
            if (record.lightmapIndex >= 254) continue;
            Require(result.ContainsKey(record.rendererId), manifest.sceneName +
                ": cannot find original MeshRenderer local ID " + record.rendererId +
                " (" + record.objectName + "). No name-based guessing was attempted.");
        }
        return result;
    }

    static LegacyLightmapBinder[] FindBinders(Scene scene)
    {
        var binders = new List<LegacyLightmapBinder>();
        foreach (GameObject root in scene.GetRootGameObjects())
        foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
        {
            LegacyLightmapBinder binder = transform.GetComponent<LegacyLightmapBinder>();
            if (binder != null) binders.Add(binder);
        }
        return binders.ToArray();
    }

    static ResolvedMap ResolveMap(MapInfo info)
    {
        Scene scene = EditorSceneManager.OpenScene(info.manifest.scenePath, OpenSceneMode.Single);
        Require(scene.IsValid() && scene.isLoaded, "Could not open scene " + info.manifest.sceneName + ".");
        Dictionary<int, ResolvedRenderer> indexed = IndexRenderers(scene, info.manifest);
        Renderer[] renderers = new Renderer[info.lightmappedRecords.Count];
        ResolvedRenderer[] geometry = new ResolvedRenderer[info.lightmappedRecords.Count];
        for (int i = 0; i < info.lightmappedRecords.Count; i++)
        {
            ResolvedRenderer resolvedRenderer = indexed[info.lightmappedRecords[i].rendererId];
            geometry[i] = resolvedRenderer;
            renderers[i] = resolvedRenderer.renderer;
        }
        LegacyLightmapBinder[] existing = FindBinders(scene);
        Require(existing.Length <= 1, info.manifest.sceneName +
            ": found multiple LegacyLightmapBinder components; remove duplicates manually before binding.");
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(info.texturePath);
        Require(texture != null, "Lightmap failed to import: " + info.texturePath);
        ResolvedMap result = new ResolvedMap();
        result.scene = scene;
        result.texture = texture;
        result.renderers = renderers;
        result.geometry = geometry;
        return result;
    }

    static bool SameRenderers(Renderer[] a, Renderer[] b)
    {
        if (a == null || b == null || a.Length != b.Length) return false;
        for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) return false;
        return true;
    }

    static string NewToken()
    {
        return DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N").Substring(0, 8);
    }

    static Receipt CreateBackups(List<MapInfo> maps, string token)
    {
        string backupRoot = Path.Combine(ProjectRoot, BackupFolder + "/" + token);
        var entries = new List<ReceiptEntry>();
        foreach (MapInfo info in maps)
        {
            string safe = SafeName(info.manifest.sceneName);
            ReceiptEntry entry = new ReceiptEntry();
            entry.sceneName = info.manifest.sceneName;
            entry.scenePath = info.manifest.scenePath;
            entry.texturePath = info.texturePath;
            entry.manifestFile = info.manifestFile;
            entry.renderers = info.lightmappedRecords.Count;
            entry.backupScene = Path.Combine(Path.Combine(backupRoot, "Scenes"), safe + ".unity");
            entry.backupSceneMeta = Path.Combine(Path.Combine(backupRoot, "Scenes"), safe + ".unity.meta");
            entry.backupTextureMeta = Path.Combine(Path.Combine(backupRoot, "TextureMetas"), safe + ".png.meta");

            Directory.CreateDirectory(Path.GetDirectoryName(entry.backupScene));
            Directory.CreateDirectory(Path.GetDirectoryName(entry.backupTextureMeta));
            File.Copy(ProjectFile(entry.scenePath), entry.backupScene, false);
            File.Copy(ProjectFile(entry.scenePath + ".meta"), entry.backupSceneMeta, false);
            File.Copy(ProjectFile(entry.texturePath + ".meta"), entry.backupTextureMeta, false);
            info.receiptEntry = entry;
            entries.Add(entry);
        }
        Receipt receipt = new Receipt();
        receipt.token = token;
        receipt.backupRoot = backupRoot;
        receipt.status = "backed-up";
        receipt.scenes = entries.ToArray();
        Directory.CreateDirectory(Path.GetDirectoryName(ReceiptPath));
        if (File.Exists(ReceiptPath))
        {
            receipt.previousReceiptBackup = Path.Combine(backupRoot, "previous-last.json");
            File.Copy(ReceiptPath, receipt.previousReceiptBackup, false);
        }
        WriteReceipt(receipt);
        return receipt;
    }

    static void WriteReceipt(Receipt receipt)
    {
        File.WriteAllText(ReceiptPath, JsonUtility.ToJson(receipt, true));
    }

    static int SetLightmapImporters(List<MapInfo> maps)
    {
        int changed = 0;
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (MapInfo info in maps)
        {
            if (!seen.Add(info.texturePath)) continue;
            TextureImporter importer = AssetImporter.GetAtPath(info.texturePath) as TextureImporter;
            Require(importer != null, "Texture importer disappeared: " + info.texturePath);
            // Plain (Default) import on purpose. The "Lightmap" import type makes Unity re-encode the
            // pixels per platform (RGBM on desktop, dLDR on mobile), but these PNGs are already the
            // original dLDR data and the recovered shaders decode them explicitly as 2.0 * texture.
            // See docs/android-lightmap-overexposure.md.
            if (importer.textureType == TextureImporterType.Default) continue;
            importer.textureType = TextureImporterType.Default;
            importer.SaveAndReimport();
            changed++;
        }
        return changed;
    }

    static bool BindOne(MapInfo info, ref int meshAtlasSourcesUpdated)
    {
        ResolvedMap resolved = ResolveMap(info);
        Scene scene = resolved.scene;
        bool changed = false;
        for (int i = 0; i < resolved.geometry.Length; i++)
        {
            ResolvedRenderer geometry = resolved.geometry[i];
            MeshAtlas meshAtlas = geometry.meshAtlas;
            if (meshAtlas == null || meshAtlas.originalMesh == geometry.recoveredMesh) continue;

            // MeshAtlas.OnEnable otherwise restores its old source mesh and replaces the
            // recovered MeshFilter reference with a transient, non-asset clone. Retarget its
            // source first; its UV0 atlas operation then clones the recovered mesh (including UV2).
            meshAtlas.DisableMesh();
            meshAtlas.originalMesh = geometry.recoveredMesh;
            meshAtlas.UpdateMesh();
            EditorUtility.SetDirty(meshAtlas);
            EditorUtility.SetDirty(geometry.filter);
            EditorUtility.SetDirty(meshAtlas.gameObject);
            changed = true;
            meshAtlasSourcesUpdated++;
        }

        LegacyLightmapBinder[] existing = FindBinders(scene);
        LegacyLightmapBinder binder;
        if (existing.Length == 0)
        {
            GameObject go = new GameObject(BinderObjectName);
            binder = go.AddComponent<LegacyLightmapBinder>();
            changed = true;
        }
        else
        {
            binder = existing[0];
        }

        if (binder.lightmapColor != resolved.texture || !SameRenderers(binder.renderers, resolved.renderers) ||
            binder.sourceManifest != info.manifestFile || !binder.enabled || !binder.gameObject.activeSelf)
            changed = true;

        if (changed)
        {
            binder.lightmapColor = resolved.texture;
            binder.renderers = resolved.renderers;
            binder.sourceManifest = info.manifestFile;
            if (!binder.gameObject.activeSelf) binder.gameObject.SetActive(true);
            if (!binder.enabled) binder.enabled = true;
            EditorUtility.SetDirty(binder);
            EditorUtility.SetDirty(binder.gameObject);
            binder.Apply();
            EditorSceneManager.MarkSceneDirty(scene);
            Require(EditorSceneManager.SaveScene(scene), "Could not save bound scene " + info.manifest.sceneName + ".");
        }
        else
        {
            binder.Apply();
        }
        return changed;
    }

    static bool IsBusy()
    {
        if (!EditorApplication.isPlayingOrWillChangePlaymode) return false;
        EditorUtility.DisplayDialog("Lightmap recovery", "Stop Play mode before changing scenes.", "OK");
        return true;
    }

    static SceneSetup[] CaptureSceneSetup()
    {
        return EditorSceneManager.GetSceneManagerSetup();
    }

    static void RestoreSceneSetup(SceneSetup[] setup)
    {
        if (setup == null) return;
        try
        {
            EditorSceneManager.RestoreSceneManagerSetup(setup);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[BS608 Lightmaps] Could not restore the previous open-scene setup: " + ex.Message);
        }
    }

    static int ValidateMaps(List<MapInfo> maps)
    {
        int meshAtlasRetargets = 0;
        for (int i = 0; i < maps.Count; i++)
        {
            MapInfo info = maps[i];
            EditorUtility.DisplayProgressBar("Validate legacy lightmaps", info.manifest.sceneName, (float)i / maps.Count);
            ResolvedMap resolved = ResolveMap(info);
            foreach (ResolvedRenderer geometry in resolved.geometry)
                if (geometry.meshAtlas != null && geometry.meshAtlas.originalMesh != geometry.recoveredMesh)
                    meshAtlasRetargets++;
        }
        return meshAtlasRetargets;
    }

    [MenuItem("Tools/Block Strike Recovery/Validate ALL legacy lightmaps")]
    public static void ValidateAll()
    {
        if (IsBusy()) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        SceneSetup[] setup = CaptureSceneSetup();
        try
        {
            List<MapInfo> maps = LoadMaps();
            int meshAtlasRetargets = ValidateMaps(maps);
            Debug.Log("[BS608 Lightmaps] Preflight passed: " + maps.Count + " scenes, " +
                ExpectedLightmappedRendererCount + " renderer bindings, one legacy lightmap per scene" +
                (meshAtlasRetargets > 0 ? "; " + meshAtlasRetargets + " MeshAtlas source(s) need retargeting" : "") + ".");
            EditorUtility.DisplayDialog("Lightmap preflight passed",
                "Validated " + maps.Count + " scenes and " + ExpectedLightmappedRendererCount +
                " exact renderer IDs. Serialized recovered mesh assets and lightmap textures are present." +
                (meshAtlasRetargets > 0 ? "\n\nFound " + meshAtlasRetargets +
                    " MeshAtlas component(s) whose ExecuteInEditMode clone masks the recovered MeshFilter. " +
                    "The explicit Bind command will retarget those source references; Validate made no scene changes." : "") +
                "\n\nNo scene files or texture-importer settings were changed.", "OK");
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            EditorUtility.DisplayDialog("Lightmap preflight failed", ex.Message +
                "\n\nNo binding changes were made.", "OK");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            RestoreSceneSetup(setup);
        }
    }

    [MenuItem("Tools/Block Strike Recovery/Bind ALL legacy lightmaps")]
    public static void BindAll()
    {
        if (IsBusy()) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        SceneSetup[] setup = CaptureSceneSetup();
        Receipt receipt = null;
        bool completed = false;
        try
        {
            List<MapInfo> maps = LoadMaps();
            int meshAtlasRetargets = ValidateMaps(maps);
            EditorUtility.ClearProgressBar();
            if (!EditorUtility.DisplayDialog("Bind legacy lightmaps",
                "Preflight passed. This will:\n" +
                "• add an edit-mode/runtime binder to all " + maps.Count + " map scenes;\n" +
                "• bind " + ExpectedLightmappedRendererCount + " renderers to their original LightmapFar-0.png;\n" +
                "• import the 54 textures as plain (Default) textures, not the Lightmap type;\n" +
                (meshAtlasRetargets > 0 ? "• retarget " + meshAtlasRetargets +
                    " MeshAtlas source reference(s) to recovered meshes so their edit-mode clones preserve repaired geometry;\n" : "") +
                "• back up all changed scene files and texture .meta files outside Assets.\n\n" +
                "No light bake is run. A Revert command restores the backups. Continue?",
                "Bind lightmaps", "Cancel")) return;

            string token = NewToken();
            receipt = CreateBackups(maps, token);
            int importersChanged = SetLightmapImporters(maps);
            int scenesChanged = 0;
            int renderersBound = 0;
            int meshAtlasSourcesUpdated = 0;
            for (int i = 0; i < maps.Count; i++)
            {
                MapInfo info = maps[i];
                EditorUtility.DisplayProgressBar("Bind legacy lightmaps", info.manifest.sceneName,
                    (float)i / maps.Count);
                if (BindOne(info, ref meshAtlasSourcesUpdated)) scenesChanged++;
                renderersBound += info.lightmappedRecords.Count;
            }
            if (scenesChanged == 0 && importersChanged == 0)
            {
                RestorePreviousReceipt(receipt);
                completed = true;
                EditorUtility.DisplayDialog("Lightmaps already bound",
                    "All " + renderersBound + " renderer bindings and texture import settings already match. No files were changed; the previous Revert receipt was preserved.",
                    "OK");
                return;
            }
            receipt.status = "complete";
            WriteReceipt(receipt);
            completed = true;
            Debug.Log("[BS608 Lightmaps] Binding complete: " + renderersBound + " renderers in " +
                maps.Count + " scenes; " + importersChanged + " texture importers changed.");
            EditorUtility.DisplayDialog("Lightmap binding complete",
                "Bound " + renderersBound + " renderers across " + maps.Count + " maps.\n" +
                "Changed " + importersChanged + " texture import settings to Default.\n" +
                "MeshAtlas source references updated: " + meshAtlasSourcesUpdated + ".\n" +
                "Scene files changed: " + scenesChanged + ".\n\n" +
                "The binding is applied in edit mode and when scenes load at runtime. " +
                "Visual results still need checking in Unity; use Revert if anything looks wrong.\n\n" +
                "Backup: " + receipt.backupRoot,
                "OK");
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            if (receipt != null)
            {
                try
                {
                    RestoreReceipt(receipt);
                    RestorePreviousReceipt(receipt);
                }
                catch (Exception rollback)
                {
                    Debug.LogError("[BS608 Lightmaps] Automatic rollback needs attention. Backups: " +
                        receipt.backupRoot + "\n" + rollback);
                    EditorUtility.DisplayDialog("Lightmap rollback needs attention",
                        ex.Message + "\n\nAutomatic rollback failed. Use the saved backups here:\n" +
                        receipt.backupRoot, "OK");
                }
            }
            EditorUtility.DisplayDialog("Lightmap binding stopped",
                ex.Message + (receipt != null ? "\n\nThe tool attempted to restore its backups." :
                    "\n\nPreflight failed before any file changes."), "OK");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            RestoreSceneSetup(setup);
            if (!completed && receipt != null)
                Debug.Log("[BS608 Lightmaps] Failed run backup remains at " + receipt.backupRoot + ".");
        }
    }

    static void RestorePreviousReceipt(Receipt receipt)
    {
        if (receipt != null && !string.IsNullOrEmpty(receipt.previousReceiptBackup) &&
            File.Exists(receipt.previousReceiptBackup))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ReceiptPath));
            File.Copy(receipt.previousReceiptBackup, ReceiptPath, true);
        }
        else if (File.Exists(ReceiptPath))
        {
            File.Delete(ReceiptPath);
        }
    }

    static void RestoreReceipt(Receipt receipt)
    {
        Require(receipt != null && receipt.scenes != null && receipt.scenes.Length == ExpectedMapCount,
            "Invalid lightmap-binding receipt.");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        foreach (ReceiptEntry entry in receipt.scenes)
        {
            Require(File.Exists(entry.backupScene) && File.Exists(entry.backupSceneMeta) &&
                    File.Exists(entry.backupTextureMeta),
                "Backup is incomplete for " + entry.sceneName + ". Check " + receipt.backupRoot + ".");
            File.Copy(entry.backupScene, ProjectFile(entry.scenePath), true);
            File.Copy(entry.backupSceneMeta, ProjectFile(entry.scenePath + ".meta"), true);
            File.Copy(entry.backupTextureMeta, ProjectFile(entry.texturePath + ".meta"), true);
        }
        foreach (ReceiptEntry entry in receipt.scenes)
        {
            AssetDatabase.ImportAsset(entry.texturePath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(entry.scenePath, ImportAssetOptions.ForceUpdate);
        }
        AssetDatabase.Refresh();
    }

    [MenuItem("Tools/Block Strike Recovery/Revert last legacy lightmap binding")]
    public static void RevertLast()
    {
        if (IsBusy()) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (!File.Exists(ReceiptPath))
        {
            EditorUtility.DisplayDialog("Lightmap recovery", "No lightmap-binding receipt found.", "OK");
            return;
        }
        if (!EditorUtility.DisplayDialog("Revert lightmap binding",
            "Restore all " + ExpectedMapCount + " map scenes and texture importer settings from the previous binding backup?\n\n" +
            "Later edits to those scenes and lightmap .meta files will be discarded. Backups are kept on disk.",
            "Revert", "Cancel")) return;
        SceneSetup[] setup = CaptureSceneSetup();
        try
        {
            Receipt receipt = JsonUtility.FromJson<Receipt>(File.ReadAllText(ReceiptPath));
            RestoreReceipt(receipt);
            RestorePreviousReceipt(receipt);
            EditorUtility.DisplayDialog("Lightmap binding reverted",
                "Restored scenes and texture import settings from:\n" + receipt.backupRoot, "OK");
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            EditorUtility.DisplayDialog("Lightmap revert failed", ex.Message +
                "\n\nBackups have not been deleted.", "OK");
        }
        finally
        {
            RestoreSceneSetup(setup);
        }
    }
}
