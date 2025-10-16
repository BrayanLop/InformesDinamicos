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

        public async Task<IActionResult> CrearDatos()
        {
            try
            {
                var registros = new[]
                {
                    new { ClienteId = "5", InstitucionId = "INST_001", Seccion = "Academico", 
                          Datos = (object)new { 
                              programa = "Ingeniería", semestre = 8, promedio = 4.2, creditos = 180,
                              materias = new[] { "Cálculo", "Física", "Programación" },
                              notas = new { parcial1 = 4.1, parcial2 = 4.3, final = 4.2 }
                          } },
                    new { ClienteId = "5", InstitucionId = "INST_001", Seccion = "Comunidad", 
                          Datos = (object)new { 
                              cargo = "Estudiante", eventos = 5, proyectos = 2, voluntariado = "Cruz Roja",
                              actividades = new[] { "Deportes", "Teatro", "Debate" },
                              logros = new { reconocimientos = 2, certificados = 3 }
                          } },
                    new { ClienteId = "15", InstitucionId = "INST_002", Seccion = "Academico", 
                          Datos = (object)new { 
                              programa = "Medicina", semestre = 6, promedio = 4.5, creditos = 220,
                              materias = new[] { "Anatomía", "Fisiología", "Patología" },
                              notas = new { parcial1 = 4.4, parcial2 = 4.6, final = 4.5 }
                          } },
                    new { ClienteId = "15", InstitucionId = "INST_002", Seccion = "Comunidad", 
                          Datos = (object)new { 
                              cargo = "Monitor", eventos = 8, proyectos = 1, club = "Deportivo",
                              actividades = new[] { "Fútbol", "Natación", "Atletismo" },
                              logros = new { reconocimientos = 1, certificados = 2 }
                          } },
                    new { ClienteId = "3", InstitucionId = "INST_003", Seccion = "Academico", 
                          Datos = (object)new { 
                              programa = "Derecho", semestre = 4, promedio = 3.8, creditos = 120,
                              materias = new[] { "Constitucional", "Civil", "Penal" },
                              notas = new { parcial1 = 3.7, parcial2 = 3.9, final = 3.8 }
                          } },
                    new { ClienteId = "18", InstitucionId = "INST_004", Seccion = "Academico", 
                          Datos = (object)new { 
                              programa = "Psicología", semestre = 7, promedio = 4.1, creditos = 200,
                              materias = new[] { "Cognitiva", "Social", "Clínica" },
                              notas = new { parcial1 = 4.0, parcial2 = 4.2, final = 4.1 }
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
                ViewBag.Error = "Cliente ID requerido";
                return View(new List<ClienteData>());
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
    }
}