using System.IO;
using Newtonsoft.Json;

namespace SevenNineWorthy.Core
{
    /// <summary>Reads a content/config file and fails loudly, naming the file, if it is missing or malformed.</summary>
    internal static class JsonFile
    {
        private static readonly JsonSerializerSettings Strict = new JsonSerializerSettings
        {
            // A misspelled key is an error, not a silently ignored field.
            MissingMemberHandling = MissingMemberHandling.Error,
        };

        public static T Read<T>(string path)
        {
            if (!File.Exists(path))
                throw new GameDataException($"Data file not found: {Path.GetFullPath(path)}");

            T result;
            try
            {
                result = JsonConvert.DeserializeObject<T>(File.ReadAllText(path), Strict);
            }
            catch (JsonException e)
            {
                throw new GameDataException($"Malformed data in {Path.GetFileName(path)}: {e.Message}", e);
            }

            if (result == null)
                throw new GameDataException($"Data file is empty: {Path.GetFileName(path)}");
            return result;
        }
    }

    public class GameDataException : System.Exception
    {
        public GameDataException(string message, System.Exception inner = null) : base(message, inner) { }
    }
}
