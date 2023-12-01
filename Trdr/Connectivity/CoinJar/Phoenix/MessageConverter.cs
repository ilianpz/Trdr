using System.Text.Json;
using System.Text.Json.Serialization;

namespace Trdr.Connectivity.CoinJar.Phoenix;

internal sealed class MessageConverter : JsonConverter<Message>
{
    public override Message Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        Utf8JsonReader readerAtStart = reader; // Copy so we can reset the reader if needed

        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException();

        string? topic = null;
        string? @event = null;

        while (reader.Read())
        {
            topic ??= TryReadProperty(ref reader, Message.TopicProperty);
            @event ??= TryReadProperty(ref reader, Message.EventProperty);

            if (topic != null && @event != null)
            {
                reader = readerAtStart; // Reset the reader so we can parse from the beginning

                if (@event == EventType.Reply)
                    return JsonSerializer.Deserialize<MessageWithPayload<ReplyPayload>>(ref reader) ?? throw new JsonException();
                if (topic.StartsWith(TopicType.Ticker + ":"))
                    return JsonSerializer.Deserialize<MessageWithPayload<TickerPayload>>(ref reader) ?? throw new JsonException();

                return JsonSerializer.Deserialize<Message>(ref reader) ?? throw new JsonException();
            }
        }

        throw new JsonException();
    }

    public override void Write(Utf8JsonWriter writer, Message value, JsonSerializerOptions options)
    {
        throw new NotSupportedException();
    }

    private static string? TryReadProperty(ref Utf8JsonReader reader, string propertyName)
    {
        if (reader.TokenType == JsonTokenType.PropertyName && reader.GetString() == propertyName)
        {
            if (!reader.Read() || reader.TokenType != JsonTokenType.String)
                throw new JsonException();

            return reader.GetString();
        }

        return null;
    }
}