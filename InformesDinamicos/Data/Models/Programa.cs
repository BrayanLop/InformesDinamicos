using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace InformesDinamicos.Data.Models
{
    public class Programa
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        public string ProgramaId { get; set; } = string.Empty;
        public string InstitucionId { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public int Nivel { get; set; }
        public double Promedio { get; set; }
        public int Creditos { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        public int Version { get; set; } = 1;
    }
}