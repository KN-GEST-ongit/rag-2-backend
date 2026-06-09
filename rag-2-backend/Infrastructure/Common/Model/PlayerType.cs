using System.Text.Json.Serialization;

namespace rag_2_backend.Infrastructure.Common.Model;

public enum PlayerType
{
    [JsonPropertyName("KEYBOARD")]
    Keyboard,
    [JsonPropertyName("SOCKET")]
    Socket
}