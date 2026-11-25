namespace SevenNineWorthy.Core
{
    /// <summary>A passing lift in comfort, from something read, heard or watched.</summary>
    public class Effect
    {
        public string Name { get; set; }

        /// <summary>How many more run launches this lasts.</summary>
        public int TurnsLeft { get; set; }

        /// <summary>What it added to the orbiter's comfort, taken back when it expires.</summary>
        public int ComfortBoost { get; set; }
    }
}
