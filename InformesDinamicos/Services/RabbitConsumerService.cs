using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using InformesDinamicos.Data.Models;

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

        public async Task<int> ProcesarEventosEnCola()
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
            
            channel.QueueDeclare("informe-dinamico", true, false, false, null);
            
            var eventosProcessados = 0;
            var continuar = true;

            while (continuar)
            {
                var result = channel.BasicGet("informe-dinamico", false);
                
                if (result == null)
                {
                    continuar = false;
                }
                else
                {
                    var messageContent = Encoding.UTF8.GetString(result.Body.ToArray());
                    _logger.LogInformation($"Procesando evento: {messageContent}");
                    
                    try
                    {
                        var evento = JsonSerializer.Deserialize<EventoJack>(messageContent);
                        await GuardarEvento(evento);
                        channel.BasicAck(result.DeliveryTag, false);
                        eventosProcessados++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error procesando evento");
                        channel.BasicNack(result.DeliveryTag, false, true);
                    }
                }
            }

            return eventosProcessados;
        }

        private async Task GuardarEvento(EventoJack evento)
        {
            var collection = _shardingService.GetClienteCollection(evento.ClienteId, evento.Seccion);
            
            var clienteData = new ClienteData
            {
                ClienteId = evento.ClienteId,
                LastUpdated = DateTime.UtcNow,
                Datos = JsonSerializer.Deserialize<DatosCliente>(evento.Cambio.ToString())
            };

            var filter = MongoDB.Driver.Builders<ClienteData>.Filter.Eq(x => x.ClienteId, evento.ClienteId);
            await collection.ReplaceOneAsync(filter, clienteData, new MongoDB.Driver.ReplaceOptions { IsUpsert = true });
        }
    }
}