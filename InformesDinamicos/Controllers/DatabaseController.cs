using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using InformesDinamicos.Services;
using InformesDinamicos.Data.Models;

namespace InformesDinamicos.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DatabaseController : ControllerBase
    {
        private readonly ShardingService _shardingService;
        private readonly IMongoClient _mongoClient;

        public DatabaseController(ShardingService shardingService, IMongoClient mongoClient)
        {
            _shardingService = shardingService;
            _mongoClient = mongoClient;
        }

        [HttpGet("databases")]
        public async Task<IActionResult> GetDatabases()
        {
            var databases = await _mongoClient.ListDatabaseNamesAsync();
            return Ok(await databases.ToListAsync());
        }

        [HttpGet("{seccion}/collections")]
        public async Task<IActionResult> GetCollections(string seccion)
        {
            var database = _shardingService.GetSeccionDatabase(seccion);
            var collections = await database.ListCollectionNamesAsync();
            return Ok(await collections.ToListAsync());
        }

        [HttpGet("{seccion}/stats")]
        public async Task<IActionResult> GetSeccionStats(string seccion)
        {
            var database = _shardingService.GetSeccionDatabase(seccion);
            var collections = await database.ListCollectionNamesAsync();
            var collectionList = await collections.ToListAsync();
            
            var stats = new List<object>();
            foreach (var collectionName in collectionList)
            {
                var collection = database.GetCollection<ClienteData>(collectionName);
                var count = await collection.CountDocumentsAsync(_ => true);
                stats.Add(new { Collection = collectionName, DocumentCount = count });
            }
            
            return Ok(new { Seccion = seccion, Collections = stats });
        }
    }
}