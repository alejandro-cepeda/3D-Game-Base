using UnityEngine;

public static class GameBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureGameManager()
    {
        if (Object.FindFirstObjectByType<GameManager>() != null)
        {
            return;
        }

        GameObject obj = new GameObject("GameManager");
        obj.AddComponent<GameManager>();
    }
}
