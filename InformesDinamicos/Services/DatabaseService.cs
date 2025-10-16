using MongoDB.Driver;
using InformesDinamicos.Data.Models;

namespace InformesDinamicos.Services
{
    public class DatabaseService
    {
        private readonly IMongoClient _mongoClient;
        private readonly IConfiguration _configuration;

        public DatabaseService(IConfiguration configuration)
        {
            _configuration = configuration;
            var connectionString = _configuration.GetConnectionString("MongoDB");
            _mongoClient = new MongoClient(connectionString);
        }

        // BD Académico
        public IMongoDatabase GetAcademicoDatabase() => _mongoClient.GetDatabase("Academico");
        public IMongoCollection<Programa> GetProgramasCollection() => GetAcademicoDatabase().GetCollection<Programa>("programas");
        public IMongoCollection<Asignatura> GetAsignaturasCollection() => GetAcademicoDatabase().GetCollection<Asignatura>("asignaturas");

        // BD Comunidad  
        public IMongoDatabase GetComunidadDatabase() => _mongoClient.GetDatabase("Comunidad");
        public IMongoCollection<Persona> GetPersonasCollection() => GetComunidadDatabase().GetCollection<Persona>("personas");

        // BD General
        public IMongoDatabase GetGeneralDatabase() => _mongoClient.GetDatabase("General");
        public IMongoCollection<Institucion> GetInstitucionesCollection() => GetGeneralDatabase().GetCollection<Institucion>("instituciones");

        // Determinar shard por InstitucionId
        public string DeterminarShard(string institucionId)
        {
            var hash = institucionId.GetHashCode();
            var shardNumber = Math.Abs(hash) % 2;
            return shardNumber == 0 ? "1_10" : "11_20";
        }

        // Métodos async para informes
        public async Task<List<Institucion>> GetInstitucionesAsync()
        {
            return await GetInstitucionesCollection().Find(_ => true).ToListAsync();
        }

        public async Task<List<Programa>> GetProgramasAsync(string? institucionId = null)
        {
            var filter = string.IsNullOrEmpty(institucionId) ? 
                Builders<Programa>.Filter.Empty : 
                Builders<Programa>.Filter.Eq(p => p.InstitucionId, institucionId);
            return await GetProgramasCollection().Find(filter).ToListAsync();
        }

        public async Task<List<Asignatura>> GetAsignaturasAsync()
        {
            return await GetAsignaturasCollection().Find(_ => true).ToListAsync();
        }

        public async Task<List<Persona>> GetPersonasAsync()
        {
            return await GetPersonasCollection().Find(_ => true).ToListAsync();
        }
    }
}