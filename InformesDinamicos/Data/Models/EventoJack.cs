using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace InformesDinamicos.Data.Models
{
    public class EventoJack
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("clienteId")]
        public string ClienteId { get; set; }

        [BsonElement("seccion")]
        public string Seccion { get; set; }

        [BsonElement("registroId")]
        public string RegistroId { get; set; }

        [BsonElement("cambio")]
        public object Cambio { get; set; }

        [BsonElement("timestamp")]
        public DateTime Timestamp { get; set; }
    }
}