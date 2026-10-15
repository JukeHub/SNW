namespace SevenNineWorthy.Core
{
    /// <summary>Turns the orbiter's comfort/media state into "state of mind" flavor text. No mechanical effect.</summary>
    public static class BuffNarrator
    {
        public static string Describe(Orbiter orbiter)
        {
            var media = orbiter.Media.GetLastConsumed();
            string mind;
            switch (media?.Type)
            {
                case MediaType.Music: mind = $"{media.Title} is still playing somewhere in the back of your mind."; break;
                case MediaType.Book: mind = $"You keep turning over a passage from {media.Title}."; break;
                case MediaType.Movie: mind = $"A scene from {media.Title} keeps replaying, softly."; break;
                default: mind = "Nothing's playing. The hum of the orbiter fills the quiet."; break;
            }

            if (orbiter.Comforts.Count > 0)
                mind += $" Around you: {string.Join(", ", orbiter.Comforts)}.";

            return "State of mind: " + mind;
        }
    }
}
