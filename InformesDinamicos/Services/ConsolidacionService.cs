using InformesDinamicos.Data.Models;
using MongoDB.Driver;
using System.Text.Json;
using MongoDB.Bson;

namespace InformesDinamicos.Services
{
    public class ConsolidacionService
    {
        private readonly ShardingService _shardingService;
        private readonly ILogger<ConsolidacionService> _logger;

        public ConsolidacionService(ShardingService shardingService, ILogger<ConsolidacionService> logger)
        {
            _shardingService = shardingService;
            _logger = logger;
        }

        public async Task<InstitucionInfo> ConsolidarDatosInstitucion(string clienteId, string institucionId)
        {
            var secciones = new[] { "Academico", "Financiero", "Comunidad" };
            var institucionInfo = new InstitucionInfo
            {
                Id = institucionId,
                DatosAcademicos = new Dictionary<string, object>(),
                DatosFinancieros = new Dictionary<string, object>(),
                DatosComunidad = new Dictionary<string, object>(),
                MetadatosAdicionales = new Dictionary<string, object>()
            };

            foreach (var seccion in secciones)
            {
                try
                {
                    var shard = _shardingService.DeterminarShard(clienteId);
                    var db = _shardingService.GetSeccionDatabase(seccion);
                    var collection = db.GetCollection<ClienteData>($"{seccion}_{shard}");
                    var filtro = Builders<ClienteData>.Filter.And(
                        Builders<ClienteData>.Filter.Eq(x => x.ClienteId, clienteId),
                        Builders<ClienteData>.Filter.Eq(x => x.InstitucionId, institucionId)
                    );

                    var datos = await collection.Find(filtro).FirstOrDefaultAsync();
                    if (datos?.Datos != null)
                    {
                        var datosDict = JsonSerializer.Deserialize<Dictionary<string, object>>(datos.Datos.ToJson());
                        
                        switch (seccion.ToLower())
                        {
                            case "academico":
                                institucionInfo.DatosAcademicos = datosDict;
                                break;
                            case "financiero":
                                institucionInfo.DatosFinancieros = datosDict;
                                break;
                            case "comunidad":
                                institucionInfo.DatosComunidad = datosDict;
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error consolidando datos de {seccion} para cliente {clienteId}");
                }
            }

            return institucionInfo;
        }

        public async Task<List<ClienteData>> BuscarEnTodasLasSecciones(string clienteId)
        {
            var secciones = new[] { "Academico", "Financiero", "Comunidad" };
            var resultados = new List<ClienteData>();

            foreach (var seccion in secciones)
            {
                try
                {
                    var shards = new[] { "1_10", "11_20" };
                    foreach (var shard in shards)
                    {
                        var db = _shardingService.GetSeccionDatabase(seccion);
                        var collection = db.GetCollection<ClienteData>($"{seccion}_{shard}");
                        var datos = await collection.Find(x => x.ClienteId == clienteId).ToListAsync();
                        resultados.AddRange(datos);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error buscando en sección {seccion} para cliente {clienteId}");
                }
            }

            return resultados;
        }
    }
}