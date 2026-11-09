using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

/// <summary>
/// ใส่ระบบ Progression ให้อัตโนมัติทุกครั้งที่โหลดฉาก ไม่ต้องลากอะไรใส่ Scene
/// - MainMenu : ShopMenu
/// - InGame   : RunRewardTracker + ToolEffects + ProgressHUD
/// ถ้าวาง component เหล่านี้ไว้ใน Scene เองแล้ว จะไม่สร้างซ้ำ
/// </summary>
public static class ProgressionBootstrap
{
    public const string MenuSceneName = "MainMenu";
    public const string GameSceneName = "InGame";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Setup(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Setup(scene);
    }

    private static void Setup(Scene scene)
    {
        if (scene.name == MenuSceneName)
        {
            EnsureEventSystem();
            if (Object.FindAnyObjectByType<ShopMenu>() == null)
                new GameObject("ShopMenu").AddComponent<ShopMenu>();
        }
        else if (scene.name == GameSceneName)
        {
            EnsureEventSystem();
            if (Object.FindAnyObjectByType<RunRewardTracker>() != null) return;

            var go = new GameObject("Progression");
            go.AddComponent<RunRewardTracker>();
            go.AddComponent<ToolEffects>();
            go.AddComponent<ProgressHUD>();
        }
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindAnyObjectByType<EventSystem>() != null) return;
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }
}
