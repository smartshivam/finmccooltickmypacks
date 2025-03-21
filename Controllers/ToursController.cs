using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyToursApi.Data;
using MyToursApi.Models;

namespace MyToursApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ToursController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ToursController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Tours/Today
        [HttpGet("Today")]
        public async Task<IActionResult> GetTodayTours()
        {
            

            var tours = await _context.Tours
                .Include(t => t.Passengers)
                .ToListAsync();

            return Ok(tours);
        }

        // GET: api/Tours/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetTour(int id)
        {
            var tour = await _context.Tours
                .Include(t => t.Passengers)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tour == null)
            {
                return NotFound();
            }

            return Ok(tour);
        }
    }
}
