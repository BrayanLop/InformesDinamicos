using Microsoft.AspNetCore.Mvc;
using InformesDinamicos.Data.Models;
using InformesDinamicos.Services;
using MongoDB.Driver;

namespace InformesDinamicos.Controllers
{
    public class HomeController : Controller
    {
        private readonly ShardingService _shardingService;

        public HomeController(ShardingService shardingService)
        {
            _shardingService = shardingService;
        }

        public async Task<IActionResult> Index(string clienteId = "", string seccion = "Academico")
        {
            if (string.IsNullOrEmpty(clienteId))
            {
                return View(new List<ClienteData>());
            }

            var collection = _shardingService.GetClienteCollection(clienteId, seccion);
            var cliente = await collection.Find(x => x.ClienteId == clienteId).FirstOrDefaultAsync();
            
            var result = cliente != null ? new List<ClienteData> { cliente } : new List<ClienteData>();
            return View(result);
        }

        public async Task<IActionResult> Shard(string shardName)
        {
            var collection = _shardingService.GetShardCollection(shardName);
            var clientes = await collection.Find(_ => true)
                .SortByDescending(c => c.LastUpdated)
                .Limit(20)
                .ToListAsync();
            
            return View("Index", clientes);
        }
    }
}