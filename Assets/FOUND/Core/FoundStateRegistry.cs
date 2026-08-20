using System;
using System.Collections.Generic;

namespace Found.Core
{
    public static class FoundStateRegistry
    {
        private static readonly string[,] States =
        {
            { "AL", "Alabama" }, { "AK", "Alaska" }, { "AZ", "Arizona" }, { "AR", "Arkansas" }, { "CA", "California" },
            { "CO", "Colorado" }, { "CT", "Connecticut" }, { "DE", "Delaware" }, { "FL", "Florida" }, { "GA", "Georgia" },
            { "HI", "Hawaii" }, { "ID", "Idaho" }, { "IL", "Illinois" }, { "IN", "Indiana" }, { "IA", "Iowa" },
            { "KS", "Kansas" }, { "KY", "Kentucky" }, { "LA", "Louisiana" }, { "ME", "Maine" }, { "MD", "Maryland" },
            { "MA", "Massachusetts" }, { "MI", "Michigan" }, { "MN", "Minnesota" }, { "MS", "Mississippi" }, { "MO", "Missouri" },
            { "MT", "Montana" }, { "NE", "Nebraska" }, { "NV", "Nevada" }, { "NH", "New Hampshire" }, { "NJ", "New Jersey" },
            { "NM", "New Mexico" }, { "NY", "New York" }, { "NC", "North Carolina" }, { "ND", "North Dakota" }, { "OH", "Ohio" },
            { "OK", "Oklahoma" }, { "OR", "Oregon" }, { "PA", "Pennsylvania" }, { "RI", "Rhode Island" }, { "SC", "South Carolina" },
            { "SD", "South Dakota" }, { "TN", "Tennessee" }, { "TX", "Texas" }, { "UT", "Utah" }, { "VT", "Vermont" },
            { "VA", "Virginia" }, { "WA", "Washington" }, { "WV", "West Virginia" }, { "WI", "Wisconsin" }, { "WY", "Wyoming" }
        };

        public static void EnsureAllStates(FoundCatalogData data)
        {
            if (data == null) throw new ArgumentNullException("data");
            if (data.states == null) data.states = new List<StateAlbumDefinition>();

            for (int row = 0; row < States.GetLength(0); row++)
            {
                string code = States[row, 0];
                string name = States[row, 1];
                StateAlbumDefinition existing = Find(data.states, code);
                if (existing != null)
                {
                    if (string.IsNullOrWhiteSpace(existing.name)) existing.name = name;
                    if (string.IsNullOrWhiteSpace(existing.albumName)) existing.albumName = name + " Album";
                    continue;
                }
                data.states.Add(new StateAlbumDefinition
                {
                    code = code,
                    name = name,
                    albumName = name + " Album",
                    subtitle = string.Empty,
                    completionText = string.Empty,
                    completionDesignId = string.Empty,
                    stampDesignIds = new List<string>()
                });
            }
        }

        private static StateAlbumDefinition Find(List<StateAlbumDefinition> states, string code)
        {
            for (int i = 0; i < states.Count; i++)
            {
                StateAlbumDefinition state = states[i];
                if (state != null && string.Equals(state.code, code, StringComparison.OrdinalIgnoreCase)) return state;
            }
            return null;
        }
    }
}
