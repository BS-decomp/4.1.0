// Editor-only static-geometry recovery for the 608 export in Unity 5.6.
// Does not add or replace runtime components, scripts, cameras, events or plugins.
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

public static class BlockStrikeGeometryRecovery
{
    const string DataFolder = "Assets/Editor/BlockStrikeRecovery/MapGeometry";
    const string Prefix = "BS608_Recovered_";
    const string RecoveryFolder = "RecoveryBackups/MapGeometry";

    [Serializable]
    public class Manifest
    {
        public int version;
        public string sceneName, scenePath, originalScenePath;
        public Record[] renderers;
    }

    [Serializable]
    public class Record
    {
        public int rendererId, filterId, transformId, vertexCount, subMeshCount;
        public int batchRootId, lightmapIndex;
        public string objectName, meshGuid, indexHash, positionHash;
        public int[] subsets;
        public float[] lightmapScaleOffset, localPosition, localRotation, localScale;
    }

    [Serializable]
    public class ReceiptEntry
    {
        public string manifestFile, sceneName, scenePath, backupScene, backupMeta, assetFolder;
        public int renderers;
        public bool skipped;
        public string skipReason;
    }

    [Serializable]
    public class Receipt
    {
        public string token, backupRoot, assetRoot;
        public ReceiptEntry[] scenes;
    }

    [Serializable]
    public class LegacyReceipt
    {
        public string scenePath, backupScene, backupMeta, assetFolder;
    }

    class Pending
    {
        public Record record;
        public MeshFilter filter;
        public MeshRenderer renderer;
        public Mesh mesh;
    }

    static string ProjectRoot { get { return Directory.GetParent(Application.dataPath).FullName; } }
    static string ReceiptPath { get { return Path.Combine(ProjectRoot, RecoveryFolder + "/last.json"); } }
    static string LegacyReceiptPath { get { return Path.Combine(ProjectRoot, "RecoveryBackups/BustGeometry/last.json"); } }

    static string ProjectFile(string path)
    {
        if (Path.IsPathRooted(path)) return path;
        return Path.Combine(ProjectRoot, path);
    }

    static void Require(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
    }

    static long LocalId(Object obj)
    {
        var serialized = new SerializedObject(obj);
        var mode = typeof(SerializedObject).GetProperty("inspectorMode", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        Require(mode != null, "This editor does not expose serialized local IDs.");
        mode.SetValue(serialized, Enum.ToObject(mode.PropertyType, 1), null);
        var property = serialized.FindProperty("m_LocalIdentfierInFile"); // Unity's spelling
        if (property == null) property = serialized.FindProperty("m_LocalIdentifierInFile");
        Require(property != null, "Cannot read local ID for " + obj.name);
        return property.longValue;
    }

    static Vector3 V3(float[] v) { return new Vector3(v[0], v[1], v[2]); }
    static Vector4 V4(float[] v) { return new Vector4(v[0], v[1], v[2], v[3]); }
    static bool Finite(float v) { return !float.IsNaN(v) && !float.IsInfinity(v); }

    static T Get<T>(Dictionary<long, Object> objects, long id) where T : Object
    {
        Object obj;
        Require(objects.TryGetValue(id, out obj) && obj is T,
            "Missing/wrong imported component ID " + id + " (" + typeof(T).Name + "). No guessing by name.");
        return (T)obj;
    }

    static Dictionary<long, Object> IndexScene(UnityEngine.SceneManagement.Scene scene, Manifest manifest)
    {
        var wanted = new HashSet<long>();
        foreach (Record record in manifest.renderers)
        {
            wanted.Add(record.rendererId);
            wanted.Add(record.filterId);
            wanted.Add(record.transformId);
        }
        var result = new Dictionary<long, Object>();
        // Unrelated prefab-instance components may have generated IDs. Only the
        // exact original IDs requested by the manifest are mandatory.
        foreach (GameObject root in scene.GetRootGameObjects())
        foreach (Transform tr in root.GetComponentsInChildren<Transform>(true))
        {
            Consider(result, wanted, tr);
            var filter = tr.GetComponent<MeshFilter>();
            var renderer = tr.GetComponent<MeshRenderer>();
            if (filter != null) Consider(result, wanted, filter);
            if (renderer != null) Consider(result, wanted, renderer);
        }
        return result;
    }

    static void Consider(Dictionary<long, Object> objects, HashSet<long> wanted, Object obj)
    {
        long id = LocalId(obj);
        if (!wanted.Contains(id)) return;
        Require(id != 0 && !objects.ContainsKey(id), "Invalid/duplicate local ID for " + obj.name);
        objects.Add(id, obj);
    }

    static void BatchFields(MeshRenderer renderer, bool apply)
    {
        var serialized = new SerializedObject(renderer);
        var batch = serialized.FindProperty("m_StaticBatchInfo");
        var subsets = serialized.FindProperty("m_SubsetIndices");
        Require(batch != null || (subsets != null && subsets.isArray),
            "Cannot access static batch fields on " + renderer.name);
        if (batch != null)
        {
            var first = batch.FindPropertyRelative("firstSubMesh");
            var count = batch.FindPropertyRelative("subMeshCount");
            Require(first != null && count != null, "Unknown static batch layout.");
            if (apply) { first.intValue = 0; count.intValue = 0; }
        }
        if (apply)
        {
            if (subsets != null && subsets.isArray) subsets.ClearArray();
            var batchRoot = serialized.FindProperty("m_StaticBatchRoot");
            if (batchRoot != null) batchRoot.objectReferenceValue = null;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    static string HashIndices(List<int[]> submeshes)
    {
        using (var stream = new MemoryStream())
        using (var writer = new BinaryWriter(stream))
        {
            foreach (int[] indices in submeshes)
            foreach (int index in indices)
                writer.Write(index);
            writer.Flush();
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(stream.ToArray())).Replace("-", "").ToLowerInvariant();
        }
    }

    static string HashPositions(Vector3[] vertices)
    {
        using (var stream = new MemoryStream())
        using (var writer = new BinaryWriter(stream))
        {
            foreach (Vector3 v in vertices)
            {
                writer.Write(v.x); writer.Write(v.y); writer.Write(v.z);
            }
            writer.Flush();
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(stream.ToArray())).Replace("-", "").ToLowerInvariant();
        }
    }

    static T[] Select<T>(T[] source, List<int> used, int vertexCount, string label)
    {
        if (source.Length == 0) return new T[0];
        Require(source.Length == vertexCount, "Unexpected " + label + " array length.");
        var output = new T[used.Count];
        for (int i = 0; i < used.Count; i++) output[i] = source[used[i]];
        return output;
    }

    static bool ValidateIdentity(Dictionary<long, Object> objects, Record record,
        out MeshRenderer renderer, out MeshFilter filter, out Transform tr)
    {
        renderer = Get<MeshRenderer>(objects, record.rendererId);
        filter = Get<MeshFilter>(objects, record.filterId);
        tr = Get<Transform>(objects, record.transformId);
        Require(renderer.gameObject == filter.gameObject && tr.gameObject == renderer.gameObject,
            "Static geometry components are on different objects: " + record.objectName);
        Require(tr.name == record.objectName, "Object name differs from the original export: ID " + record.transformId);
        Require((tr.localPosition - V3(record.localPosition)).sqrMagnitude < 1e-8f,
            "Object position was edited: " + tr.name);
        Require((tr.localScale - V3(record.localScale)).sqrMagnitude < 1e-8f,
            "Object scale was edited: " + tr.name);
        var q = record.localRotation;
        Require(Quaternion.Angle(tr.localRotation, new Quaternion(q[0], q[1], q[2], q[3])) < 0.05f,
            "Object rotation was edited: " + tr.name);
        return filter.sharedMesh != null && filter.sharedMesh.name.StartsWith(Prefix);
    }

    static bool PositionDataMatches(Mesh source, Record record)
    {
        if (source == null) return false;
        return source.vertexCount == record.vertexCount &&
               source.subMeshCount == record.subMeshCount &&
               HashPositions(source.vertices) == record.positionHash;
    }

    static Mesh ResolveSourceMesh(MeshFilter filter, Record record, string sceneName)
    {
        // Prefer the immutable mesh asset named by the original export. Unity's
        // editor can expose a regenerated static-batch mesh through sharedMesh,
        // especially after a scene was repaired and later reverted.
        string manifestPath = AssetDatabase.GUIDToAssetPath(record.meshGuid);
        Mesh manifestMesh = null;
        if (manifestPath.Length > 0)
            manifestMesh = AssetDatabase.LoadAssetAtPath<Mesh>(manifestPath);
        if (PositionDataMatches(manifestMesh, record)) return manifestMesh;
        if (manifestMesh != null)
            Debug.LogWarning("[BS608 Recovery] " + sceneName + "/" + filter.name +
                ": manifest mesh asset no longer has the original content; checking the scene mesh instead.");

        Mesh sceneMesh = filter.sharedMesh;
        Require(sceneMesh != null, "Missing source mesh: " + filter.name);
        string scenePath = AssetDatabase.GetAssetPath(sceneMesh);
        string sceneGuid = AssetDatabase.AssetPathToGUID(scenePath);
        if (sceneGuid != record.meshGuid)
            Debug.LogWarning("[BS608 Recovery] " + sceneName + "/" + filter.name +
                ": scene mesh GUID changed from " + record.meshGuid + " to " + sceneGuid +
                "; exact triangle/index checks will decide.");
        return sceneMesh;
    }

    static Mesh Split(Mesh source, Transform tr, Record record)
    {
        Require(source.vertexCount == record.vertexCount && source.subMeshCount == record.subMeshCount,
            "Imported mesh layout differs from export: " + tr.name +
            " (expected " + record.vertexCount + " vertices/" + record.subMeshCount +
            " submeshes, got " + source.vertexCount + "/" + source.subMeshCount + ")");
        Require(HashPositions(source.vertices) == record.positionHash,
            "Imported vertex positions differ from the original export: " + tr.name);
        Require(source.blendShapeCount == 0 && source.bindposes.Length == 0 && source.boneWeights.Length == 0,
            "Skinned geometry is not supported by this static repair.");
        var original = new List<int[]>();
        foreach (int subset in record.subsets)
        {
            Require(subset >= 0 && subset < source.subMeshCount && source.GetTopology(subset) == MeshTopology.Triangles,
                "Invalid source subset: " + tr.name);
            original.Add(source.GetTriangles(subset));
        }
        Require(HashIndices(original) == record.indexHash,
            "Imported triangle data differs from original export: " + tr.name);

        var used = new List<int>();
        var remap = new Dictionary<int, int>();
        var triangles = new List<int[]>();
        foreach (int[] indices in original)
        {
            var rebuilt = new int[indices.Length];
            for (int j = 0; j < indices.Length; j++)
            {
                int old = indices[j], mapped;
                Require(old >= 0 && old < source.vertexCount, "Index outside vertex buffer: " + tr.name);
                if (!remap.TryGetValue(old, out mapped))
                {
                    mapped = used.Count;
                    remap.Add(old, mapped);
                    used.Add(old);
                }
                rebuilt[j] = mapped;
            }
            triangles.Add(rebuilt);
        }
        Require(used.Count > 0 && used.Count <= 65534, "Unsupported recovered mesh size: " + tr.name);
        Require(record.batchRootId == 0, "Non-world batch root requires a different conversion: " + tr.name);

        Matrix4x4 toLocal = tr.worldToLocalMatrix;
        Require(Finite(toLocal.determinant) && Mathf.Abs(toLocal.determinant) > 1e-10f,
            "Singular transform: " + tr.name);
        Vector3[] vertices = Select(source.vertices, used, source.vertexCount, "vertices");
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 world = vertices[i];
            vertices[i] = toLocal.MultiplyPoint3x4(world);
            Vector3 roundtrip = tr.localToWorldMatrix.MultiplyPoint3x4(vertices[i]);
            Require(Finite(vertices[i].x) && Finite(vertices[i].y) && Finite(vertices[i].z) &&
                    (roundtrip - world).sqrMagnitude < 0.0001f,
                "World/local roundtrip failed: " + tr.name);
        }

        Vector3[] normals = Select(source.normals, used, source.vertexCount, "normals");
        Matrix4x4 normalMatrix = toLocal.inverse.transpose;
        for (int i = 0; i < normals.Length; i++) normals[i] = normalMatrix.MultiplyVector(normals[i]).normalized;
        Vector4[] tangents = Select(source.tangents, used, source.vertexCount, "tangents");
        float sign = toLocal.determinant < 0 ? -1f : 1f;
        for (int i = 0; i < tangents.Length; i++)
        {
            Vector3 xyz = toLocal.MultiplyVector(new Vector3(tangents[i].x, tangents[i].y, tangents[i].z)).normalized;
            tangents[i] = new Vector4(xyz.x, xyz.y, xyz.z, tangents[i].w * sign);
        }

        var mesh = new Mesh();
        try
        {
            mesh.name = Prefix + record.rendererId + "_" + record.objectName;
            mesh.vertices = vertices;
            if (normals.Length > 0) mesh.normals = normals;
            if (tangents.Length > 0) mesh.tangents = tangents;
            mesh.colors32 = Select(source.colors32, used, source.vertexCount, "colors");
            mesh.uv = Select(source.uv, used, source.vertexCount, "UV0");
            // The original 608 batch UV2 already includes the atlas scale/offset.
            // Keep it and change only the renderer's scale/offset to identity below.
            mesh.uv2 = Select(source.uv2, used, source.vertexCount, "UV2");
            mesh.uv3 = Select(source.uv3, used, source.vertexCount, "UV3");
            mesh.uv4 = Select(source.uv4, used, source.vertexCount, "UV4");
            if (record.lightmapIndex < 254)
            {
                Require(mesh.uv2.Length == vertices.Length, "Missing baked lightmap coordinates: " + tr.name);
                Vector4 st = V4(record.lightmapScaleOffset);
                foreach (Vector2 uv in mesh.uv2)
                    Require(uv.x >= st.z - 0.002f && uv.x <= st.z + st.x + 0.002f &&
                            uv.y >= st.w - 0.002f && uv.y <= st.w + st.y + 0.002f,
                        "Imported UV2 is not the expected baked atlas data: " + tr.name);
            }
            mesh.subMeshCount = triangles.Count;
            for (int i = 0; i < triangles.Count; i++) mesh.SetTriangles(triangles[i], i);
            mesh.RecalculateBounds();
            return mesh;
        }
        catch
        {
            Object.DestroyImmediate(mesh);
            throw;
        }
    }

    static string SafeName(string value)
    {
        var chars = value.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            char c = chars[i];
            bool ok = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') ||
                      (c >= '0' && c <= '9') || c == ' ' || c == '_' || c == '-' || c == '.';
            if (!ok) chars[i] = '_';
        }
        return new string(chars);
    }

    static Manifest LoadManifest(string path)
    {
        var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(path));
        Require(manifest != null && manifest.version == 1 && manifest.sceneName != null && manifest.renderers != null,
            "Invalid geometry manifest: " + path);
        return manifest;
    }

    static string SceneFile(Manifest manifest)
    {
        if (File.Exists(ProjectFile(manifest.scenePath))) return manifest.scenePath;
        if (File.Exists(ProjectFile(manifest.originalScenePath))) return manifest.originalScenePath;
        throw new InvalidOperationException("Scene not found: " + manifest.sceneName);
    }

    static ReceiptEntry Skipped(Manifest manifest, string manifestFile, string reason)
    {
        return new ReceiptEntry
        {
            manifestFile = manifestFile,
            sceneName = manifest.sceneName,
            scenePath = manifest.scenePath,
            backupScene = "", backupMeta = "", assetFolder = "",
            renderers = 0, skipped = true, skipReason = reason
        };
    }

    static ReceiptEntry RepairOne(Manifest manifest, string manifestFile,
        string token, string assetRoot, string backupRoot)
    {
        if (manifest.renderers.Length == 0)
            return Skipped(manifest, manifestFile, "no static-batched renderers");

        string scenePath = SceneFile(manifest);
        string safe = SafeName(manifest.sceneName);
        string sceneBackup = Path.Combine(backupRoot, safe + ".unity");
        string metaBackup = Path.Combine(backupRoot, safe + ".unity.meta");
        string assetFolder = assetRoot + "/" + safe;
        var pending = new List<Pending>();
        bool sceneTouched = false;
        bool assetsCreated = false;
        UnityEngine.SceneManagement.Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        try
        {
            var objects = IndexScene(scene, manifest);
            int already = 0;
            foreach (Record record in manifest.renderers)
            {
                MeshRenderer renderer; MeshFilter filter; Transform tr;
                if (ValidateIdentity(objects, record, out renderer, out filter, out tr)) already++;
            }
            if (already == manifest.renderers.Length)
            {
                Debug.Log("[BS608 Recovery] " + manifest.sceneName + " is already repaired; leaving it unchanged.");
                return Skipped(manifest, manifestFile, "already repaired");
            }
            Require(already == 0, manifest.sceneName + " is only partially repaired. Restore its backup before applying this tool.");

            foreach (Record record in manifest.renderers)
            {
                MeshRenderer renderer; MeshFilter filter; Transform tr;
                ValidateIdentity(objects, record, out renderer, out filter, out tr);
                Mesh sourceMesh = ResolveSourceMesh(filter, record, manifest.sceneName);
                Require(renderer.sharedMaterials.Length == record.subsets.Length,
                    "Material count differs: " + tr.name);
                BatchFields(renderer, false);
                pending.Add(new Pending
                {
                    record = record,
                    filter = filter,
                    renderer = renderer,
                    mesh = Split(sourceMesh, tr, record)
                });
            }

            // Every source check and in-memory mesh conversion succeeded. Only now write.
            Directory.CreateDirectory(backupRoot);
            File.Copy(ProjectFile(scenePath), sceneBackup, false);
            File.Copy(ProjectFile(scenePath + ".meta"), metaBackup, false);
            Directory.CreateDirectory(ProjectFile(assetFolder));
            AssetDatabase.Refresh();
            assetsCreated = true;
            foreach (Pending item in pending)
                AssetDatabase.CreateAsset(item.mesh, assetFolder + "/Renderer-" + item.record.rendererId + ".asset");
            AssetDatabase.SaveAssets();

            sceneTouched = true;
            foreach (Pending item in pending)
            {
                BatchFields(item.renderer, true);
                item.filter.sharedMesh = item.mesh;
                if (item.record.lightmapIndex < 254)
                    item.renderer.lightmapScaleOffset = new Vector4(1, 1, 0, 0);
                EditorUtility.SetDirty(item.filter);
                EditorUtility.SetDirty(item.renderer);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            Require(EditorSceneManager.SaveScene(scene), "Could not save repaired scene: " + manifest.sceneName);
            return new ReceiptEntry
            {
                manifestFile = manifestFile,
                sceneName = manifest.sceneName,
                scenePath = scenePath,
                backupScene = sceneBackup,
                backupMeta = metaBackup,
                assetFolder = assetFolder,
                renderers = pending.Count,
                skipped = false,
                skipReason = ""
            };
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            try
            {
                if (sceneTouched && File.Exists(sceneBackup) && File.Exists(metaBackup))
                    RestoreFiles(scenePath, sceneBackup, metaBackup, true);
                if (assetsCreated && AssetDatabase.IsValidFolder(assetFolder))
                    AssetDatabase.DeleteAsset(assetFolder);
            }
            catch (Exception rollback)
            {
                Debug.LogError("[BS608 Recovery] Rollback needs attention for " + manifest.sceneName +
                    ". Backup: " + sceneBackup + "\n" + rollback);
            }
            throw new InvalidOperationException(manifest.sceneName + ": " + ex.Message, ex);
        }
        finally
        {
            foreach (Pending item in pending)
                if (item.mesh != null && !AssetDatabase.Contains(item.mesh))
                    Object.DestroyImmediate(item.mesh);
        }
    }

    static void RestoreFiles(string scenePath, string backupScene, string backupMeta, bool reopen)
    {
        Require(File.Exists(backupScene) && File.Exists(backupMeta), "Backup is incomplete: " + scenePath);
        // The caller explicitly agreed to save/discard current scene changes where applicable.
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        File.Copy(backupScene, ProjectFile(scenePath), true);
        File.Copy(backupMeta, ProjectFile(scenePath + ".meta"), true);
        AssetDatabase.ImportAsset(scenePath, ImportAssetOptions.ForceUpdate);
        if (reopen) EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
    }

    static void Repair(bool allScenes)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Recovery", "Stop Play first.", "OK");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        string title = allScenes ? "Repair all scene geometry" : "Repair Bust geometry";
        string scope = allScenes
            ? "This repairs static MeshRenderer geometry in all scenes that need it, including Menu. Bust is skipped safely if the earlier pilot repair is already present."
            : "This repairs only Bust. It can be used after the full repair as a no-change check.";
        if (!EditorUtility.DisplayDialog(title,
            scope + "\n\nEach modified scene and .meta is backed up outside Assets. Cameras, events, scripts, colliders, transforms, hierarchy and materials are not replaced.\n\nProceed?",
            "Repair", "Cancel")) return;

        string token = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N").Substring(0, 8);
        string backupRoot = Path.Combine(ProjectRoot, RecoveryFolder + "/" + token);
        string assetRoot = "Assets/RecoveredGeometry/MapGeometry-" + token;
        var completed = new List<ReceiptEntry>();
        int repairedScenes = 0, repairedRenderers = 0, skippedScenes = 0;
        try
        {
            Require(Directory.Exists(ProjectFile(DataFolder)), "Geometry manifests are not installed: " + DataFolder);
            var files = Directory.GetFiles(ProjectFile(DataFolder), "*.json");
            Array.Sort(files, StringComparer.Ordinal);
            Require(files.Length == 56, "Expected 56 scene manifests, found " + files.Length + ".");
            float total = files.Length;
            for (int i = 0; i < files.Length; i++)
            {
                var manifest = LoadManifest(files[i]);
                if (!allScenes && manifest.sceneName != "Bust") continue;
                EditorUtility.DisplayProgressBar(title, manifest.sceneName, i / total);
                string manifestFile = Path.GetFileName(files[i]);
                ReceiptEntry entry = RepairOne(manifest, manifestFile, token, assetRoot, backupRoot);
                completed.Add(entry);
                if (entry.skipped) skippedScenes++;
                else
                {
                    repairedScenes++;
                    repairedRenderers += entry.renderers;
                }
                var receipt = new Receipt
                {
                    token = token,
                    backupRoot = backupRoot,
                    assetRoot = assetRoot,
                    scenes = completed.ToArray()
                };
                if (!entry.skipped || repairedScenes > 0)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(ReceiptPath));
                    File.WriteAllText(ReceiptPath, JsonUtility.ToJson(receipt, true));
                }
            }
            Debug.Log("[BS608 Recovery] Geometry repair complete: " + repairedRenderers +
                " renderers in " + repairedScenes + " scenes; " + skippedScenes + " scenes skipped.");
            EditorUtility.DisplayDialog("Geometry repair complete",
                "Repaired " + repairedRenderers + " renderers in " + repairedScenes + " scenes.\n" +
                "Skipped " + skippedScenes + " scenes (empty or already repaired).\n\n" +
                "Open maps in Scene view without Play. Shaders, baked lighting, startup and online services are separate remaining tasks.\n\nBackup: " + backupRoot,
                "OK");
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            EditorUtility.DisplayDialog("Geometry recovery stopped",
                ex.Message + "\n\nScenes completed before this point were kept. The failing scene attempted to roll back to its backup. See Console for details.",
                "OK");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    [MenuItem("Tools/Block Strike Recovery/Repair ALL scene geometry")]
    public static void RepairAllScenes()
    {
        Repair(true);
    }

    [MenuItem("Tools/Block Strike Recovery/Repair Bust geometry")]
    public static void RepairBustOnly()
    {
        Repair(false);
    }

    [MenuItem("Tools/Block Strike Recovery/Revert last full geometry repair")]
    public static void RevertFullRepair()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (!File.Exists(ReceiptPath))
        {
            EditorUtility.DisplayDialog("Recovery", "No full-repair receipt found.", "OK");
            return;
        }
        if (!EditorUtility.DisplayDialog("Revert geometry repair",
            "Restore every scene changed by the last full repair from its backup? Later edits to those scenes will be lost. Recovered mesh assets stay on disk so other references are not broken.",
            "Revert", "Cancel")) return;
        try
        {
            var receipt = JsonUtility.FromJson<Receipt>(File.ReadAllText(ReceiptPath));
            Require(receipt != null && receipt.scenes != null, "Invalid repair receipt.");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            int restored = 0;
            foreach (ReceiptEntry entry in receipt.scenes)
            {
                if (entry.skipped || entry.backupScene.Length == 0) continue;
                Require(File.Exists(entry.backupScene) && File.Exists(entry.backupMeta),
                    "Backup is incomplete: " + entry.sceneName);
                File.Copy(entry.backupScene, ProjectFile(entry.scenePath), true);
                File.Copy(entry.backupMeta, ProjectFile(entry.scenePath + ".meta"), true);
                AssetDatabase.ImportAsset(entry.scenePath, ImportAssetOptions.ForceUpdate);
                restored++;
            }
            File.Delete(ReceiptPath);
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Geometry repair reverted", "Restored " + restored + " scenes from backups.", "OK");
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            EditorUtility.DisplayDialog("Revert failed", ex.Message, "OK");
        }
    }

    [MenuItem("Tools/Block Strike Recovery/Revert Bust pilot repair")]
    public static void RevertBustPilot()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (!File.Exists(LegacyReceiptPath))
        {
            EditorUtility.DisplayDialog("Recovery", "No Bust pilot receipt found.", "OK");
            return;
        }
        if (!EditorUtility.DisplayDialog("Revert Bust pilot",
            "Replace Bust with its pre-pilot backup? Later edits to Bust will be lost. Pilot mesh assets stay on disk.",
            "Revert", "Cancel")) return;
        try
        {
            var receipt = JsonUtility.FromJson<LegacyReceipt>(File.ReadAllText(LegacyReceiptPath));
            RestoreFiles(receipt.scenePath, receipt.backupScene, receipt.backupMeta, true);
            File.Delete(LegacyReceiptPath);
            EditorUtility.DisplayDialog("Bust pilot reverted", "Bust was restored from its pilot backup.", "OK");
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            EditorUtility.DisplayDialog("Revert failed", ex.Message, "OK");
        }
    }
}
