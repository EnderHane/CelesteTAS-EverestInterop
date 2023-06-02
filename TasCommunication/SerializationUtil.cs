using System.Text;
using System.Text.Json;

namespace TasCommunication;

public static class SerializationUtil {

    private static readonly JsonSerializerOptions Option_ = new() {
        IncludeFields = true,
    };

    public static byte[] SerializeToUtf8JsonBytes<T>(T obj) {
        string s = JsonSerializer.Serialize(obj, Option_);
        return Encoding.UTF8.GetBytes(s);
    }

    public static T DeserializeUtf8JsonBytes<T>(byte[] json) {
        if (json == null) {
            return default;
        }
        string s = Encoding.UTF8.GetString(json);
        T result = JsonSerializer.Deserialize<T>(s, Option_);
        return result ?? default;
    }

}