using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json;

namespace InformesDinamicos.Data.Models
{
    public class ClienteData
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("clienteId")]
        public string ClienteId { get; set; }

        [BsonElement("institucionId")]
        public string InstitucionId { get; set; }

        [BsonElement("lastUpdated")]
        public DateTime LastUpdated { get; set; }

        [BsonElement("datos")]
        public BsonDocument Datos { get; set; }

        [BsonElement("version")]
        public int Version { get; set; } = 1;
    }

    public class InstitucionInfo
    {
        public string Id { get; set; }
        public string Nombre { get; set; }
        public string Codigo { get; set; }
        public Dictionary<string, object> DatosAcademicos { get; set; }
        public Dictionary<string, object> DatosFinancieros { get; set; }
        public Dictionary<string, object> DatosComunidad { get; set; }
        public Dictionary<string, object> MetadatosAdicionales { get; set; }
    }
}