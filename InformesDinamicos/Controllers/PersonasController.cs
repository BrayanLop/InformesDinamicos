using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using InformesDinamicos.Data.Models;
using InformesDinamicos.Services;

namespace InformesDinamicos.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PersonasController : ControllerBase
    {
        private readonly IMongoCollection<Persona> _collection;

        public PersonasController(DatabaseService dbService)
        {
            _collection = dbService.GetPersonasCollection();
        }

        [HttpGet]
        public async Task<ActionResult<List<Persona>>> GetAll([FromQuery] string? rol = null, [FromQuery] string? institucionId = null, [FromQuery] int? edadMin = null, [FromQuery] int? edadMax = null)
        {
            var filterBuilder = Builders<Persona>.Filter;
            var filter = filterBuilder.Empty;

            if (!string.IsNullOrEmpty(rol))
                filter &= filterBuilder.Eq(x => x.Rol, rol);
            
            if (!string.IsNullOrEmpty(institucionId))
                filter &= filterBuilder.Eq(x => x.InstitucionId, institucionId);

            if (edadMin.HasValue)
                filter &= filterBuilder.Gte(x => x.Edad, edadMin.Value);

            if (edadMax.HasValue)
                filter &= filterBuilder.Lte(x => x.Edad, edadMax.Value);

            var personas = await _collection.Find(filter).ToListAsync();
            return Ok(personas);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Persona>> GetById(string id)
        {
            var persona = await _collection.Find(x => x.PersonaId == id).FirstOrDefaultAsync();
            return persona == null ? NotFound() : Ok(persona);
        }

        [HttpPost]
        public async Task<ActionResult<Persona>> Create(Persona persona)
        {
            persona.PersonaId = $"pers_{DateTime.Now:yyyyMMddHHmmss}_{Random.Shared.Next(1000, 9999)}";
            persona.CreatedAt = DateTime.UtcNow;
            persona.LastUpdated = DateTime.UtcNow;
            
            await _collection.InsertOneAsync(persona);
            return CreatedAtAction(nameof(GetById), new { id = persona.PersonaId }, persona);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, Persona persona)
        {
            persona.LastUpdated = DateTime.UtcNow;
            persona.Version++;
            
            var result = await _collection.ReplaceOneAsync(x => x.PersonaId == id, persona);
            return result.MatchedCount == 0 ? NotFound() : NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _collection.DeleteOneAsync(x => x.PersonaId == id);
            return result.DeletedCount == 0 ? NotFound() : NoContent();
        }
    }
}