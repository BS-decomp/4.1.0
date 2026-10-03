// BS-decomp / Block Strike 4.1.0 — project audit.
//
// Tools > Block Strike > Audit all maps
//
// Runs the same static scan the playtest preflight uses (SceneScan) over every
// scene in Assets/Levels and writes a report that says what is actually broken:
// still-batched geometry, missing scripts, dummy shaders, missing lightmaps and
// scenes that are not reachable from Build Settings. It changes nothing.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class BlockStrikeRecoveryMenu
{
    private const string ReportPath = "Assets/RecoveryReports/MapAudit.txt";

    [MenuItem("Tools/Block Strike/Audit all maps")]
    public static void AuditImportedMaps()
    {
        string[] scenes = AssetDatabase.FindAssets("t:SceneAsset", new[] { "Assets/Levels" })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => p.EndsWith(".unity"))
            .OrderBy(p => p)
            .ToArray();

        HashSet<string> inBuild = new HashSet<string>(
            EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path));

        StringBuilder report = new StringBuilder();
        report.AppendLine("Block Strike 4.1.0 — map audit");
        report.AppendLine("scenes: " + scenes.Length);
        report.AppendLine();

        int batched = 0, missingScripts = 0, dummyShaders = 0, missingLightmaps = 0, noGameManager = 0, notInBuild = 0;
        for (int i = 0; i < scenes.Length; i++)
        {
            string path = scenes[i];
            EditorUtility.DisplayProgressBar("Block Strike audit", path, (float)i / scenes.Length);
            SceneScan scan = SceneScan.Run(path);
            batched += scan.combinedMeshRenderers;
            missingScripts += scan.missingScripts.Count;
            dummyShaders += scan.dummyShaders.Count;
            missingLightmaps += scan.missingLightmaps.Count;
            if (!scan.hasGameManager) noGameManager++;
            if (!inBuild.Contains(path)) notInBuild++;

            bool clean = scan.combinedMeshRenderers == 0 && scan.missingScripts.Count == 0 &&
                         scan.dummyShaders.Count == 0 && scan.missingLightmaps.Count == 0;
            report.AppendLine(string.Format("{0,-28} {1}", Path.GetFileNameWithoutExtension(path),
                clean ? "ok" : string.Format("batched={0} missingScripts={1} dummyShaders={2} missingLightmaps={3}",
                    scan.combinedMeshRenderers, scan.missingScripts.Count, scan.dummyShaders.Count, scan.missingLightmaps.Count)));
            foreach (string detail in scan.missingScripts.Take(5)) report.AppendLine("      missing script: " + detail);
            foreach (string detail in scan.dummyShaders.Take(5)) report.AppendLine("      dummy shader:   " + detail);
            foreach (string detail in scan.missingLightmaps.Take(5)) report.AppendLine("      lightmap:       " + detail);
        }
        EditorUtility.ClearProgressBar();

        string summary = string.Format(
            "scenes {0} | renderers on combined meshes {1} | missing scripts {2} | dummy shaders {3} | " +
            "missing lightmaps {4} | scenes without GameManager {5} | not in Build Settings {6}",
            scenes.Length, batched, missingScripts, dummyShaders, missingLightmaps, noGameManager, notInBuild);
        report.Insert(0, summary + "\n\n");

        Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
        File.WriteAllText(ReportPath, report.ToString());
        AssetDatabase.Refresh();
        Object asset = AssetDatabase.LoadAssetAtPath<Object>(ReportPath);
        if (asset != null)
        {
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }

        if (batched > 0 || missingScripts > 0 || noGameManager > 0)
        {
            Debug.LogError("[Block Strike audit] " + summary + "\nReport: " + ReportPath);
        }
        else
        {
            Debug.Log("[Block Strike audit] " + summary + "\nReport: " + ReportPath);
        }
        EditorUtility.DisplayDialog("Block Strike audit", summary + "\n\nОтчёт: " + ReportPath, "OK");
    }
}
