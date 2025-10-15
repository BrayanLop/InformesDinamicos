using Microsoft.AspNetCore.Mvc;
using InformesDinamicos.Data.Models;
using InformesDinamicos.Services;
using MongoDB.Driver;

namespace InformesDinamicos.Controllers
{
    public class TestController : Controller
    {
        private readonly ShardingService _shardingService;
        private readonly RabbitConsumerService _rabbitConsumer;

        public TestController(ShardingService shardingService, RabbitConsumerService rabbitConsumer)
        {
            _shardingService = shardingService;
            _rabbitConsumer = rabbitConsumer;
        }

        public async Task<IActionResult> CrearDatos()
        {
            var clientes = new[]
            {
                new ClienteData { ClienteId = "12345607", Datos = new DatosCliente { Cursos = new List<string> {"Math", "Science"}, Promedio = 85.5m }, LastUpdated = DateTime.UtcNow },
                new ClienteData { ClienteId = "12345615", Datos = new DatosCliente { Cursos = new List<string> {"History"}, Promedio = 92.0m }, LastUpdated = DateTime.UtcNow }
            };

            foreach (var cliente in clientes)
            {
                var collection = _shardingService.GetClienteCollection(cliente.ClienteId, "Academico");
                await collection.InsertOneAsync(cliente);
            }

            return Ok("Datos creados");
        }

        public async Task<IActionResult> VerDatos()
        {
            var shards = new[] { "Academico_0_10", "Academico_11_20", "Comunidad_0_10", "Comunidad_11_20" };
            var resultado = new Dictionary<string, object>();

            foreach (var shard in shards)
            {
                var collection = _shardingService.GetShardCollection(shard);
                var datos = await collection.Find(_ => true).ToListAsync();
                resultado[shard] = datos;
            }

            return Json(resultado);
        }

        public async Task<IActionResult> ProcesarEventos()
        {
            var eventosProcessados = await _rabbitConsumer.ProcesarEventosEnCola();
            return Ok($"Se procesaron {eventosProcessados} eventos de RabbitMQ");
        }
    }
}