using Microsoft.AspNetCore.Mvc;
using InformesDinamicos.Services;

namespace InformesDinamicos.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InformesController : ControllerBase
    {
        private readonly DatabaseService _databaseService;

        public InformesController(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        [HttpGet("resumen")]
        public async Task<IActionResult> GetResumen(string? institucionId = null)
        {
            try
            {
                var instituciones = await _databaseService.GetInstitucionesAsync();
                var programas = await _databaseService.GetProgramasAsync(institucionId);
                var asignaturas = await _databaseService.GetAsignaturasAsync();
                var personas = await _databaseService.GetPersonasAsync();

                if (!string.IsNullOrEmpty(institucionId))
                {
                    asignaturas = asignaturas.Where(a => a.InstitucionId == institucionId).ToList();
                    personas = personas.Where(p => p.InstitucionId == institucionId).ToList();
                }

                return Ok(new
                {
                    instituciones = instituciones.Count(),
                    programas = programas.Count(),
                    asignaturas = asignaturas.Count(),
                    personas = personas.Count()
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("programas")]
        public async Task<IActionResult> GetProgramas(string? institucionId = null)
        {
            try
            {
                var programas = await _databaseService.GetProgramasAsync(institucionId);
                return Ok(programas);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("personas")]
        public async Task<IActionResult> GetPersonas(string? institucionId = null)
        {
            try
            {
                var personas = await _databaseService.GetPersonasAsync();
                
                if (!string.IsNullOrEmpty(institucionId))
                {
                    personas = personas.Where(p => p.InstitucionId == institucionId).ToList();
                }

                return Ok(personas);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("asignaturas")]
        public async Task<IActionResult> GetAsignaturas(string? institucionId = null)
        {
            try
            {
                var asignaturas = await _databaseService.GetAsignaturasAsync();
                
                if (!string.IsNullOrEmpty(institucionId))
                {
                    asignaturas = asignaturas.Where(a => a.InstitucionId == institucionId).ToList();
                }

                return Ok(asignaturas);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}