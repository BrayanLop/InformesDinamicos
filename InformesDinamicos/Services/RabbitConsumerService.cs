using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using InformesDinamicos.Data.Models;
using MongoDB.Driver;
using MongoDB.Bson;

namespace InformesDinamicos.Services
{
    public class RabbitConsumerService
    {
        private readonly IConfiguration _configuration;
        private readonly ShardingService _shardingService;
        private readonly ILogger<RabbitConsumerService> _logger;

        public RabbitConsumerService(IConfiguration configuration, ShardingService shardingService, ILogger<RabbitConsumerService> logger)
        {
            _configuration = configuration;
            _shardingService = shardingService;
            _logger = logger;
        }

        public async Task<bool> ProcesarEventosEnCola()
        {
            var factory = new ConnectionFactory
            {
                HostName = "localhost",
                Port = 5672,
                UserName = "guest",
                Password = "guest"
            };

            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            channel.QueueDeclare("informe", true, false, false, null);
            var result = channel.BasicGet("informe", false);

            if (result == null)
            {
                return false;
            }

            var messageContent = Encoding.UTF8.GetString(result.Body.ToArray());
            _logger.LogInformation($"Procesando evento: {messageContent}");

            try
            {
                var evento = JsonSerializer.Deserialize<EventoJack>(messageContent);
                await GuardarEvento(evento);
                channel.BasicAck(result.DeliveryTag, false);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando evento");
                channel.BasicNack(result.DeliveryTag, false, true);
                throw;
            }
        }

        private async Task GuardarEvento(EventoJack evento)
        {
            var institucionId = $"INST_{evento.RegistroId}";
            var shard = _shardingService.DeterminarShard(evento.ClienteId);
            var db = _shardingService.GetSeccionDatabase(evento.Seccion);
            var collection = db.GetCollection<ClienteData>($"{evento.Seccion}_{shard}");
            
            var filter = Builders<ClienteData>.Filter.Eq(x => x.ClienteId, evento.ClienteId);
            var registroExistente = await collection.Find(filter).FirstOrDefaultAsync();
            
            BsonDocument datosCompletos;
            
            if (registroExistente != null)
            {
                datosCompletos = registroExistente.Datos ?? new BsonDocument();
            }
            else
            {
                datosCompletos = new BsonDocument();
            }
            
            if (evento.Cambio.TryGetProperty("Datos", out var nuevosDatos))
            {
                var nuevosDatosBson = BsonDocument.Parse(nuevosDatos.GetRawText());
                foreach (var elemento in nuevosDatosBson)
                {
                    datosCompletos[elemento.Name] = elemento.Value;
                }
            }
            
            var clienteData = new ClienteData
            {
                ClienteId = evento.ClienteId,
                InstitucionId = institucionId,
                Datos = datosCompletos,
                LastUpdated = DateTime.UtcNow,
                Version = (registroExistente?.Version ?? 0) + 1
            };
            
            await collection.ReplaceOneAsync(filter, clienteData, new ReplaceOptions { IsUpsert = true });
            
            _logger.LogInformation($"Evento guardado: Cliente {evento.ClienteId} en BD {evento.Seccion}");
        }
    }
}