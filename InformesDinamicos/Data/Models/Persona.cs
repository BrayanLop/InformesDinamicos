using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace InformesDinamicos.Data.Models
{
    public class Persona
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        public string PersonaId { get; set; } = string.Empty;
        public string InstitucionId { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
        public int Edad { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        public int Version { get; set; } = 1;
    }
}