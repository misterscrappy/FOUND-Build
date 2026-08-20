using System.Collections.Generic;
using Found.Core;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Found.UI
{
    public sealed class FoundStateSelectorController : MonoBehaviour
    {
        [SerializeField] private Dropdown dropdown;
        [SerializeField] private string initialStateCode = "NY";
        [SerializeField] private Text availabilityText;
        [SerializeField] private UnityEvent onSelectionChanged;

        private readonly List<StateAlbumSummary> states = new List<StateAlbumSummary>();
        public string SelectedStateCode { get; private set; }

        private void Start()
        {
            Refresh();
        }

        public void Refresh()
        {
            if (FoundGame.Instance == null || dropdown == null) return;
            states.Clear();
            states.AddRange(FoundGame.Instance.Queries.GetStateAlbums(FoundGame.Instance.SaveData));
            List<string> labels = new List<string>(states.Count);
            int initialIndex = 0;
            for (int i = 0; i < states.Count; i++)
            {
                StateAlbumSummary state = states[i];
                string label = state.state.name;
                if (state.contentAvailable) label += "  " + state.foundCount + "/" + state.totalCount;
                else label += "  • COMING SOON";
                labels.Add(label);
                if (state.state.code == initialStateCode) initialIndex = i;
            }

            dropdown.onValueChanged.RemoveListener(OnDropdownChanged);
            dropdown.ClearOptions();
            dropdown.AddOptions(labels);
            dropdown.value = Mathf.Clamp(initialIndex, 0, Mathf.Max(0, labels.Count - 1));
            dropdown.RefreshShownValue();
            dropdown.onValueChanged.AddListener(OnDropdownChanged);
            ApplySelection(dropdown.value);
        }

        public void SelectState(string stateCode)
        {
            for (int i = 0; i < states.Count; i++)
            {
                if (states[i].state.code != stateCode) continue;
                dropdown.value = i;
                ApplySelection(i);
                return;
            }
        }

        private void OnDropdownChanged(int index)
        {
            ApplySelection(index);
        }

        private void ApplySelection(int index)
        {
            if (index < 0 || index >= states.Count) return;
            StateAlbumSummary selected = states[index];
            SelectedStateCode = selected.state.code;
            if (availabilityText != null)
            {
                availabilityText.text = selected.contentAvailable
                    ? selected.state.albumName + " • " + selected.foundCount + " / " + selected.totalCount
                    : selected.state.name + " content has not been authored yet.";
            }
            if (onSelectionChanged != null) onSelectionChanged.Invoke();
        }
    }
}
