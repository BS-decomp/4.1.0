// BS-decomp / Block Strike 4.1.0 — playtest arming window.
//
// Tools > Block Strike > Playtest
//
//   1. runs a preflight over the project and the chosen map and prints every
//      problem it finds (short line in the window, full detail in the console);
//   2. if nothing is broken it arms ONE play mode session: nick "byvlal",
//      offline Photon room, the game mode the map belongs to;
//   3. the session is consumed by the first Play and removed when play mode
//      ends, when the window is used again, or on the next editor start if
//      Unity was closed/crashed while a session was still armed.
//
// Nothing in the project is modified by arming: the session lives in
// Temp/BSPlaytestSession.json, which Unity itself wipes.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public class BSPlaytestWindow : EditorWindow
{
    private const string TicketKey = "BS.Playtest.Ticket";
    private const string ArmedSceneKey = "BS.Playtest.Scene";

    private string[] scenePaths = new string[0];
    private string[] sceneLabels = new string[0];
    private int sceneIndex;
    private string nick = "byvlal";
    private int modeOverride = -1;
    private bool bootThroughMenu = true;
    private const string BootScene = "Menu";
    private Vector2 scroll;
    private List<Check> checks = new List<Check>();
    private bool preflightDone;

    private class Check
    {
        public bool error;
        public bool warning;
        public string title;
        public string detail;
    }

    [MenuItem("Tools/Block Strike/Playtest %#p")]
    public static void Open()
    {
        GetWindow<BSPlaytestWindow>(false, "BS Playtest", true).Refresh();
    }

    [MenuItem("Tools/Block Strike/Clear playtest state")]
    public static void ClearState()
    {
        DisarmSession("manual reset");
        EditorUtility.DisplayDialog("Block Strike Playtest", "Состояние плейтеста очищено.", "OK");
    }

    private void OnEnable()
    {
        Refresh();
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private void OnDisable()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            DisarmSession("play mode ended");
        }
    }

    private void Refresh()
    {
        scenePaths = AssetDatabase.FindAssets("t:SceneAsset", new[] { "Assets/Levels" })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => p.EndsWith(".unity"))
            .OrderBy(p => p)
            .ToArray();
        sceneLabels = scenePaths
            .Select(p => Path.GetFileNameWithoutExtension(p) + "   (" + Path.GetDirectoryName(p).Replace("Assets/Levels", "") + ")")
            .ToArray();
        string current = EditorPrefs.GetString(ArmedSceneKey, string.Empty);
        if (!string.IsNullOrEmpty(current))
        {
            int found = Array.IndexOf(scenePaths, current);
            if (found >= 0) sceneIndex = found;
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Block Strike 4.1.0 — playtest", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Оффлайн-прогон карты без сервера: ник byvlal, оффлайн-комната Photon, " +
            "режим берётся из Resources/others/SceneManager.json.\n" +
            "Сессия одноразовая: один Play. Выход из Play — всё снимается.",
            MessageType.None);

        if (scenePaths.Length == 0)
        {
            EditorGUILayout.HelpBox("В Assets/Levels нет сцен.", MessageType.Error);
            if (GUILayout.Button("Обновить список")) Refresh();
            return;
        }

        sceneIndex = Mathf.Clamp(sceneIndex, 0, scenePaths.Length - 1);
        sceneIndex = EditorGUILayout.Popup("Карта", sceneIndex, sceneLabels);
        nick = EditorGUILayout.TextField("Ник", nick);
        bootThroughMenu = EditorGUILayout.ToggleLeft(
            "Поднимать игру через " + BootScene + " (как в оригинале: аккаунт, оружие, скин, джойстик)",
            bootThroughMenu);
        if (!bootThroughMenu)
        {
            EditorGUILayout.HelpBox(
                "Прямой запуск карты пропускает инициализацию Logo/Menu: не будет настроек, " +
                "локализации и данных аккаунта — игрок будет неполноценным. Включай только для " +
                "проверки геометрии.", MessageType.Warning);
        }

        string sceneName = Path.GetFileNameWithoutExtension(scenePaths[sceneIndex]);
        int resolved = BSPlaytestModes.ResolveMode(sceneName);
        string[] modeNames = new[] { "Авто (" + (resolved >= 0 ? ((GameMode)resolved).ToString() : "не найден") + ")" }
            .Concat(Enum.GetNames(typeof(GameMode))).ToArray();
        int popup = EditorGUILayout.Popup("Режим", modeOverride + 1, modeNames);
        modeOverride = popup - 1;

        EditorGUILayout.Space();
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Проверить карту", GUILayout.Height(26)))
            {
                RunPreflight(scenePaths[sceneIndex]);
            }
            GUI.enabled = preflightDone && !checks.Any(c => c.error);
            if (GUILayout.Button("Подготовить плейтест", GUILayout.Height(26)))
            {
                Arm(scenePaths[sceneIndex], resolved);
            }
            GUI.enabled = true;
        }
        if (GUILayout.Button("Сбросить состояние плейтеста"))
        {
            DisarmSession("manual reset");
        }

        EditorGUILayout.Space();
        if (File.Exists(BSPlaytestSession.FilePath))
        {
            EditorGUILayout.HelpBox("Сессия ВЗВЕДЕНА. Открой карту и нажми Play.", MessageType.Info);
        }

        scroll = EditorGUILayout.BeginScrollView(scroll);
        foreach (Check c in checks)
        {
            EditorGUILayout.HelpBox(c.title, c.error ? MessageType.Error : c.warning ? MessageType.Warning : MessageType.Info);
        }
        EditorGUILayout.EndScrollView();
    }

    // ------------------------------------------------------------------ //
    // preflight
    // ------------------------------------------------------------------ //

    private void RunPreflight(string scenePath)
    {
        checks = new List<Check>();
        StringBuilder log = new StringBuilder();
        log.AppendLine("[BS Playtest] preflight for " + scenePath);

        Add(!EditorApplication.isCompiling && !EditorUtility.scriptCompilationFailed,
            "Скрипты компилируются", "Сначала почини ошибки компиляции — плейтест не запустится.", true);

        Add(File.Exists(scenePath), "Файл сцены на месте", scenePath, true);

        bool inBuild = EditorBuildSettings.scenes.Any(s => s.path == scenePath && s.enabled);
        Add(inBuild, "Сцена в Build Settings",
            "Сцена не включена в Build Settings. Для Play из открытой сцены это не критично, " +
            "но LevelManager.LoadLevel на неё не перейдёт.", false);

        Add(Resources.Load("player/ControllerManager") != null, "Resources/player/ControllerManager",
            "Нет префаба игрока — GameManager не сможет его заспавнить.", true);

        TextAsset sceneList = Resources.Load<TextAsset>("others/SceneManager");
        Add(sceneList != null, "Resources/others/SceneManager.json",
            "Нет списка карт по режимам — режим придётся выбирать вручную.", false);

        Add(Resources.Load("PhotonServerSettings") != null, "PhotonServerSettings",
            "Нет PhotonServerSettings — PUN не инициализируется.", true);

        string bootPath = AssetDatabase.FindAssets("t:SceneAsset " + BootScene, new[] { "Assets/Levels" })
            .Select(AssetDatabase.GUIDToAssetPath)
            .FirstOrDefault(p => System.IO.Path.GetFileNameWithoutExtension(p) == BootScene);
        if (bootThroughMenu)
        {
            Add(bootPath != null, "Сцена " + BootScene + " найдена",
                "Без неё игру нечем инициализировать: сними галку или восстанови сцену.", true);
            Add(bootPath != null && EditorBuildSettings.scenes.Any(s => s.path == bootPath && s.enabled),
                BootScene + " включена в Build Settings",
                "Application.LoadLevel(\"" + BootScene + "\") не сработает, пока сцены нет в Build Settings.", true);
        }

        string sceneName = Path.GetFileNameWithoutExtension(scenePath);
        int mode = modeOverride >= 0 ? modeOverride : BSPlaytestModes.ResolveMode(sceneName);
        Add(mode >= 0, "Режим карты: " + (mode >= 0 ? ((GameMode)mode).ToString() : "не найден"),
            "Карты нет ни в одном режиме в SceneManager.json — выбери режим вручную в выпадающем списке.", false);

        SceneScan scan = SceneScan.Run(scenePath);
        Add(scan.hasGameManager, "В сцене есть GameManager",
            "Без GameManager игрок не появится: это не игровая карта или компонент потерян.", true);

        Add(scan.missingScripts.Count == 0,
            "Missing MonoBehaviour: " + scan.missingScripts.Count,
            scan.missingScripts.Count == 0 ? "" : "Битые ссылки на скрипты:\n  " + string.Join("\n  ", scan.missingScripts.Take(20).ToArray()),
            false);

        Add(scan.combinedMeshRenderers == 0,
            "Склеенная статическая геометрия: " + scan.combinedMeshRenderers + " рендереров",
            "Рендереры всё ещё смотрят в Combined Mesh — карта будет из летающих бочек. " +
            "Запусти tools/debatch_static_meshes.py.", true);

        Add(scan.dummyShaders.Count == 0,
            "Пустые/битые шейдеры: " + scan.dummyShaders.Count,
            scan.dummyShaders.Count == 0 ? "" : "Материалы с заглушкой вместо шейдера:\n  " + string.Join("\n  ", scan.dummyShaders.Take(20).ToArray()),
            false);

        Add(scan.missingLightmaps.Count == 0,
            "Лайтмапы: " + (scan.lightmapCount - scan.missingLightmaps.Count) + "/" + scan.lightmapCount + " на месте",
            scan.missingLightmaps.Count == 0 ? "" : "Нет текстур лайтмап:\n  " + string.Join("\n  ", scan.missingLightmaps.Take(20).ToArray()),
            false);

        foreach (Check c in checks)
        {
            log.AppendLine((c.error ? "ERROR  " : c.warning ? "WARN   " : "ok     ") + c.title +
                           (string.IsNullOrEmpty(c.detail) ? "" : "\n        " + c.detail.Replace("\n", "\n        ")));
        }

        preflightDone = true;
        int errors = checks.Count(c => c.error);
        int warnings = checks.Count(c => c.warning);
        if (errors > 0)
        {
            Debug.LogError(log.ToString());
            EditorUtility.DisplayDialog("Block Strike Playtest",
                "Запускать нельзя: " + errors + " ошибок, " + warnings + " предупреждений.\n\n" +
                string.Join("\n", checks.Where(c => c.error).Select(c => "• " + c.title).ToArray()) +
                "\n\nПодробности — в консоли.", "Понял");
        }
        else
        {
            Debug.Log(log.ToString());
            EditorUtility.DisplayDialog("Block Strike Playtest",
                warnings == 0
                    ? "Всё чисто. Жми «Подготовить плейтест»."
                    : "Ошибок нет, но есть " + warnings + " предупреждений (см. консоль). Можно готовить плейтест.",
                "OK");
        }
    }

    private void Add(bool ok, string title, string detail, bool fatal)
    {
        checks.Add(new Check
        {
            error = !ok && fatal,
            warning = !ok && !fatal,
            title = (ok ? "OK: " : fatal ? "ОШИБКА: " : "ВНИМАНИЕ: ") + title,
            detail = ok ? "" : detail
        });
    }

    // ------------------------------------------------------------------ //
    // arming
    // ------------------------------------------------------------------ //

    private void Arm(string scenePath, int resolvedMode)
    {
        int mode = modeOverride >= 0 ? modeOverride : resolvedMode;
        BSPlaytestSession session = new BSPlaytestSession
        {
            armed = true,
            ticket = Guid.NewGuid().ToString("N"),
            nick = string.IsNullOrEmpty(nick) ? "byvlal" : nick,
            scene = Path.GetFileNameWithoutExtension(scenePath),
            gameMode = mode,
            spawnPlayer = true,
            bootThroughMenu = bootThroughMenu,
            bootScene = BootScene,
            createdUtc = DateTime.UtcNow.ToString("o"),
            maxAgeMinutes = 180
        };

        Directory.CreateDirectory(Path.GetDirectoryName(BSPlaytestSession.FilePath));
        File.WriteAllText(BSPlaytestSession.FilePath, JsonUtility.ToJson(session, true));
        EditorPrefs.SetString(TicketKey, session.ticket);
        EditorPrefs.SetString(ArmedSceneKey, scenePath);

        EditorUtility.DisplayDialog("Block Strike Playtest",
            "Готово.\n\n" + (bootThroughMenu
                ? "Жми Play из любой сцены: плейтест сам поднимет " + BootScene +
                  ", сделает аккаунт и зайдёт на карту через оффлайн-комнату игры.\n"
                : "Открой карту «" + session.scene + "» и нажми Play.\n") +
            "Ник: " + session.nick + "\nРежим: " + (mode >= 0 ? ((GameMode)mode).ToString() : "TeamDeathmatch (по умолчанию)") + "\n\n" +
            "Сессия одноразовая: после выхода из Play её нужно взвести заново.",
            "Поехали");
        Debug.Log("[BS Playtest] armed: " + JsonUtility.ToJson(session));
    }

    private static void DisarmSession(string reason)
    {
        bool had = File.Exists(BSPlaytestSession.FilePath);
        try { if (had) File.Delete(BSPlaytestSession.FilePath); } catch { }
        EditorPrefs.DeleteKey(TicketKey);
        if (had)
        {
            Debug.Log("[BS Playtest] session cleared (" + reason + ").");
        }
    }

    internal static void DisarmFromGuard(string reason)
    {
        DisarmSession(reason);
    }
}

/// <summary>Crash guard: an armed session must never survive an editor restart.
/// Unity only honours [InitializeOnLoad] on top-level classes, so this lives
/// outside the window.</summary>
[InitializeOnLoad]
internal static class BSPlaytestStaleSessionGuard
{
    static BSPlaytestStaleSessionGuard()
    {
        if (!File.Exists(BSPlaytestSession.FilePath))
        {
            return;
        }
        // Entering play mode reloads the domain as well — do not eat the session
        // that the run which is just starting is about to consume.
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }
        BSPlaytestWindow.DisarmFromGuard("stale session found on editor start (Unity was closed or crashed while armed)");
        Debug.LogWarning(
            "[BS Playtest] Нашёл взведённую сессию с прошлого запуска редактора и снял её. " +
            "Если плейтест нужен — взведи заново: Tools > Block Strike > Playtest.");
    }
}

/// <summary>Scene → game mode, read from the game's own Resources/others/SceneManager.json.</summary>
public static class BSPlaytestModes
{
    public static int ResolveMode(string sceneName)
    {
        TextAsset asset = Resources.Load<TextAsset>("others/SceneManager");
        if (asset == null)
        {
            return -1;
        }
        // The file is a flat JSON array: [{"GameMode":"TeamDeathmatch","Scenes":["Bust",...]}, ...]
        foreach (Match block in Regex.Matches(asset.text, "\\{\\s*\"GameMode\"\\s*:\\s*\"([A-Za-z]+)\"\\s*,\\s*\"Scenes\"\\s*:\\s*\\[(.*?)\\]\\s*\\}", RegexOptions.Singleline))
        {
            string modeName = block.Groups[1].Value;
            foreach (Match scene in Regex.Matches(block.Groups[2].Value, "\"([^\"]+)\""))
            {
                if (string.Equals(scene.Groups[1].Value, sceneName, StringComparison.Ordinal))
                {
                    try { return (int)(GameMode)Enum.Parse(typeof(GameMode), modeName); }
                    catch { return -1; }
                }
            }
        }
        return -1;
    }
}

/// <summary>Static analysis of a scene file; nothing is loaded or modified.</summary>
public class SceneScan
{
    public bool hasGameManager;
    public int combinedMeshRenderers;
    public int lightmapCount;
    public List<string> missingScripts = new List<string>();
    public List<string> dummyShaders = new List<string>();
    public List<string> missingLightmaps = new List<string>();

    public static SceneScan Run(string scenePath)
    {
        SceneScan scan = new SceneScan();
        if (!File.Exists(scenePath))
        {
            return scan;
        }
        string text = File.ReadAllText(scenePath);

        HashSet<string> scriptGuids = new HashSet<string>();
        foreach (Match m in Regex.Matches(text, "m_Script: \\{fileID: (-?\\d+), guid: ([0-9a-f]{32})"))
        {
            scriptGuids.Add(m.Groups[2].Value);
        }
        foreach (string guid in scriptGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path))
            {
                scan.missingScripts.Add("guid " + guid + " (скрипт не найден в проекте)");
            }
            else if (path.EndsWith("GameManager.cs"))
            {
                scan.hasGameManager = true;
            }
        }

        HashSet<string> meshGuids = new HashSet<string>();
        foreach (Match m in Regex.Matches(text, "m_Mesh: \\{fileID: \\d+, guid: ([0-9a-f]{32})"))
        {
            meshGuids.Add(m.Groups[1].Value);
        }
        Dictionary<string, bool> combined = new Dictionary<string, bool>();
        foreach (string guid in meshGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            combined[guid] = !string.IsNullOrEmpty(path) && Path.GetFileName(path).StartsWith("Combined Mesh");
        }
        foreach (Match m in Regex.Matches(text, "m_Mesh: \\{fileID: \\d+, guid: ([0-9a-f]{32})"))
        {
            bool isCombined;
            if (combined.TryGetValue(m.Groups[1].Value, out isCombined) && isCombined)
            {
                scan.combinedMeshRenderers++;
            }
        }

        HashSet<string> materialGuids = new HashSet<string>();
        foreach (Match m in Regex.Matches(text, "- \\{fileID: 2100000, guid: ([0-9a-f]{32})"))
        {
            materialGuids.Add(m.Groups[1].Value);
        }
        foreach (string guid in materialGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path)) continue;
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) continue;
            if (mat.shader == null || mat.shader.name == "Hidden/InternalErrorShader")
            {
                scan.dummyShaders.Add(Path.GetFileName(path) + " → шейдер не загрузился");
                continue;
            }
            string shaderPath = AssetDatabase.GetAssetPath(mat.shader);
            if (!string.IsNullOrEmpty(shaderPath) && File.Exists(shaderPath))
            {
                string head = File.ReadAllText(shaderPath);
                if (head.Contains("Shader created for shader asset") || head.Contains("DummyShaderTextExporter"))
                {
                    scan.dummyShaders.Add(Path.GetFileName(path) + " → заглушка " + Path.GetFileName(shaderPath));
                }
            }
        }

        foreach (Match m in Regex.Matches(text, "m_Lightmap(?:Far|Near): \\{fileID: \\d+, guid: ([0-9a-f]{32})"))
        {
            scan.lightmapCount++;
            if (string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(m.Groups[1].Value)))
            {
                scan.missingLightmaps.Add("guid " + m.Groups[1].Value);
            }
        }
        return scan;
    }
}
