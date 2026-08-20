using System;
using System.Collections.Generic;
using UnityEngine;

namespace Found.UI
{
    [Serializable]
    public sealed class FoundScreenBinding
    {
        public string id;
        public GameObject root;
    }

    public sealed class FoundNavigationController : MonoBehaviour
    {
        [SerializeField] private string initialScreen = "explore";
        [SerializeField] private List<FoundScreenBinding> screens = new List<FoundScreenBinding>();

        public string CurrentScreen { get; private set; }
        public event Action<string> ScreenChanged;

        private void Start()
        {
            Show(initialScreen);
        }

        public void ShowExplore() { Show("explore"); }
        public void ShowCollection() { Show("collection"); }
        public void ShowTrade() { Show("trade"); }
        public void ShowProfile() { Show("profile"); }

        public void Show(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return;
            bool found = false;
            for (int i = 0; i < screens.Count; i++)
            {
                FoundScreenBinding screen = screens[i];
                if (screen == null || screen.root == null) continue;
                bool active = string.Equals(screen.id, id, StringComparison.OrdinalIgnoreCase);
                screen.root.SetActive(active);
                found |= active;
            }
            if (!found) throw new InvalidOperationException("FOUND screen is not registered: " + id);
            CurrentScreen = id;
            if (ScreenChanged != null) ScreenChanged(id);
        }
    }
}
