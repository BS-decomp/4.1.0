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
        Debug.Log($"[Block Strike Recovery] Maps: {scenes.Length}; lightmapped: {lightmapped}; static batch roots: {staticBatchRoots}. Geometry manifests are required before repair.");
    }
}
