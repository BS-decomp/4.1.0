using System.IO;
using UnityEditor;
using UnityEngine;

public static class BlockStrikeRecoveryMenu
{
    [MenuItem("Tools/Block Strike Recovery/Audit imported maps")]
    public static void AuditImportedMaps()
    {
        var scenes = Directory.GetFiles("Assets/Levels/Maps", "*.unity", SearchOption.AllDirectories);
        int lightmapped = 0, staticBatchRoots = 0;
        foreach (var scene in scenes)
        {
            var text = File.ReadAllText(scene);
            if (text.Contains("m_LightmapIndex:")) lightmapped++;
            if (text.Contains("m_StaticBatchRoot: {fileID: ") && !text.Contains("m_StaticBatchRoot: {fileID: 0")) staticBatchRoots++;
        }
        var reportDir = "Assets/RecoveryReports";
        Directory.CreateDirectory(reportDir);
        var reportPath = Path.Combine(reportDir, "MapAudit.txt");
        var report = $"Block Strike 4.1.0 map audit\nMaps: {scenes.Length}\nLightmapped scenes: {lightmapped}\nStatic batch roots: {staticBatchRoots}\n\nGeometry manifests are required before repair.\n";
        File.WriteAllText(reportPath, report);
        AssetDatabase.Refresh();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(reportPath);
        EditorGUIUtility.PingObject(Selection.activeObject);
        EditorUtility.DisplayDialog("Block Strike Recovery", report + "\nReport saved to Assets/RecoveryReports/MapAudit.txt", "OK");
        Debug.Log("[Block Strike Recovery] Report saved to " + reportPath);
    }
}
