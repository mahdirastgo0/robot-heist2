#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// راه‌اندازی خودکار پروژه در اولین باز شدن:
// ساخت Scene خالی، اضافه به Build Settings، تنظیمات Player و هشدار اگه DLL نبود.
[InitializeOnLoad]
public static class ProjectSetup
{
    const string ScenePath = "Assets/Scenes/Main.unity";
    const string DllPath = "Assets/Plugins/x86_64/RobotHeistCore.dll";

    static ProjectSetup() { EditorApplication.delayCall += AutoRun; }

    // شیدرهایی که فقط با کد ساخته میشن باید تو بیلد هم باشن
    static void AddAlwaysIncludedShader(string name)
    {
        var sh = Shader.Find(name);
        if (sh == null) return;
        var gs = AssetDatabase.LoadAssetAtPath<Object>("ProjectSettings/GraphicsSettings.asset");
        if (gs == null) return;
        var so = new SerializedObject(gs);
        var arr = so.FindProperty("m_AlwaysIncludedShaders");
        for (int i = 0; i < arr.arraySize; i++) if (arr.GetArrayElementAtIndex(i).objectReferenceValue == sh) return;
        arr.arraySize++;
        arr.GetArrayElementAtIndex(arr.arraySize - 1).objectReferenceValue = sh;
        so.ApplyModifiedProperties();
    }

    static void AutoRun()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        AddAlwaysIncludedShader("Skybox/Panoramic");
        AddAlwaysIncludedShader("Standard");
        // فقط تو پروژه‌ی خالی؛ اگه از قبل Scene داری دست نمی‌زنیم
        if (AssetDatabase.FindAssets("t:Scene").Length == 0) Setup();
        else WarnIfNoDll();
    }

    [MenuItem("Robot Heist/Setup Project")]
    static void Setup()
    {
        if (!File.Exists(ScenePath))
        {
            Directory.CreateDirectory("Assets/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        bool inBuild = false;
        foreach (var s in EditorBuildSettings.scenes) if (s.path == ScenePath) inBuild = true;
        if (!inBuild) EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

        PlayerSettings.companyName = "RobotHeist";
        PlayerSettings.productName = "Robot Heist";
        PlayerSettings.runInBackground = true;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.defaultScreenWidth = 1280;
        PlayerSettings.defaultScreenHeight = 720;

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Robot Heist] Project is ready. Press Play.");
        WarnIfNoDll();
    }

    static void WarnIfNoDll()
    {
        if (!File.Exists(DllPath))
            Debug.LogWarning("[Robot Heist] RobotHeistCore.dll not found in Assets/Plugins/x86_64. " +
                             "Run native/build_windows.bat first (see README). The game still runs, but without the C++ core.");
    }
}
#endif
