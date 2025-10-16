using Microsoft.AspNetCore.Mvc;
using InformesDinamicos.Data.Models;
using InformesDinamicos.Services;
using MongoDB.Driver;
using System.Text.Json;
using MongoDB.Bson.IO;

namespace InformesDinamicos.Controllers
{
    public class DataController : Controller
    {
        private readonly ShardingService _shardingService;
        private readonly RabbitConsumerService _rabbitConsumer;

        public DataController(ShardingService shardingService, RabbitConsumerService rabbitConsumer)
        {
            _shardingService = shardingService;
            _rabbitConsumer = rabbitConsumer;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetShardData(string database, string shard)
        {
            try
            {
                if (string.IsNullOrEmpty(database) || string.IsNullOrEmpty(shard))
                {
                    return Json(new { error = "Database y shard son requeridos" });
                }

                var db = _shardingService.GetSeccionDatabase(database);
                var collection = db.GetCollection<ClienteData>(shard);
                var data = await collection.Find(_ => true).Limit(50).ToListAsync();
                
                var result = data.Select(item => new {
                    clienteId = item.ClienteId,
                    institucionId = item.InstitucionId,
                    datos = GetDatosAsString(item.Datos),
                    lastUpdated = item.LastUpdated,
                    version = item.Version
                }).ToList();
                
                return Json(result);
            }
            catch (MongoException ex)
            {
                return Json(new { error = $"Error de base de datos: {ex.Message}" });
            }
            catch (ArgumentException ex)
            {
                return Json(new { error = $"Parámetros inválidos: {ex.Message}" });
            }
            catch (Exception ex)
            {
                return Json(new { error = $"Error inesperado: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ProcessEvent()
        {
            try
            {
                var processed = await _rabbitConsumer.ProcesarEventosEnCola();
                return Json(new { success = processed, message = processed ? "Evento procesado" : "No hay eventos" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private string GetDatosAsString(MongoDB.Bson.BsonDocument datos)
        {
            try
            {
                if (datos == null)
                    return "N/A";

                return datos.ToString();
            }
            catch
            {
                return "N/A";
            }
        }
    }
}