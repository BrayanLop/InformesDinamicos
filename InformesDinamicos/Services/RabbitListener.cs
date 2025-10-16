using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using InformesDinamicos.Data;
using InformesDinamicos.Data.Models;
using MongoDB.Driver;
using MongoDB.Bson;

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
            var institucionId = $"INST_{evento.RegistroId}";
            var shard = _shardingService.DeterminarShard(evento.ClienteId);
            var seccionKey = evento.Seccion.ToLower() == "academico" ? "datos_academicos" : "datos_comunidad";
            
            var db = _shardingService.GetSeccionDatabase(evento.Seccion);
            var collection = db.GetCollection<ClienteData>($"{evento.Seccion}_{shard}");
            
            var filter = Builders<ClienteData>.Filter.Eq(x => x.ClienteId, evento.ClienteId);
            var registroExistente = await collection.Find(filter).FirstOrDefaultAsync();
            
            MongoDB.Bson.BsonDocument datosSeccion;
            
            if (registroExistente != null)
            {
                datosSeccion = registroExistente.Datos ?? new MongoDB.Bson.BsonDocument();
            }
            else
            {
                datosSeccion = new MongoDB.Bson.BsonDocument();
            }
            
            if (evento.Cambio.TryGetProperty("Datos", out var nuevosDatos))
            {
                var nuevosDatosBson = MongoDB.Bson.BsonDocument.Parse(nuevosDatos.GetRawText());
                datosSeccion[seccionKey] = nuevosDatosBson;
            }
            
            var clienteData = new ClienteData
            {
                ClienteId = evento.ClienteId,
                InstitucionId = institucionId,
                Datos = datosSeccion,
                LastUpdated = DateTime.UtcNow,
                Version = (registroExistente?.Version ?? 0) + 1
            };
            
            await collection.ReplaceOneAsync(filter, clienteData, new ReplaceOptions { IsUpsert = true });
        }

        public override void Dispose()
        {
            _channel?.Close();
            _connection?.Close();
            base.Dispose();
        }
    }
}