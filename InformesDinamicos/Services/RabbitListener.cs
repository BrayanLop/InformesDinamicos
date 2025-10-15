using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using InformesDinamicos.Data;
using InformesDinamicos.Data.Models;
using MongoDB.Driver;

namespace InformesDinamicos.Services
{
    public class RabbitListener : BackgroundService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<RabbitListener> _logger;
        private readonly ShardingService _shardingService;
        private IConnection _connection;
        private IModel _channel;

        public RabbitListener(IConfiguration configuration, ILogger<RabbitListener> logger, ShardingService shardingService)
        {
            _configuration = configuration;
            _logger = logger;
            _shardingService = shardingService;
            InitRabbitMQ();
        }

        private void InitRabbitMQ()
        {
            var factory = new ConnectionFactory
            {
                HostName = _configuration["RabbitMQ:HostName"],
                UserName = _configuration["RabbitMQ:UserName"],
                Password = _configuration["RabbitMQ:Password"]
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            _channel.QueueDeclare(_configuration["RabbitMQ:QueueName"], false, false, false, null);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var messageContent = Encoding.UTF8.GetString(body);
                _logger.LogInformation($"Evento recibido: {messageContent}");
                
                try
                {
                    var evento = JsonSerializer.Deserialize<EventoJack>(messageContent);
                    await ProcesarEvento(evento);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error procesando evento");
                }
            };

            _channel.BasicConsume(_configuration["RabbitMQ:QueueName"], true, consumer);
            return Task.CompletedTask;
        }

        private async Task ProcesarEvento(EventoJack evento)
        {
            var collection = _shardingService.GetClienteCollection(evento.ClienteId, evento.Seccion);
            
            var filter = Builders<ClienteData>.Filter.Eq(x => x.ClienteId, evento.ClienteId);
            var update = Builders<ClienteData>.Update
                .Set(x => x.LastUpdated, DateTime.UtcNow)
                .Set(x => x.Datos, evento.Cambio);
            
            await collection.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true });
        }

        public override void Dispose()
        {
            _channel?.Close();
            _connection?.Close();
            base.Dispose();
        }
    }
}