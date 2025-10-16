using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using InformesDinamicos.Data.Models;
using InformesDinamicos.Services;

namespace InformesDinamicos.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AsignaturasController : ControllerBase
    {
        private readonly IMongoCollection<Asignatura> _collection;

        public AsignaturasController(DatabaseService dbService)
        {
            _collection = dbService.GetAsignaturasCollection();
        }

        [HttpGet]
        public async Task<ActionResult<List<Asignatura>>> GetAll([FromQuery] string? programaId = null, [FromQuery] string? institucionId = null)
        {
            var filterBuilder = Builders<Asignatura>.Filter;
            var filter = filterBuilder.Empty;

            if (!string.IsNullOrEmpty(programaId))
                filter &= filterBuilder.Eq(x => x.ProgramaId, programaId);
            
            if (!string.IsNullOrEmpty(institucionId))
                filter &= filterBuilder.Eq(x => x.InstitucionId, institucionId);

            var asignaturas = await _collection.Find(filter).ToListAsync();
            return Ok(asignaturas);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Asignatura>> GetById(string id)
        {
            var asignatura = await _collection.Find(x => x.AsignaturaId == id).FirstOrDefaultAsync();
            return asignatura == null ? NotFound() : Ok(asignatura);
        }

        [HttpPost]
        public async Task<ActionResult<Asignatura>> Create(Asignatura asignatura)
        {
            asignatura.AsignaturaId = $"asig_{DateTime.Now:yyyyMMddHHmmss}_{Random.Shared.Next(1000, 9999)}";
            asignatura.CreatedAt = DateTime.UtcNow;
            asignatura.LastUpdated = DateTime.UtcNow;
            
            await _collection.InsertOneAsync(asignatura);
            return CreatedAtAction(nameof(GetById), new { id = asignatura.AsignaturaId }, asignatura);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, Asignatura asignatura)
        {
            asignatura.LastUpdated = DateTime.UtcNow;
            asignatura.Version++;
            
            var result = await _collection.ReplaceOneAsync(x => x.AsignaturaId == id, asignatura);
            return result.MatchedCount == 0 ? NotFound() : NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _collection.DeleteOneAsync(x => x.AsignaturaId == id);
            return result.DeletedCount == 0 ? NotFound() : NoContent();
        }
    }
}