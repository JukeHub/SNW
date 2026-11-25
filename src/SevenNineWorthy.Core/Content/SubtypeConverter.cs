using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace SevenNineWorthy.Core
{
    /// <summary>
    /// Reads and writes a class that has subclasses, using a "type" field to say which one:
    /// { "type": "book", ... } becomes a Book. The type is the class name, so a new subclass
    /// needs no registration. Content files and the save file both go through here.
    /// </summary>
    public class SubtypeConverter : JsonConverter
    {
        private const string TypeField = "type";

        public override bool CanConvert(Type objectType) => true; // Only ever attached by attribute.

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;

            var json = JObject.Load(reader);
            var typeName = json.Property(TypeField, StringComparison.OrdinalIgnoreCase);
            Type type = objectType;

            if (typeName != null)
            {
                string name = (string)typeName.Value;
                type = objectType.Assembly.GetTypes().FirstOrDefault(t =>
                           objectType.IsAssignableFrom(t) && !t.IsAbstract &&
                           string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase))
                       ?? throw new JsonSerializationException($"'{name}' is not a kind of {objectType.Name}.");
                typeName.Remove();
            }
            else if (type.IsAbstract)
            {
                throw new JsonSerializationException($"Every {objectType.Name} needs a \"{TypeField}\" field saying which kind it is.");
            }

            // Populate fills in an object we made ourselves, so it doesn't come back through this converter.
            object result = Activator.CreateInstance(type);
            serializer.Populate(json.CreateReader(), result);
            return result;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            string name = value.GetType().Name;
            var contract = (JsonObjectContract)serializer.ContractResolver.ResolveContract(value.GetType());

            writer.WriteStartObject();
            writer.WritePropertyName(TypeField);
            writer.WriteValue(char.ToLowerInvariant(name[0]) + name.Substring(1));
            foreach (var property in contract.Properties.Where(p => !p.Ignored && p.Readable))
            {
                writer.WritePropertyName(property.PropertyName);
                serializer.Serialize(writer, property.ValueProvider.GetValue(value));
            }
            writer.WriteEndObject();
        }
    }
}
