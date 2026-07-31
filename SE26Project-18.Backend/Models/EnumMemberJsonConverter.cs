using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SE26Project_18.Backend.Models;

/// <summary>
/// Serializes enums using their [EnumMember(Value = "...")] attribute strings.
/// Falls back to the enum name if no attribute is present.
/// </summary>
public class EnumMemberJsonConverter : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert) => typeToConvert.IsEnum;

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var converterType = typeof(EnumMemberConverter<>).MakeGenericType(typeToConvert);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }

    private sealed class EnumMemberConverter<T> : JsonConverter<T> where T : struct, Enum
    {
        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = reader.GetString();
            if (value == null) return default;

            foreach (var field in typeToConvert.GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                var attr = field.GetCustomAttribute<EnumMemberAttribute>();
                if (attr?.Value == value)
                    return (T)field.GetValue(null)!;
            }

            if (Enum.TryParse<T>(value, ignoreCase: true, out var result))
                return result;

            return default;
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            var field = value.GetType().GetField(value.ToString());
            var attr = field?.GetCustomAttribute<EnumMemberAttribute>();
            writer.WriteStringValue(attr?.Value ?? value.ToString());
        }
    }
}