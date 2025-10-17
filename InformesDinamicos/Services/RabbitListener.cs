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
            _channel.QueueDeclare(_configuration["RabbitMQ:QueueName"], true, false, false, null);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var messageContent = Encoding.UTF8.GetString(body);
                _logger.LogInformation($"Leyendo evento: {messageContent}");
                
                try
                {
                    var evento = JsonSerializer.Deserialize<EventoJack>(messageContent);
                    await ProcesarEvento(evento);
                    _logger.LogInformation("Evento procesado (mensaje permanece en cola)");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error procesando evento");
                }
            };

            // autoAck = false para que los mensajes permanezcan en la cola
            _channel.BasicConsume(_configuration["RabbitMQ:QueueName"], false, consumer);
            return Task.CompletedTask;
        }

        private async Task ProcesarEvento(EventoJack evento)
        {
            try
            {
                _logger.LogInformation($"Procesando evento: {evento.Tipo} - {evento.Entidad}");
                
                switch (evento.Entidad?.ToLower())
                {
                    case "institucion":
                        await ProcesarInstitucion(evento);
                        break;
                    case "programa":
                        await ProcesarPrograma(evento);
                        break;
                    case "asignatura":
                        await ProcesarAsignatura(evento);
                        break;
                    case "persona":
                        await ProcesarPersona(evento);
                        break;
                    default:
                        _logger.LogWarning($"Entidad no reconocida: {evento.Entidad}");
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error procesando evento {evento.Tipo} para {evento.Entidad}");
            }
        }
        
        private async Task ProcesarInstitucion(EventoJack evento)
        {
            _logger.LogInformation($"Procesando institución - Tipo: {evento.Tipo}, Id: {evento.Id}");
            var db = _shardingService.GetDatabase("General");
            var collection = db.GetCollection<Institucion>("instituciones");
            
            switch (evento.Tipo?.ToLower())
            {
                case "crear":
                case "actualizar":
                    var datosJson = evento.Datos.GetRawText();
                    var datos = JsonSerializer.Deserialize<Dictionary<string, object>>(datosJson);
                    
                    _logger.LogInformation($"Insertando/Actualizando institución: {datos["institucionId"]} - {datos["nombre"]}");
                    
                    var update = Builders<Institucion>.Update
                        .Set(x => x.InstitucionId, datos["institucionId"].ToString())
                        .Set(x => x.Nombre, datos["nombre"].ToString())
                        .Set(x => x.Codigo, datos["codigo"].ToString())
                        .Set(x => x.LastUpdated, DateTime.UtcNow);
                    
                    var result = await collection.UpdateOneAsync(
                        Builders<Institucion>.Filter.Eq(x => x.InstitucionId, datos["institucionId"].ToString()),
                        update,
                        new UpdateOptions { IsUpsert = true }
                    );
                    _logger.LogInformation($"Resultado MongoDB - Matched: {result.MatchedCount}, Modified: {result.ModifiedCount}, Upserted: {result.UpsertedId}");
                    break;
                case "eliminar":
                    _logger.LogInformation($"Eliminando institución: {evento.Id}");
                    var deleteResult = await collection.DeleteOneAsync(
                        Builders<Institucion>.Filter.Eq(x => x.InstitucionId, evento.Id)
                    );
                    _logger.LogInformation($"Eliminación - Deleted: {deleteResult.DeletedCount}");
                    break;
            }
        }
        
        private async Task ProcesarPrograma(EventoJack evento)
        {
            var db = _shardingService.GetDatabase("Academico");
            var collection = db.GetCollection<Programa>("programas");
            
            switch (evento.Tipo?.ToLower())
            {
                case "crear":
                case "actualizar":
                    var programa = JsonSerializer.Deserialize<Programa>(evento.Datos.GetRawText());
                    await collection.ReplaceOneAsync(
                        Builders<Programa>.Filter.Eq(x => x.ProgramaId, programa.ProgramaId),
                        programa,
                        new ReplaceOptions { IsUpsert = true }
                    );
                    break;
                case "eliminar":
                    await collection.DeleteOneAsync(
                        Builders<Programa>.Filter.Eq(x => x.ProgramaId, evento.Id)
                    );
                    break;
            }
        }
        
        private async Task ProcesarAsignatura(EventoJack evento)
        {
            var db = _shardingService.GetDatabase("Academico");
            var collection = db.GetCollection<Asignatura>("asignaturas");
            
            switch (evento.Tipo?.ToLower())
            {
                case "crear":
                case "actualizar":
                    var asignatura = JsonSerializer.Deserialize<Asignatura>(evento.Datos.GetRawText());
                    await collection.ReplaceOneAsync(
                        Builders<Asignatura>.Filter.Eq(x => x.AsignaturaId, asignatura.AsignaturaId),
                        asignatura,
                        new ReplaceOptions { IsUpsert = true }
                    );
                    break;
                case "eliminar":
                    await collection.DeleteOneAsync(
                        Builders<Asignatura>.Filter.Eq(x => x.AsignaturaId, evento.Id)
                    );
                    break;
            }
        }
        
        private async Task ProcesarPersona(EventoJack evento)
        {
            var db = _shardingService.GetDatabase("Comunidad");
            var collection = db.GetCollection<Persona>("personas");
            
            switch (evento.Tipo?.ToLower())
            {
                case "crear":
                case "actualizar":
                    var persona = JsonSerializer.Deserialize<Persona>(evento.Datos.GetRawText());
                    await collection.ReplaceOneAsync(
                        Builders<Persona>.Filter.Eq(x => x.PersonaId, persona.PersonaId),
                        persona,
                        new ReplaceOptions { IsUpsert = true }
                    );
                    break;
                case "eliminar":
                    await collection.DeleteOneAsync(
                        Builders<Persona>.Filter.Eq(x => x.PersonaId, evento.Id)
                    );
                    break;
            }
        }

        public async Task ProcesarEventoManual(EventoJack evento)
        {
            await ProcesarEvento(evento);
        }
        
        public override void Dispose()
        {
            _channel?.Close();
            _connection?.Close();
            base.Dispose();
        }
    }
}