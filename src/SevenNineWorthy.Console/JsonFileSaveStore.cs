using System.IO;
using Newtonsoft.Json;
using SevenNineWorthy.Core;

namespace SevenNineWorthy.ConsoleHost
{
    /// <summary>Stores the Orbiter as one JSON file on disk.</summary>
    public class JsonFileSaveStore : ISaveStore
    {
        private readonly string _path;

        public JsonFileSaveStore(string path)
        {
            _path = path;
        }

        public Orbiter Load()
        {
            if (!File.Exists(_path)) return null;

            // Let a corrupt save throw rather than quietly starting over and overwriting it.
            return JsonConvert.DeserializeObject<Orbiter>(File.ReadAllText(_path))
                   ?? throw new InvalidDataException($"Save file is empty: {_path}");
        }

        public void Save(Orbiter orbiter)
        {
            // Write then swap, so a crash mid-write can't destroy the existing save.
            string temp = _path + ".tmp";
            File.WriteAllText(temp, JsonConvert.SerializeObject(orbiter, Formatting.Indented));
            File.Move(temp, _path, overwrite: true);
        }
    }
}
