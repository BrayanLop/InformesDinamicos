using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace InformesDinamicos.Data.Models
{
    public class ClienteData
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("clienteId")]
        public string ClienteId { get; set; }

        [BsonElement("lastUpdated")]
        public DateTime LastUpdated { get; set; }

        [BsonElement("datos")]
        public DatosCliente Datos { get; set; }
    }

    public class DatosCliente
    {
        [BsonElement("cursos")]
        public List<string> Cursos { get; set; } = new();

        [BsonElement("promedio")]
        public decimal Promedio { get; set; }
    }
}