using InformesDinamicos.Data;
using InformesDinamicos.Data.Models;
using MongoDB.Driver;

namespace InformesDinamicos.Services
{
    public class ShardingService
    {
        private readonly IMongoClient _mongoClient;
        private readonly string _connectionString;

        public ShardingService(IMongoClient mongoClient, IConfiguration configuration)
        {
            _mongoClient = mongoClient;
            _connectionString = configuration.GetConnectionString("MongoDB");
        }

        public string DeterminarShard(string clienteId)
        {
            // Si es un número, usar la lógica original
            if (int.TryParse(clienteId, out var clienteIdNum))
            {
                return clienteIdNum <= 10 ? "1_10" : "11_20";
            }
            
            // Si es un GUID u otro string, usar hash para determinar shard
            var hash = clienteId.GetHashCode();
            var positiveHash = Math.Abs(hash);
            return (positiveHash % 2 == 0) ? "1_10" : "11_20";
        }

        public IMongoCollection<ClienteData> GetClienteCollection(string clienteId, string seccion)
        {
            var database = _mongoClient.GetDatabase(seccion);
            var shardName = $"{seccion}_{DeterminarShard(clienteId)}";
            return database.GetCollection<ClienteData>(shardName);
        }

        public IMongoDatabase GetSeccionDatabase(string seccion)
        {
            return _mongoClient.GetDatabase(seccion);
        }
        
        public IMongoDatabase GetDatabase(string databaseName)
        {
            return _mongoClient.GetDatabase(databaseName);
        }
    }
}