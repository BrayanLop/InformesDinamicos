using Microsoft.AspNetCore.Mvc;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using InformesDinamicos.Data.Models;
using InformesDinamicos.Services;

namespace InformesDinamicos.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RabbitConsumerController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly RabbitListener _rabbitListener;
        private readonly ILogger<RabbitConsumerController> _logger;

        public RabbitConsumerController(IConfiguration configuration, RabbitListener rabbitListener, ILogger<RabbitConsumerController> logger)
        {
            _configuration = configuration;
            _rabbitListener = rabbitListener;
            _logger = logger;
        }

        [HttpPost("consumir-mensaje")]
        public async Task<IActionResult> ConsumirMensaje()
        {
            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = _configuration["RabbitMQ:HostName"],
                    UserName = _configuration["RabbitMQ:UserName"],
                    Password = _configuration["RabbitMQ:Password"]
                };

                using var connection = factory.CreateConnection();
                using var channel = connection.CreateModel();
                
                var queueName = _configuration["RabbitMQ:QueueName"];
                channel.QueueDeclare(queueName, true, false, false, null);

                // Obtener un mensaje de la cola
                var result = channel.BasicGet(queueName, false);
                
                if (result == null)
                {
                    return Ok(new { mensaje = "No hay mensajes en la cola", hayMensajes = false });
                }

                var body = result.Body.ToArray();
                var messageContent = Encoding.UTF8.GetString(body);
                
                _logger.LogInformation($"Mensaje raw consumido: {messageContent}");

                // Validar que sea JSON válido
                if (string.IsNullOrWhiteSpace(messageContent) || !messageContent.TrimStart().StartsWith("{"))
                {
                    return BadRequest(new { 
                        error = "El mensaje no es JSON válido",
                        contenido = messageContent,
                        tipo = "formato_invalido"
                    });
                }

                try
                {
                    // Procesar el evento
                    var eventoWrapper = JsonSerializer.Deserialize<EventoJack>(messageContent);
                    
                    if (eventoWrapper == null)
                    {
                        return BadRequest(new { 
                            error = "No se pudo deserializar el evento",
                            contenido = messageContent
                        });
                    }
                    
                    // Extraer el evento real del campo Cambio
                    var cambioJson = eventoWrapper.Cambio.GetRawText();
                    var evento = JsonSerializer.Deserialize<EventoJack>(cambioJson);
                    
                    if (evento == null)
                    {
                        return BadRequest(new { 
                            error = "No se pudo deserializar el campo Cambio",
                            contenido = cambioJson
                        });
                    }
                    
                    _logger.LogInformation($"Evento deserializado - Tipo: {evento.Tipo}, Entidad: {evento.Entidad}, Id: {evento.Id}");
                    _logger.LogInformation($"Datos del evento: {evento.Datos.GetRawText()}");
                    
                    if (_rabbitListener == null)
                    {
                        _logger.LogError("RabbitListener es null!");
                        return BadRequest(new { error = "RabbitListener no está disponible" });
                    }
                    
                    await _rabbitListener.ProcesarEventoManual(evento);
                    
                    _logger.LogInformation("Evento procesado exitosamente en MongoDB");

                    // NO hacer ACK - el mensaje permanece en la cola
                    // channel.BasicAck(result.DeliveryTag, false);

                    return Ok(new { 
                        mensaje = "Mensaje procesado exitosamente", 
                        evento = messageContent.Length > 200 ? messageContent.Substring(0, 200) + "..." : messageContent,
                        hayMensajes = true,
                        deliveryTag = result.DeliveryTag
                    });
                }
                catch (JsonException jsonEx)
                {
                    _logger.LogError(jsonEx, "Error deserializando JSON: {Message}", messageContent);
                    return BadRequest(new { 
                        error = "Error deserializando JSON",
                        detalles = jsonEx.Message,
                        contenido = messageContent.Length > 500 ? messageContent.Substring(0, 500) + "..." : messageContent
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error consumiendo mensaje");
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("contar-mensajes")]
        public IActionResult ContarMensajes()
        {
            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = _configuration["RabbitMQ:HostName"],
                    UserName = _configuration["RabbitMQ:UserName"],
                    Password = _configuration["RabbitMQ:Password"]
                };

                using var connection = factory.CreateConnection();
                using var channel = connection.CreateModel();
                
                var queueName = _configuration["RabbitMQ:QueueName"];
                var queueInfo = channel.QueueDeclarePassive(queueName);

                return Ok(new { 
                    cola = queueName,
                    mensajes = queueInfo.MessageCount,
                    consumidores = queueInfo.ConsumerCount
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("ver-mensaje")]
        public IActionResult VerMensaje()
        {
            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = _configuration["RabbitMQ:HostName"],
                    UserName = _configuration["RabbitMQ:UserName"],
                    Password = _configuration["RabbitMQ:Password"]
                };

                using var connection = factory.CreateConnection();
                using var channel = connection.CreateModel();
                
                var queueName = _configuration["RabbitMQ:QueueName"];
                channel.QueueDeclare(queueName, true, false, false, null);

                // Obtener un mensaje SIN consumirlo (peek)
                var result = channel.BasicGet(queueName, false);
                
                if (result == null)
                {
                    return Ok(new { mensaje = "No hay mensajes en la cola", hayMensajes = false });
                }

                var body = result.Body.ToArray();
                var messageContent = Encoding.UTF8.GetString(body);
                
                // NACK inmediatamente para devolver el mensaje a la cola
                channel.BasicNack(result.DeliveryTag, false, true);

                return Ok(new { 
                    mensaje = "Mensaje encontrado (no procesado)",
                    contenido = messageContent,
                    longitud = messageContent.Length,
                    esJson = messageContent.TrimStart().StartsWith("{"),
                    deliveryTag = result.DeliveryTag
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("purgar-cola")]
        public IActionResult PurgarCola()
        {
            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = _configuration["RabbitMQ:HostName"],
                    UserName = _configuration["RabbitMQ:UserName"],
                    Password = _configuration["RabbitMQ:Password"]
                };

                using var connection = factory.CreateConnection();
                using var channel = connection.CreateModel();
                
                var queueName = _configuration["RabbitMQ:QueueName"];
                var purged = channel.QueuePurge(queueName);

                return Ok(new { 
                    mensaje = $"Cola purgada: {purged} mensajes eliminados",
                    mensajesEliminados = purged
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}