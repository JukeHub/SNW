namespace SevenNineWorthy.Core
{
    /// <summary>All input to the core comes through here: one key per choice.</summary>
    public interface IInputSource
    {
        /// <summary>
        /// Blocks until the player presses one of <paramref name="validKeys"/> and returns it upper-cased.
        /// </summary>
        char ReadKey(string validKeys);
    }
}
