using System.Text.Json;
using System.Text.Json.Serialization;

namespace InformesDinamicos.Data.Models
{
    public class EventoJack
    {
        [JsonPropertyName("Id")]
        public string Id { get; set; }

        [JsonPropertyName("ClienteId")]
        public string ClienteId { get; set; }



        [JsonPropertyName("Seccion")]
        public string Seccion { get; set; }

        [JsonPropertyName("RegistroId")]
        public string RegistroId { get; set; }

        [JsonPropertyName("Cambio")]
        public JsonElement Cambio { get; set; }

        [JsonPropertyName("Timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonPropertyName("Version")]
        public int Version { get; set; } = 1;
    }
}