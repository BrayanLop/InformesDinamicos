using InformesDinamicos.Data;
using InformesDinamicos.Data.Models;
using MongoDB.Driver;

namespace InformesDinamicos.Services
{
    public class ShardingService
    {
        private readonly MongoDbContext _mongoContext;

        public ShardingService(MongoDbContext mongoContext)
        {
            _mongoContext = mongoContext;
        }

        public string DeterminarShard(string clienteId, string seccion)
        {
            var clienteIdNum = int.Parse(clienteId.Substring(clienteId.Length - 2));
            var rango = clienteIdNum <= 10 ? "0_10" : "11_20";
            return $"{seccion}_{rango}";
        }

        public IMongoCollection<ClienteData> GetClienteCollection(string clienteId, string seccion)
        {
            var shardName = DeterminarShard(clienteId, seccion);
            return _mongoContext.GetCollection<ClienteData>(shardName);
        }

        public IMongoCollection<ClienteData> GetShardCollection(string shardName)
        {
            return _mongoContext.GetCollection<ClienteData>(shardName);
        }
    }
}