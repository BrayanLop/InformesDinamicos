using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace InformesDinamicos.Data.Models
{
    public class Institucion
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [JsonPropertyName("institucionId")]
        public string InstitucionId { get; set; } = string.Empty;
        
        [JsonPropertyName("nombre")]
        public string Nombre { get; set; } = string.Empty;
        
        [JsonPropertyName("codigo")]
        public string Codigo { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        public int Version { get; set; } = 1;
    }
}