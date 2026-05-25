using System.Linq;
using UnityEditor;
using UnityEngine;

public static class EscapeRoomSceneAutoSetup
{
    private const string ScenesFolder = "Assets/Scenes";

    [MenuItem("Tools/Escape Room/Auto Setup All Scenes")]
    public static void AutoSetupAllScenes()
    {
        RunAutoSetup(showCompletionDialog: true);
    }

    public static void AutoSetupAllScenesSilent()
    {
        RunAutoSetup(showCompletionDialog: false);
    }

    private static void RunAutoSetup(bool showCompletionDialog)
    {
        bool builtPrototype = EscapeRoomPrototypeBuilder.BuildPrototypeScene(promptBeforeReplace: false, showCompletionDialog: false);
        if (!builtPrototype)
        {
            Debug.LogWarning("Automatsko postavljanje escape rooma je prekinuto prije izgradnje scene prototipa.");
            return;
        }

        string[] registeredScenes = RegisterAllScenesInBuildSettings();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Automatsko postavljanje escape rooma je dovršeno. Ponovno je izgrađena scena {EscapeRoomPrototypeBuilder.GeneratedScenePath} i registrirano je {registeredScenes.Length} scena u Build Settings.");

        if (showCompletionDialog)
        {
            EditorUtility.DisplayDialog(
                "Automatsko postavljanje je dovršeno",
                $"Ponovno je izgrađena generirana scena prototipa i registrirano je {registeredScenes.Length} scena u Build Settings. Nije potrebno ručno povezivanje.",
                "U redu");
        }
    }

    [MenuItem("Tools/Escape Room/Register All Scenes In Build Settings")]
    public static void RegisterScenesOnly()
    {
        string[] registeredScenes = RegisterAllScenesInBuildSettings();
        AssetDatabase.SaveAssets();

        Debug.Log($"Registrirano je {registeredScenes.Length} scena u Build Settings.");
        EditorUtility.DisplayDialog(
            "Scene su registrirane",
            $"Registrirano je {registeredScenes.Length} scena iz mape {ScenesFolder} u Build Settings.",
            "U redu");
    }

    private static string[] RegisterAllScenesInBuildSettings()
    {
        string[] scenePaths = AssetDatabase.FindAssets("t:Scene", new[] { ScenesFolder })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(path => path.EndsWith(".unity"))
            .OrderBy(path => path == EscapeRoomPrototypeBuilder.GeneratedScenePath ? 0 : 1)
            .ThenBy(path => path)
            .ToArray();

        EditorBuildSettings.scenes = scenePaths
            .Select(path => new EditorBuildSettingsScene(path, true))
            .ToArray();

        return scenePaths;
    }
}