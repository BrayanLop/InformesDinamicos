using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using InformesDinamicos.Data.Models;
using InformesDinamicos.Services;

namespace InformesDinamicos.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InstitucionesController : ControllerBase
    {
        private readonly IMongoCollection<Institucion> _collection;

        public InstitucionesController(DatabaseService dbService)
        {
            _collection = dbService.GetInstitucionesCollection();
        }

        [HttpGet]
        public async Task<ActionResult<List<Institucion>>> GetAll()
        {
            var instituciones = await _collection.Find(_ => true).ToListAsync();
            return Ok(instituciones);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Institucion>> GetById(string id)
        {
            var institucion = await _collection.Find(x => x.InstitucionId == id).FirstOrDefaultAsync();
            return institucion == null ? NotFound() : Ok(institucion);
        }

        [HttpPost]
        public async Task<ActionResult<Institucion>> Create(Institucion institucion)
        {
            institucion.InstitucionId = $"inst_{DateTime.Now:yyyyMMddHHmmss}_{Random.Shared.Next(1000, 9999)}";
            institucion.CreatedAt = DateTime.UtcNow;
            institucion.LastUpdated = DateTime.UtcNow;
            
            await _collection.InsertOneAsync(institucion);
            return CreatedAtAction(nameof(GetById), new { id = institucion.InstitucionId }, institucion);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, Institucion institucion)
        {
            institucion.LastUpdated = DateTime.UtcNow;
            institucion.Version++;
            
            var result = await _collection.ReplaceOneAsync(x => x.InstitucionId == id, institucion);
            return result.MatchedCount == 0 ? NotFound() : NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _collection.DeleteOneAsync(x => x.InstitucionId == id);
            return result.DeletedCount == 0 ? NotFound() : NoContent();
        }
    }
}