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
        
        // Nuevos campos para CRUD normalizado
        [JsonPropertyName("Tipo")]
        public string Tipo { get; set; } = string.Empty; // "crear", "actualizar", "eliminar"
        
        [JsonPropertyName("Entidad")]
        public string Entidad { get; set; } = string.Empty; // "institucion", "programa", "asignatura", "persona"
        
        [JsonPropertyName("Datos")]
        public JsonElement Datos { get; set; } // Datos de la entidad
    }
}