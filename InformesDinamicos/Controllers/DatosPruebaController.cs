using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using InformesDinamicos.Data.Models;
using InformesDinamicos.Services;

namespace InformesDinamicos.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DatosPruebaController : ControllerBase
    {
        private readonly DatabaseService _dbService;

        public DatosPruebaController(DatabaseService dbService)
        {
            _dbService = dbService;
        }

        [HttpPost("crear-todo")]
        public async Task<IActionResult> CrearTodo()
        {
            try
            {
                var instituciones = await CrearInstituciones();
                var programas = await CrearProgramas(instituciones);
                var asignaturas = await CrearAsignaturas(programas);
                var personas = await CrearPersonas(instituciones);

                return Ok(new
                {
                    mensaje = "Datos de prueba creados exitosamente",
                    instituciones = instituciones.Count,
                    programas = programas.Count,
                    asignaturas = asignaturas.Count,
                    personas = personas.Count
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpDelete("limpiar-todo")]
        public async Task<IActionResult> LimpiarTodo()
        {
            try
            {
                await _dbService.GetInstitucionesCollection().DeleteManyAsync(_ => true);
                await _dbService.GetProgramasCollection().DeleteManyAsync(_ => true);
                await _dbService.GetAsignaturasCollection().DeleteManyAsync(_ => true);
                await _dbService.GetPersonasCollection().DeleteManyAsync(_ => true);

                return Ok(new { mensaje = "Todos los datos han sido eliminados" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        private async Task<List<Institucion>> CrearInstituciones()
        {
            var instituciones = new List<Institucion>
            {
                new() { InstitucionId = "inst_001", Nombre = "Universidad Nacional", Codigo = "UN" },
                new() { InstitucionId = "inst_002", Nombre = "Universidad de los Andes", Codigo = "UNIANDES" },
                new() { InstitucionId = "inst_003", Nombre = "Pontificia Universidad Javeriana", Codigo = "PUJ" },
                new() { InstitucionId = "inst_004", Nombre = "Universidad del Rosario", Codigo = "UR" },
                new() { InstitucionId = "inst_005", Nombre = "Universidad EAFIT", Codigo = "EAFIT" }
            };

            await _dbService.GetInstitucionesCollection().InsertManyAsync(instituciones);
            return instituciones;
        }

        private async Task<List<Programa>> CrearProgramas(List<Institucion> instituciones)
        {
            var programas = new List<Programa>();
            var nombres = new[] { "Ingeniería de Sistemas", "Medicina", "Derecho", "Administración", "Psicología" };

            foreach (var inst in instituciones)
            {
                foreach (var nombre in nombres)
                {
                    programas.Add(new Programa
                    {
                        ProgramaId = $"prog_{inst.InstitucionId}_{programas.Count + 1:D3}",
                        InstitucionId = inst.InstitucionId,
                        Nombre = nombre,
                        Nivel = Random.Shared.Next(1, 6),
                        Promedio = Math.Round(Random.Shared.NextDouble() * 2 + 3, 1),
                        Creditos = Random.Shared.Next(120, 200)
                    });
                }
            }

            await _dbService.GetProgramasCollection().InsertManyAsync(programas);
            return programas;
        }

        private async Task<List<Asignatura>> CrearAsignaturas(List<Programa> programas)
        {
            var asignaturas = new List<Asignatura>();
            var materias = new[] { "Cálculo I", "Física I", "Química", "Estadística", "Programación", "Base de Datos", "Redes", "Algoritmos" };

            foreach (var programa in programas)
            {
                foreach (var materia in materias.Take(Random.Shared.Next(3, 6)))
                {
                    asignaturas.Add(new Asignatura
                    {
                        AsignaturaId = $"asig_{programa.ProgramaId}_{asignaturas.Count + 1:D3}",
                        ProgramaId = programa.ProgramaId,
                        InstitucionId = programa.InstitucionId,
                        Nombre = materia,
                        Creditos = Random.Shared.Next(2, 5),
                        Semestre = Random.Shared.Next(1, 11)
                    });
                }
            }

            await _dbService.GetAsignaturasCollection().InsertManyAsync(asignaturas);
            return asignaturas;
        }

        private async Task<List<Persona>> CrearPersonas(List<Institucion> instituciones)
        {
            var personas = new List<Persona>();
            var nombres = new[] { "Juan Pérez", "María García", "Carlos López", "Ana Martínez", "Luis Rodríguez", "Carmen Sánchez", "Pedro González", "Laura Fernández" };
            var roles = new[] { "Estudiante", "Profesor", "Monitor", "Coordinador" };

            foreach (var inst in instituciones)
            {
                foreach (var nombre in nombres)
                {
                    personas.Add(new Persona
                    {
                        PersonaId = $"pers_{inst.InstitucionId}_{personas.Count + 1:D3}",
                        InstitucionId = inst.InstitucionId,
                        Nombre = nombre,
                        Rol = roles[Random.Shared.Next(roles.Length)],
                        Edad = Random.Shared.Next(18, 65)
                    });
                }
            }

            await _dbService.GetPersonasCollection().InsertManyAsync(personas);
            return personas;
        }
    }
}