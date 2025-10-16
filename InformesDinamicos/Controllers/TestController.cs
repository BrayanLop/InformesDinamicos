using Microsoft.AspNetCore.Mvc;
using InformesDinamicos.Data.Models;
using InformesDinamicos.Services;
using MongoDB.Driver;
using System.Text.Json;
using MongoDB.Bson;

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
            try
            {
                var clientes = new[]
                {
                    // Shard 1_10
                    new { 
                        Id = "5", 
                        Inst = "INST_001",
                        DatosAcademicos = new BsonDocument {
                            { "programa", "Ingeniería de Sistemas" },
                            { "semestre", 8 },
                            { "promedio", 4.2 },
                            { "creditos", 145 },
                            { "estado", "Activo" }
                        },
                        DatosComunidad = new BsonDocument {
                            { "cargo", "Estudiante" },
                            { "eventos_participados", 5 },
                            { "proyectos_activos", 2 }
                        }
                    },
                    // Shard 11_20
                    new { 
                        Id = "15", 
                        Inst = "INST_002",
                        DatosAcademicos = new BsonDocument {
                            { "programa", "Medicina" },
                            { "semestre", 6 },
                            { "promedio", 4.5 },
                            { "creditos", 120 },
                            { "estado", "Activo" }
                        },
                        DatosComunidad = new BsonDocument {
                            { "cargo", "Estudiante" },
                            { "eventos_participados", 8 },
                            { "proyectos_activos", 1 }
                        }
                    }
                };

                foreach (var cliente in clientes)
                {
                    var shard = _shardingService.DeterminarShard(cliente.Id);
                    var filter = Builders<ClienteData>.Filter.Eq(x => x.ClienteId, cliente.Id);
                    
                    // BD Academico - SOLO datos académicos
                    var datosAcademicos = new BsonDocument {
                        { "datos_academicos", cliente.DatosAcademicos }
                    };
                    
                    var dbAcademico = _shardingService.GetSeccionDatabase("Academico");
                    var collectionAcademico = dbAcademico.GetCollection<ClienteData>($"Academico_{shard}");
                    var clienteAcademico = new ClienteData
                    {
                        ClienteId = cliente.Id,
                        InstitucionId = cliente.Inst,
                        Datos = datosAcademicos,
                        LastUpdated = DateTime.UtcNow,
                        Version = 1
                    };
                    await collectionAcademico.ReplaceOneAsync(filter, clienteAcademico, new ReplaceOptions { IsUpsert = true });
                    
                    // BD Comunidad - SOLO datos de comunidad
                    var datosComunidad = new BsonDocument {
                        { "datos_comunidad", new BsonDocument {
                            { "cargo", cliente.DatosComunidad["cargo"] },
                            { "eventos_participados", cliente.DatosComunidad["eventos_participados"] },
                            { "proyectos_activos", cliente.DatosComunidad["proyectos_activos"] },
                            { "personas", new BsonArray {
                                new BsonDocument { { "id", "1" }, { "nombre", "Juan" }, { "apellido", "Pérez" } },
                                new BsonDocument { { "id", "2" }, { "nombre", "María" }, { "apellido", "García" } }
                            }}
                        }}
                    };
                    
                    var dbComunidad = _shardingService.GetSeccionDatabase("Comunidad");
                    var collectionComunidad = dbComunidad.GetCollection<ClienteData>($"Comunidad_{shard}");
                    var clienteComunidad = new ClienteData
                    {
                        ClienteId = cliente.Id,
                        InstitucionId = cliente.Inst,
                        Datos = datosComunidad,
                        LastUpdated = DateTime.UtcNow,
                        Version = 1
                    };
                    await collectionComunidad.ReplaceOneAsync(filter, clienteComunidad, new ReplaceOptions { IsUpsert = true });
                }

                return Ok($"Datos específicos creados en BDs separadas para {clientes.Length} clientes");
            }
            catch (Exception ex)
            {
                return BadRequest($"Error: {ex.Message}");
            }
        }

        public async Task<IActionResult> VerDatos()
        {
            var shards = new[] { "1_10", "11_20" };
            var secciones = new[] { "Academico", "Comunidad" };
            var resultado = new Dictionary<string, object>();

            foreach (var seccion in secciones)
            {
                foreach (var shard in shards)
                {
                    var collectionName = $"{seccion}_{shard}";
                    var database = _shardingService.GetSeccionDatabase(seccion);
                    var collection = database.GetCollection<ClienteData>(collectionName);
                    var datos = await collection.Find(_ => true).ToListAsync();
                    resultado[collectionName] = datos;
                }
            }

            return Json(resultado);
        }

        public async Task<IActionResult> ProcesarEventos()
        {
            var eventoProcessado = await _rabbitConsumer.ProcesarEventosEnCola();
            return Ok($"Evento procesado: {eventoProcessado}");
        }
        
        public async Task<IActionResult> VerClientePorSeccion(string clienteId = "fd95295d-7832-497b-b46b-bfd98710e4e5", string seccion = "Academico")
        {
            try
            {
                var shard = _shardingService.DeterminarShard(clienteId);
                var filter = Builders<ClienteData>.Filter.Eq(x => x.ClienteId, clienteId);
                
                var db = _shardingService.GetSeccionDatabase(seccion);
                var collection = db.GetCollection<ClienteData>($"{seccion}_{shard}");
                var cliente = await collection.Find(filter).FirstOrDefaultAsync();
                
                if (cliente == null)
                {
                    return NotFound($"Cliente {clienteId} no encontrado en {seccion}");
                }
                
                return Json(new {
                    ClienteId = cliente.ClienteId,
                    InstitucionId = cliente.InstitucionId,
                    Seccion = seccion,
                    Datos = cliente.Datos,
                    LastUpdated = cliente.LastUpdated,
                    Version = cliente.Version,
                    Shard = shard
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Error: {ex.Message}");
            }
        }
        
        public async Task<IActionResult> SimularEventoActualizacion()
        {
            try
            {
                var eventoJson = @"{
                    ""ClienteId"": ""fd95295d-7832-497b-b46b-bfd98710e4e5"",
                    ""Seccion"": ""Comunidad"",
                    ""RegistroId"": ""1"",
                    ""Cambio"": {
                        ""Datos"": {
                            ""cargo"": ""Representante Estudiantil"",
                            ""eventos_participados"": 12,
                            ""proyectos_activos"": 3,
                            ""fecha_actualizacion"": ""2024-01-15""
                        }
                    },
                    ""Timestamp"": ""2025-01-15T20:48:22.1726702Z""
                }";
                
                var evento = JsonSerializer.Deserialize<EventoJack>(eventoJson);
                var institucionId = $"INST_{evento.RegistroId}";
                var shard = _shardingService.DeterminarShard(evento.ClienteId);
                
                var db = _shardingService.GetSeccionDatabase(evento.Seccion);
                var collection = db.GetCollection<ClienteData>($"{evento.Seccion}_{shard}");
                
                var filter = Builders<ClienteData>.Filter.Eq(x => x.ClienteId, evento.ClienteId);
                var registroExistente = await collection.Find(filter).FirstOrDefaultAsync();
                
                if (registroExistente == null)
                {
                    return BadRequest($"Cliente no existe en BD {evento.Seccion}. Crear primero con CrearDatos");
                }
                
                var datosSeccion = registroExistente.Datos ?? new BsonDocument();
                var seccionKey = evento.Seccion.ToLower() == "academico" ? "datos_academicos" : "datos_comunidad";
                
                if (evento.Cambio.TryGetProperty("Datos", out var nuevosDatos))
                {
                    var nuevosDatosBson = BsonDocument.Parse(nuevosDatos.GetRawText());
                    datosSeccion[seccionKey] = nuevosDatosBson;
                }
                
                var clienteActualizado = new ClienteData
                {
                    ClienteId = evento.ClienteId,
                    InstitucionId = institucionId,
                    Datos = datosSeccion,
                    LastUpdated = DateTime.UtcNow,
                    Version = registroExistente.Version + 1
                };
                
                await collection.ReplaceOneAsync(filter, clienteActualizado);
                
                return Ok(new {
                    Mensaje = $"Sección {seccionKey} actualizada exitosamente",
                    ClienteId = evento.ClienteId,
                    SeccionActualizada = seccionKey,
                    BD = db.DatabaseNamespace.DatabaseName,
                    Version = clienteActualizado.Version,
                    Shard = shard
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Error: {ex.Message}");
            }
        }
        
        public async Task<IActionResult> ActualizarSeccionEspecifica(string clienteId, string seccion, string nuevosDatos)
        {
            try
            {
                var shard = _shardingService.DeterminarShard(clienteId);
                var db = _shardingService.GetSeccionDatabase(seccion);
                var collection = db.GetCollection<ClienteData>($"{seccion}_{shard}");
                
                var filter = Builders<ClienteData>.Filter.Eq(x => x.ClienteId, clienteId);
                var registroExistente = await collection.Find(filter).FirstOrDefaultAsync();
                
                if (registroExistente == null)
                {
                    return NotFound($"Cliente {clienteId} no encontrado en BD {seccion}");
                }
                
                var datosSeccion = registroExistente.Datos ?? new BsonDocument();
                var seccionKey = seccion.ToLower() == "academico" ? "datos_academicos" : "datos_comunidad";
                var nuevosDatosBson = BsonDocument.Parse(nuevosDatos);
                datosSeccion[seccionKey] = nuevosDatosBson;
                
                var clienteActualizado = new ClienteData
                {
                    ClienteId = clienteId,
                    InstitucionId = registroExistente.InstitucionId,
                    Datos = datosSeccion,
                    LastUpdated = DateTime.UtcNow,
                    Version = registroExistente.Version + 1
                };
                
                await collection.ReplaceOneAsync(filter, clienteActualizado);
                
                return Ok(new {
                    Mensaje = $"Sección {seccionKey} actualizada",
                    ClienteId = clienteId,
                    BD = db.DatabaseNamespace.DatabaseName,
                    Version = clienteActualizado.Version
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Error: {ex.Message}");
            }
        }
    }
}