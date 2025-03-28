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
        
        // GET: api/Tours/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetTour(int id)
        {
            var tour = await _context.Tours
                .Include(t => t.Passengers)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tour == null)
                return NotFound();

            return Ok(tour);
        }

        // PUT: api/Tours/guide?tourType=...
        [HttpPut("guide")]
        public async Task<IActionResult> UpdateTourGuide([FromQuery] string tourType, [FromBody] UpdateTourDto dto)
        {
            if (string.IsNullOrEmpty(tourType))
                return BadRequest("tourType is required.");

            var tour = await _context.Tours.FirstOrDefaultAsync(t => t.TourType == tourType);
            if (tour == null)
            {
                tour = new Tour
                {
                    TourType = tourType,
                    TourName = tourType,
                    TourDate = DateTime.UtcNow, 
                    GuideName = dto.GuideName
                };
                _context.Tours.Add(tour);
                await _context.SaveChangesAsync();
                return Ok(tour);
            }

            tour.GuideName = dto.GuideName;
            await _context.SaveChangesAsync();
            return Ok(tour);
        }

        // GET: api/tours/byType?tourType=Dublin%20-%20Titanic
        [HttpGet("byType")]
        public async Task<IActionResult> GetTourByType([FromQuery] string tourType)
        {
            if (string.IsNullOrEmpty(tourType))
                return BadRequest("tourType is required.");

            var tour = await _context.Tours
                .FirstOrDefaultAsync(t => t.TourType == tourType);

            if (tour == null)
                return NotFound("Tour not found.");

            return Ok(tour);
        }

        // GET: api/records/allTours
        [HttpGet("allTours")]
        public async Task<IActionResult> GetAllTours()
        {

            var result = await _context.PassengerRecords
                .GroupBy(r => r.TourType)
                .Select(g => new {
                    TourType = g.Key,
                    PaxSum = g.Sum(x => x.Pax)
                })
                .OrderBy(x => x.TourType)
                .ToListAsync();

            return Ok(result);
        }



        public class UpdateTourDto
        {
            public string? GuideName { get; set; }
        }
    }
}
