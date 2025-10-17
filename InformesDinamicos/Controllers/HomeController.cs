using Microsoft.AspNetCore.Mvc;
using InformesDinamicos.Data.Models;
using InformesDinamicos.Services;
using MongoDB.Driver;

namespace InformesDinamicos.Controllers
{
    public class HomeController : Controller
    {
        private readonly ShardingService _shardingService;
        private readonly ConsolidacionService _consolidacionService;

        public HomeController(ShardingService shardingService, ConsolidacionService consolidacionService)
        {
            _shardingService = shardingService;
            _consolidacionService = consolidacionService;
        }

        public async Task<IActionResult> Index(string clienteId = "", string seccion = "Comunidad")
        {
            ViewBag.ClienteId = clienteId;
            ViewBag.Seccion = seccion;
            
            if (string.IsNullOrEmpty(clienteId))
            {
                return View(new List<ClienteData>());
            }

            try
            {
                var shard = _shardingService.DeterminarShard(clienteId);
                var shardCompleto = $"{seccion}_{shard}";
                
                // Buscar en el shard calculado
                var db = _shardingService.GetSeccionDatabase(seccion);
                var collection = db.GetCollection<ClienteData>($"{seccion}_{shard}");
                var cliente = await collection.Find(x => x.ClienteId == clienteId).FirstOrDefaultAsync();
                
                // Si no se encuentra, buscar en todos los shards
                if (cliente == null)
                {
                    var shards = new[] { "1_10", "11_20" };
                    foreach (var s in shards)
                    {
                        var database = _shardingService.GetSeccionDatabase(seccion);
                        var col = database.GetCollection<ClienteData>($"{seccion}_{s}");
                        cliente = await col.Find(x => x.ClienteId == clienteId).FirstOrDefaultAsync();
                        if (cliente != null)
                        {
                            shardCompleto = $"{seccion}_{s}";
                            break;
                        }
                    }
                }
                
                ViewBag.ShardInfo = cliente != null ? 
                    $"Cliente encontrado en shard: {shardCompleto}" : 
                    $"Cliente no encontrado. Calculado para shard: {shardCompleto}";
                ViewBag.ShardCompleto = shardCompleto;
                
                var result = cliente != null ? new List<ClienteData> { cliente } : new List<ClienteData>();
                return View(result);
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return View(new List<ClienteData>());
            }
        }

        public async Task<IActionResult> Shard(string shardName)
        {
            try
            {
                if (string.IsNullOrEmpty(shardName))
                {
                    ViewBag.Error = "Nombre de shard requerido";
                    return View("Index", new List<ClienteData>());
                }

                // Extraer sección del shardName (ej: "Academico_1_10" -> "Academico")
                var seccion = shardName.Split('_')[0];
                var database = _shardingService.GetSeccionDatabase(seccion);
                var collection = database.GetCollection<ClienteData>(shardName);
                
                var clientes = await collection.Find(_ => true)
                    .SortByDescending(c => c.LastUpdated)
                    .Limit(20)
                    .ToListAsync();
                
                ViewBag.ShardName = shardName;
                return View("Index", clientes);
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error cargando shard {shardName}: {ex.Message}";
                return View("Index", new List<ClienteData>());
            }
        }

        public IActionResult Stats()
        {
            return View();
        }

        public IActionResult Nuevo()
        {
            return View();
        }

        public async Task<IActionResult> CrearDatos()
        {
            try
            {
                var registros = new[]
                {
                    new { ClienteId = "5", InstitucionId = "INST_001", Seccion = "Academico", 
                          Datos = (object)new { 
                              programas = new[] {
                                  new { id = "prog_001", nombre = "Ingeniería", nivel = 8, promedio = 4.2, creditos = 180, 
                                        asignaturas = new[] { 
                                            new { id = "asig_001", nombre = "Cálculo" },
                                            new { id = "asig_002", nombre = "Física" }
                                        } },
                                  new { id = "prog_002", nombre = "Sistemas", nivel = 2, promedio = 3.8, creditos = 45, 
                                        asignaturas = new[] { 
                                            new { id = "asig_003", nombre = "Algoritmos" },
                                            new { id = "asig_004", nombre = "Bases de Datos" }
                                        } }
                              },
                              asignaturas = new[] {
                                  new { id = "asig_crud_001", nombre = "Matemáticas Avanzadas", creditos = 4, semestre = 3 },
                                  new { id = "asig_crud_002", nombre = "Química General", creditos = 3, semestre = 2 },
                                  new { id = "asig_crud_003", nombre = "Inglés Técnico", creditos = 2, semestre = 1 }
                              }
                          } },
                    new { ClienteId = "5", InstitucionId = "INST_001", Seccion = "Comunidad", 
                          Datos = (object)new { 
                              personas = new[] {
                                  new { id = "pers_001", nombre = "Juan Pérez", rol = "Estudiante", edad = 22 },
                                  new { id = "pers_002", nombre = "María García", rol = "Monitor", edad = 24 }
                              }
                          } },
                    new { ClienteId = "15", InstitucionId = "INST_002", Seccion = "Academico", 
                          Datos = (object)new { 
                              programas = new[] {
                                  new { id = "prog_003", nombre = "Medicina", nivel = 6, promedio = 4.5, creditos = 220, 
                                        asignaturas = new[] { 
                                            new { id = "asig_005", nombre = "Anatomía" },
                                            new { id = "asig_006", nombre = "Fisiología" }
                                        } }
                              },
                              asignaturas = new[] {
                                  new { id = "asig_crud_004", nombre = "Bioquímica", creditos = 5, semestre = 4 },
                                  new { id = "asig_crud_005", nombre = "Farmacología", creditos = 4, semestre = 6 }
                              }
                          } },
                    new { ClienteId = "15", InstitucionId = "INST_002", Seccion = "Comunidad", 
                          Datos = (object)new { 
                              personas = new[] {
                                  new { id = "pers_003", nombre = "Carlos López", rol = "Representante", edad = 25 },
                                  new { id = "pers_004", nombre = "Ana Martínez", rol = "Coordinadora", edad = 28 }
                              }
                          } },
                    new { ClienteId = "3", InstitucionId = "INST_003", Seccion = "Academico", 
                          Datos = (object)new { 
                              programas = new[] {
                                  new { id = "prog_004", nombre = "Derecho", nivel = 4, promedio = 3.8, creditos = 120, 
                                        asignaturas = new[] { 
                                            new { id = "asig_007", nombre = "Constitucional" },
                                            new { id = "asig_008", nombre = "Civil" }
                                        } }
                              },
                              asignaturas = new[] {
                                  new { id = "asig_crud_006", nombre = "Historia del Derecho", creditos = 3, semestre = 2 },
                                  new { id = "asig_crud_007", nombre = "Ética Profesional", creditos = 2, semestre = 8 }
                              }
                          } },
                    new { ClienteId = "18", InstitucionId = "INST_004", Seccion = "Academico", 
                          Datos = (object)new { 
                              programas = new[] {
                                  new { id = "prog_005", nombre = "Psicología", nivel = 7, promedio = 4.1, creditos = 200, 
                                        asignaturas = new[] { 
                                            new { id = "asig_009", nombre = "Cognitiva" },
                                            new { id = "asig_010", nombre = "Social" }
                                        } }
                              },
                              asignaturas = new[] {
                                  new { id = "asig_crud_008", nombre = "Estadística Aplicada", creditos = 4, semestre = 5 },
                                  new { id = "asig_crud_009", nombre = "Neuropsicología", creditos = 3, semestre = 7 }
                              }
                          } }
                };

                foreach (var registro in registros)
                {
                    var shard = _shardingService.DeterminarShard(registro.ClienteId);
                    var db = _shardingService.GetSeccionDatabase(registro.Seccion);
                    var collection = db.GetCollection<ClienteData>($"{registro.Seccion}_{shard}");
                    
                    var datosCompletos = MongoDB.Bson.BsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(registro.Datos));
                    
                    var clienteData = new ClienteData
                    {
                        Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
                        ClienteId = registro.ClienteId,
                        InstitucionId = registro.InstitucionId,
                        Datos = datosCompletos,
                        LastUpdated = DateTime.UtcNow,
                        Version = 1
                    };

                    await collection.InsertOneAsync(clienteData);
                }

                return Json(new { success = true, mensaje = $"{registros.Length} registros creados", registros = registros.Length });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        public async Task<IActionResult> Consolidado(string clienteId)
        {
            if (string.IsNullOrEmpty(clienteId))
            {
                return RedirectToAction("Nuevo");
            }

            try
            {
                var datosConsolidados = await _consolidacionService.BuscarEnTodasLasSecciones(clienteId);
                ViewBag.ClienteId = clienteId;
                ViewBag.TotalRegistros = datosConsolidados.Count;
                return View(datosConsolidados);
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return View(new List<ClienteData>());
            }
        }
        
        public IActionResult General()
        {
            return RedirectToAction("Nuevo");
        }
        
        public IActionResult Academico()
        {
            return RedirectToAction("Nuevo");
        }
        
        public IActionResult Comunidad()
        {
            return RedirectToAction("Nuevo");
        }
        
        public IActionResult Todas()
        {
            return RedirectToAction("Nuevo");
        }
        
        public IActionResult Informes()
        {
            return View();
        }

        public IActionResult RabbitMQ()
        {
            return View();
        }
    }
}