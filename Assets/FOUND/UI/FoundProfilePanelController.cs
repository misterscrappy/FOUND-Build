using System.Collections.Generic;
using Found.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Found.UI
{
    public sealed class FoundProfilePanelController : MonoBehaviour
    {
        [SerializeField] private InputField displayNameInput;
        [SerializeField] private Text levelText;
        [SerializeField] private Text xpText;
        [SerializeField] private Text coinsText;
        [SerializeField] private Text collectionText;
        [SerializeField] private Text statesText;

        private void OnEnable()
        {
            Refresh();
        }

        public void Refresh()
        {
            FoundGame game = FoundGame.Instance;
            if (game == null) return;
            PlayerProfile profile = game.SaveData.profile;
            if (displayNameInput != null && !displayNameInput.isFocused) displayNameInput.text = profile.displayName;
            if (levelText != null) levelText.text = "COLLECTOR LEVEL " + profile.level;
            if (xpText != null) xpText.text = profile.xp + " / " + FoundRules.XpNeededForLevel(profile.level) + " XP";
            if (coinsText != null) coinsText.text = profile.coins.ToString("N0") + " COINS";

            List<OwnedStampSummary> owned = game.Queries.GetOwnedCopies(game.SaveData);
            if (collectionText != null) collectionText.text = owned.Count.ToString("N0") + " PHYSICAL COPIES";

            List<StateAlbumSummary> states = game.Queries.GetStateAlbums(game.SaveData);
            int complete = 0;
            int active = 0;
            for (int i = 0; i < states.Count; i++)
            {
                if (states[i].contentAvailable) active++;
                if (states[i].complete) complete++;
            }
            if (statesText != null) statesText.text = complete + " COMPLETE • " + active + " ACTIVE • 50 STATES";
        }

        public void SaveDisplayName()
        {
            if (FoundGame.Instance == null) return;
            FoundGame.Instance.SetDisplayName(displayNameInput == null ? string.Empty : displayNameInput.text);
            Refresh();
        }
    }
}
