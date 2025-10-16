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
                    new { ClienteId = "5", InstitucionId = "INST_001", Seccion = "Academico", 
                          Datos = (object)new { 
                              programas = new[] {
                                  new { id = "prog_101", nombre = "Ingeniería de Sistemas", nivel = 8, promedio = 4.2, creditos = 145, 
                                        asignaturas = new[] { 
                                            new { id = "asig_101", nombre = "Algoritmos" },
                                            new { id = "asig_102", nombre = "Estructuras" }
                                        } },
                                  new { id = "prog_102", nombre = "Matemáticas", nivel = 3, promedio = 3.9, creditos = 60, 
                                        asignaturas = new[] { 
                                            new { id = "asig_103", nombre = "Cálculo" },
                                            new { id = "asig_104", nombre = "Álgebra" }
                                        } }
                              },
                              asignaturas = new[] {
                                  new { id = "asig_crud_101", nombre = "Física Aplicada", creditos = 4, semestre = 2 },
                                  new { id = "asig_crud_102", nombre = "Metodología de Investigación", creditos = 3, semestre = 4 }
                              }
                          } },
                    new { ClienteId = "5", InstitucionId = "INST_001", Seccion = "Comunidad", 
                          Datos = (object)new { 
                              personas = new[] {
                                  new { id = "pers_101", nombre = "Pedro Rodríguez", rol = "Estudiante", edad = 21 },
                                  new { id = "pers_102", nombre = "Laura Jiménez", rol = "Voluntaria", edad = 23 }
                              }
                          } },
                    new { ClienteId = "8", InstitucionId = "INST_002", Seccion = "Academico", 
                          Datos = (object)new { 
                              programas = new[] {
                                  new { id = "prog_103", nombre = "Medicina", nivel = 6, promedio = 4.5, creditos = 120, 
                                        asignaturas = new[] { 
                                            new { id = "asig_105", nombre = "Anatomía" },
                                            new { id = "asig_106", nombre = "Fisiología" }
                                        } }
                              },
                              asignaturas = new[] {
                                  new { id = "asig_crud_103", nombre = "Patología", creditos = 5, semestre = 5 },
                                  new { id = "asig_crud_104", nombre = "Microbiología", creditos = 4, semestre = 3 }
                              }
                          } },
                    new { ClienteId = "8", InstitucionId = "INST_002", Seccion = "Comunidad", 
                          Datos = (object)new { 
                              personas = new[] {
                                  new { id = "pers_103", nombre = "Diego Morales", rol = "Estudiante", edad = 20 }
                              }
                          } },
                    new { ClienteId = "15", InstitucionId = "INST_003", Seccion = "Academico", 
                          Datos = (object)new { 
                              programas = new[] {
                                  new { id = "prog_104", nombre = "Derecho", nivel = 4, promedio = 3.8, creditos = 80, 
                                        asignaturas = new[] { 
                                            new { id = "asig_107", nombre = "Constitucional" },
                                            new { id = "asig_108", nombre = "Civil" }
                                        } }
                              },
                              asignaturas = new[] {
                                  new { id = "asig_crud_105", nombre = "Derecho Penal", creditos = 4, semestre = 6 },
                                  new { id = "asig_crud_106", nombre = "Derecho Laboral", creditos = 3, semestre = 7 }
                              }
                          } },
                    new { ClienteId = "15", InstitucionId = "INST_003", Seccion = "Comunidad", 
                          Datos = (object)new { 
                              personas = new[] {
                                  new { id = "pers_104", nombre = "Sofía Vargas", rol = "Representante", edad = 26 }
                              }
                          } },
                    new { ClienteId = "18", InstitucionId = "INST_004", Seccion = "Academico", 
                          Datos = (object)new { 
                              programas = new[] {
                                  new { id = "prog_105", nombre = "Psicología", nivel = 7, promedio = 4.1, creditos = 130, 
                                        asignaturas = new[] { 
                                            new { id = "asig_109", nombre = "Cognitiva" },
                                            new { id = "asig_110", nombre = "Social" }
                                        } }
                              },
                              asignaturas = new[] {
                                  new { id = "asig_crud_107", nombre = "Psicometría", creditos = 3, semestre = 6 },
                                  new { id = "asig_crud_108", nombre = "Terapia Cognitiva", creditos = 4, semestre = 8 }
                              }
                          } },
                    new { ClienteId = "18", InstitucionId = "INST_004", Seccion = "Comunidad", 
                          Datos = (object)new { 
                              personas = new[] {
                                  new { id = "pers_105", nombre = "Andrés Castro", rol = "Monitor", edad = 27 }
                              }
                          } }
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