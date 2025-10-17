using Microsoft.AspNetCore.Mvc;
using InformesDinamicos.Data.Models;
using InformesDinamicos.Services;
using System.Text.Json;

namespace InformesDinamicos.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestBusController : ControllerBase
    {
        private readonly RabbitListener _rabbitListener;
        private readonly ILogger<TestBusController> _logger;

        public TestBusController(RabbitListener rabbitListener, ILogger<TestBusController> logger)
        {
            _rabbitListener = rabbitListener;
            _logger = logger;
        }

        [HttpPost("simular-evento")]
        public async Task<IActionResult> SimularEvento([FromBody] EventoJack evento)
        {
            try
            {
                _logger.LogInformation($"Simulando evento: {evento.Entidad} - {evento.Tipo}");
                
                // Llamar directamente al método de procesamiento
                await _rabbitListener.ProcesarEventoManual(evento);
                
                return Ok(new { mensaje = "Evento procesado exitosamente", evento });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando evento manual");
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("crear-institucion")]
        public async Task<IActionResult> CrearInstitucion()
        {
            var evento = new EventoJack
            {
                Tipo = "crear",
                Entidad = "institucion",
                Id = "inst_test_001",
                Datos = JsonDocument.Parse(JsonSerializer.Serialize(new
                {
                    institucionId = "inst_test_001",
                    nombre = "Universidad de Prueba",
                    codigo = "UP"
                })).RootElement,
                Timestamp = DateTime.UtcNow
            };

            return await SimularEvento(evento);
        }

        [HttpPost("crear-programa")]
        public async Task<IActionResult> CrearPrograma()
        {
            var evento = new EventoJack
            {
                Tipo = "crear",
                Entidad = "programa",
                Id = "prog_test_001",
                Datos = JsonDocument.Parse(JsonSerializer.Serialize(new
                {
                    programaId = "prog_test_001",
                    institucionId = "inst_test_001",
                    nombre = "Ingeniería de Prueba",
                    nivel = 5,
                    promedio = 4.0,
                    creditos = 160
                })).RootElement,
                Timestamp = DateTime.UtcNow
            };

            return await SimularEvento(evento);
        }

        [HttpPost("crear-asignatura")]
        public async Task<IActionResult> CrearAsignatura()
        {
            var evento = new EventoJack
            {
                Tipo = "crear",
                Entidad = "asignatura",
                Id = "asig_test_001",
                Datos = JsonDocument.Parse(JsonSerializer.Serialize(new
                {
                    asignaturaId = "asig_test_001",
                    programaId = "prog_test_001",
                    institucionId = "inst_test_001",
                    nombre = "Matemáticas de Prueba",
                    creditos = 4,
                    semestre = 1
                })).RootElement,
                Timestamp = DateTime.UtcNow
            };

            return await SimularEvento(evento);
        }

        [HttpPost("crear-persona")]
        public async Task<IActionResult> CrearPersona()
        {
            var evento = new EventoJack
            {
                Tipo = "crear",
                Entidad = "persona",
                Id = "pers_test_001",
                Datos = JsonDocument.Parse(JsonSerializer.Serialize(new
                {
                    personaId = "pers_test_001",
                    institucionId = "inst_test_001",
                    nombre = "Juan Prueba",
                    rol = "Estudiante",
                    edad = 20
                })).RootElement,
                Timestamp = DateTime.UtcNow
            };

            return await SimularEvento(evento);
        }

        [HttpPost("crear-datos-completos")]
        public async Task<IActionResult> CrearDatosCompletos()
        {
            try
            {
                await CrearInstitucion();
                await CrearPrograma();
                await CrearAsignatura();
                await CrearPersona();

                return Ok(new { mensaje = "Datos completos creados exitosamente" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}