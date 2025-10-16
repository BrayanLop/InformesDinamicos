using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace InformesDinamicos.Data.Models
{
    public class Institucion
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        public string InstitucionId { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Codigo { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        public int Version { get; set; } = 1;
    }
}