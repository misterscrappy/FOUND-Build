using UnityEngine;

namespace Found.Core
{
    public static class FoundBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureGameRoot()
        {
            if (FoundGame.Instance != null) return;
            GameObject root = new GameObject("FOUND Game");
            root.AddComponent<FoundGame>();
        }
    }
}
