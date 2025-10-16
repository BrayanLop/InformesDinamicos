using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using InformesDinamicos.Data.Models;
using InformesDinamicos.Services;

namespace InformesDinamicos.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProgramasController : ControllerBase
    {
        private readonly IMongoCollection<Programa> _collection;

        public ProgramasController(DatabaseService dbService)
        {
            _collection = dbService.GetProgramasCollection();
        }

        [HttpGet]
        public async Task<ActionResult<List<Programa>>> GetAll([FromQuery] string? institucionId = null)
        {
            var filter = string.IsNullOrEmpty(institucionId) 
                ? Builders<Programa>.Filter.Empty 
                : Builders<Programa>.Filter.Eq(x => x.InstitucionId, institucionId);
                
            var programas = await _collection.Find(filter).ToListAsync();
            return Ok(programas);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Programa>> GetById(string id)
        {
            var programa = await _collection.Find(x => x.ProgramaId == id).FirstOrDefaultAsync();
            return programa == null ? NotFound() : Ok(programa);
        }

        [HttpPost]
        public async Task<ActionResult<Programa>> Create(Programa programa)
        {
            programa.ProgramaId = $"prog_{DateTime.Now:yyyyMMddHHmmss}_{Random.Shared.Next(1000, 9999)}";
            programa.CreatedAt = DateTime.UtcNow;
            programa.LastUpdated = DateTime.UtcNow;
            
            await _collection.InsertOneAsync(programa);
            return CreatedAtAction(nameof(GetById), new { id = programa.ProgramaId }, programa);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, Programa programa)
        {
            programa.LastUpdated = DateTime.UtcNow;
            programa.Version++;
            
            var result = await _collection.ReplaceOneAsync(x => x.ProgramaId == id, programa);
            return result.MatchedCount == 0 ? NotFound() : NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _collection.DeleteOneAsync(x => x.ProgramaId == id);
            return result.DeletedCount == 0 ? NotFound() : NoContent();
        }
    }
}