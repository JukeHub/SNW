namespace SevenNineWorthy.Core
{
    /// <summary>Persists the Orbiter (the only thing that survives between runs).</summary>
    public interface ISaveStore
    {
        /// <summary>Returns the saved Orbiter, or null if there is no save yet.</summary>
        Orbiter Load();

        void Save(Orbiter orbiter);
    }
}
