using Microsoft.AspNetCore.Mvc;
using InformesDinamicos.Data.Models;
using InformesDinamicos.Services;
using MongoDB.Driver;
using MongoDB.Bson;

namespace InformesDinamicos.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InsertController : ControllerBase
    {
        private readonly ShardingService _shardingService;

        public InsertController(ShardingService shardingService)
        {
            _shardingService = shardingService;
        }

        [HttpPost("datos")]
        public async Task<IActionResult> InsertarDatos([FromBody] InsertarDatosRequest request)
        {
            try
            {
                var shard = _shardingService.DeterminarShard(request.ClienteId);
                var db = _shardingService.GetSeccionDatabase(request.Seccion);
                var collection = db.GetCollection<ClienteData>($"{request.Seccion}_{shard}");
                
                var datosCompletos = BsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(request.Datos));
                
                var clienteData = new ClienteData
                {
                    ClienteId = request.ClienteId,
                    InstitucionId = request.InstitucionId,
                    Datos = datosCompletos,
                    LastUpdated = DateTime.UtcNow,
                    Version = request.Version
                };

                var filter = Builders<ClienteData>.Filter.Eq(x => x.ClienteId, request.ClienteId);
                await collection.ReplaceOneAsync(filter, clienteData, new ReplaceOptions { IsUpsert = true });

                return Ok(new { 
                    mensaje = "Datos insertados correctamente",
                    clienteId = request.ClienteId,
                    seccion = request.Seccion,
                    institucionId = request.InstitucionId
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("crear-cliente")]
        public async Task<IActionResult> CrearCliente(string clienteId, string institucionId, string seccion)
        {
            try
            {
                var datos = seccion.ToLower() == "academico" 
                    ? new { programa = "Programa Test", semestre = 5, promedio = 4.0, creditos = 100, estado = "Activo" }
                    : (object)new { cargo = "Estudiante", eventos_participados = 3, proyectos_activos = 1 };

                var shard = _shardingService.DeterminarShard(clienteId);
                var db = _shardingService.GetSeccionDatabase(seccion);
                var collection = db.GetCollection<ClienteData>($"{seccion}_{shard}");
                
                var datosCompletos = BsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(datos));
                
                var clienteData = new ClienteData
                {
                    ClienteId = clienteId,
                    InstitucionId = institucionId,
                    Datos = datosCompletos,
                    LastUpdated = DateTime.UtcNow,
                    Version = 1
                };

                var filter = Builders<ClienteData>.Filter.Eq(x => x.ClienteId, clienteId);
                await collection.ReplaceOneAsync(filter, clienteData, new ReplaceOptions { IsUpsert = true });

                return Ok(new { mensaje = "Cliente creado", clienteId, seccion, shard });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("datos-prueba")]
        public async Task<IActionResult> InsertarDatosPrueba()
        {
            try
            {
                var registros = new[]
                {
                    // Shard 1_10 (IDs 1-10)
                    new { ClienteId = "5", InstitucionId = "INST_001", Seccion = "Academico", 
                          Datos = (object)new { programa = "Ingeniería de Sistemas", semestre = 8, promedio = 4.2, creditos = 145, estado = "Activo" } },
                    new { ClienteId = "5", InstitucionId = "INST_001", Seccion = "Comunidad", 
                          Datos = (object)new { cargo = "Estudiante", eventos_participados = 5, proyectos_activos = 2 } },
                    
                    new { ClienteId = "8", InstitucionId = "INST_002", Seccion = "Academico", 
                          Datos = (object)new { programa = "Medicina", semestre = 6, promedio = 4.5, creditos = 120, estado = "Activo" } },
                    new { ClienteId = "8", InstitucionId = "INST_002", Seccion = "Comunidad", 
                          Datos = (object)new { cargo = "Estudiante", eventos_participados = 8, proyectos_activos = 1 } },
                    
                    // Shard 11_20 (IDs 11-20)
                    new { ClienteId = "15", InstitucionId = "INST_003", Seccion = "Academico", 
                          Datos = (object)new { programa = "Derecho", semestre = 4, promedio = 3.8, creditos = 80, estado = "Activo" } },
                    new { ClienteId = "15", InstitucionId = "INST_003", Seccion = "Comunidad", 
                          Datos = (object)new { cargo = "Representante", eventos_participados = 12, proyectos_activos = 3 } },
                    
                    new { ClienteId = "18", InstitucionId = "INST_004", Seccion = "Academico", 
                          Datos = (object)new { programa = "Psicología", semestre = 7, promedio = 4.1, creditos = 130, estado = "Activo" } },
                    new { ClienteId = "18", InstitucionId = "INST_004", Seccion = "Comunidad", 
                          Datos = (object)new { cargo = "Monitor", eventos_participados = 6, proyectos_activos = 1 } }
                };

                var resultados = new List<object>();

                foreach (var registro in registros)
                {
                    var shard = _shardingService.DeterminarShard(registro.ClienteId);
                    var db = _shardingService.GetSeccionDatabase(registro.Seccion);
                    var collection = db.GetCollection<ClienteData>($"{registro.Seccion}_{shard}");
                    
                    var datosCompletos = BsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(registro.Datos));
                    
                    var clienteData = new ClienteData
                    {
                        ClienteId = registro.ClienteId,
                        InstitucionId = registro.InstitucionId,
                        Datos = datosCompletos,
                        LastUpdated = DateTime.UtcNow,
                        Version = 1
                    };

                    var filter = Builders<ClienteData>.Filter.Eq(x => x.ClienteId, registro.ClienteId);
                    await collection.ReplaceOneAsync(filter, clienteData, new ReplaceOptions { IsUpsert = true });
                    
                    resultados.Add(new { 
                        clienteId = registro.ClienteId, 
                        seccion = registro.Seccion, 
                        institucion = registro.InstitucionId,
                        estado = "insertado" 
                    });
                }

                return Ok(new { 
                    mensaje = "Datos de prueba insertados correctamente",
                    total_registros = registros.Length,
                    resultados = resultados
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }

    public class InsertarDatosRequest
    {
        public string ClienteId { get; set; }
        public string InstitucionId { get; set; }
        public string Seccion { get; set; }
        public object Datos { get; set; }
        public int Version { get; set; } = 1;
    }
}