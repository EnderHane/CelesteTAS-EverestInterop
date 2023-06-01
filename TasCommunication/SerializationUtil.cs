using System.Text.Json;

namespace TasCommunication;

public static class SerializationUtil {

    private static readonly JsonSerializerOptions Option_ = new() {
        IncludeFields = true,
    };

    public static byte[] SerializeToUtf8JsonBytes<T>(T obj) {
        return JsonSerializer.SerializeToUtf8Bytes(obj, Option_);
    }

    public static T DeserializeUtf8JsonBytes<T>(byte[] json) {
        if (json == null) {
            return default;
        }
        Utf8JsonReader reader = new(json);
        return JsonSerializer.Deserialize<T>(ref reader, Option_) ?? default;
    }

}