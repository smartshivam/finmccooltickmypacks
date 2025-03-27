using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyToursApi.Data;
using MyToursApi.Models;
using ClosedXML.Excel;
using System.Globalization;

namespace MyToursApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RecordsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public RecordsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1) Import Excel
        // POST: api/records/import-excel
        [HttpPost("import-excel")]
        public async Task<IActionResult> ImportExcel([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file provided.");

            // 1. Archive notes
            var oldRecords = await _context.PassengerRecords.ToListAsync();
            foreach (var rec in oldRecords)
            {
                var arch = new ArchivePassengerRecord
                {
                    ArchivedAt = DateTime.UtcNow,
                    TourDate = rec.TourDate,
                    TourType = rec.TourType,
                    Seats = rec.Seats,
                    Surname = rec.Surname,
                    FirstName = rec.FirstName,
                    Pax = rec.Pax,
                    EmailAddress = rec.EmailAddress,
                    UniqueReference = rec.UniqueReference,
                    PhoneNumber = rec.PhoneNumber,
                    CheckedIn = rec.CheckedIn
                };
                _context.ArchivePassengerRecords.Add(arch);
            }
            // 2. delete all notes from PassengerRecords
            _context.PassengerRecords.RemoveRange(oldRecords);
            await _context.SaveChangesAsync();

            // 3. Import new notes from excel
            using (var stream = file.OpenReadStream())
            using (var workbook = new XLWorkbook(stream))
            {
                var worksheet = workbook.Worksheet(1);
                if (worksheet == null)
                    return BadRequest("No worksheet found in the Excel file.");

                var rows = worksheet.RowsUsed().Skip(1);

                int totalRows = 0;
                int importedRows = 0;
                List<string> errorMessages = new List<string>();

                var dateFormats = new[] { "dd.MM.yyyy HH:mm:ss", "dd.MM.yyyy", "MM/dd/yyyy HH:mm:ss", "MM/dd/yyyy" };

                foreach (var row in rows)
                {
                    totalRows++;
                    try
                    {
                        string tourDateStr = row.Cell(2).GetString();
                        string tourType = row.Cell(3).GetString();
                        string seats = row.Cell(4).GetString();
                        string surname = row.Cell(5).GetString();
                        string firstName = row.Cell(6).GetString();
                        string paxStr = row.Cell(7).GetString();
                        string emailAddress = row.Cell(8).GetString();
                        string uniqueReference = row.Cell(9).GetString();
                        string phoneNumber = row.Cell(11).GetString();

                        if (string.IsNullOrWhiteSpace(tourDateStr))
                            continue;

                        if (!DateTime.TryParseExact(tourDateStr, dateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime tourDate))
                        {
                            errorMessages.Add($"Row {row.RowNumber()}: Invalid Tour Date: '{tourDateStr}'.");
                            continue;
                        }
                        tourDate = DateTime.SpecifyKind(tourDate, DateTimeKind.Utc);

                        int pax = 0;
                        int.TryParse(paxStr, out pax);

                        var record = new PassengerRecord
                        {
                            TourDate = tourDate,
                            TourType = tourType,
                            Seats = seats,
                            Surname = surname,
                            FirstName = firstName,
                            Pax = pax,
                            EmailAddress = emailAddress,
                            UniqueReference = uniqueReference,
                            PhoneNumber = phoneNumber,
                            CheckedIn = false
                        };
                        _context.PassengerRecords.Add(record);
                        importedRows++;
                    }
                    catch (Exception ex)
                    {
                        errorMessages.Add($"Row {row.RowNumber()}: Exception: {ex.Message}");
                        continue;
                    }
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    Message = "Import successful",
                    TotalRowsProcessed = totalRows,
                    RowsImported = importedRows,
                    Errors = errorMessages
                });
            }
        }

        // 2) GET /api/records 
        [HttpGet]
        public async Task<IActionResult> GetRecords([FromQuery] string? tourType)
        {
            IQueryable<PassengerRecord> query = _context.PassengerRecords;

            if (!string.IsNullOrEmpty(tourType))
            {
                query = query.Where(r => r.TourType != null && r.TourType.Contains(tourType));
            }

            var records = await query.OrderBy(r => r.TourDate).ToListAsync();
            return Ok(records);
        }




        // 3) Check-in/Remove-checkin 
        [HttpPost("{id}/checkin")]
        public async Task<IActionResult> CheckIn(int id)
        {
            var record = await _context.PassengerRecords.FindAsync(id);
            if (record == null) return NotFound();

            record.CheckedIn = true;
            await _context.SaveChangesAsync();
            return Ok("Checked in");
        }

        [HttpPost("{id}/remove-checkin")]
        public async Task<IActionResult> RemoveCheckIn(int id)
        {
            var record = await _context.PassengerRecords.FindAsync(id);
            if (record == null) return NotFound();

            record.CheckedIn = false;
            await _context.SaveChangesAsync();
            return Ok("Check-in removed");
        }

        // 4) Stats
        // GET /api/records/stats
        [HttpGet("stats")]
        public IActionResult GetStats()
        {
            // Текущее время, чтобы понять, что «завершённый тур» (completed) – тот, чья дата < now
            DateTime now = DateTime.UtcNow;

            // 1. Собираем данные из PassengerRecords (актуальная таблица)
            var completedActive = _context.PassengerRecords
                .Where(r => r.TourDate < now)  // только прошедшие
                .Select(r => new
                {
                    r.TourDate,
                    r.TourType,
                    r.Pax,
                    r.CheckedIn
                })
                .ToList();

            // 2. Собираем данные из ArchivePassengerRecords (архивная таблица)
            var completedArchive = _context.ArchivePassengerRecords
                .Where(r => r.TourDate < now)
                .Select(r => new
                {
                    r.TourDate,
                    r.TourType,
                    r.Pax,
                    r.CheckedIn
                })
                .ToList();

            // 3. Объединяем обе коллекции
            var allCompleted = completedActive.Concat(completedArchive).ToList();

            // 4. Берём список туров, чтобы узнать, есть ли гид на конкретную дату + тип тура
            var tours = _context.Tours
                .Select(t => new {
                    // Берём только «дату без времени»
                    DateOnly = t.TourDate.Date,
                    t.TourType,
                    t.GuideName
                })
                .ToList();

            // 5. Создаём словарь: ключ = (Date, TourType), значение = GuideName
            //    Если на одну дату + тип есть несколько гида, берём первый
            var toursDictionary = tours
                .GroupBy(x => new { x.DateOnly, x.TourType })
                .ToDictionary(
                    g => g.Key,
                    g => g.First().GuideName
                );

            // 6. Группируем пассажирские записи тоже по дате (без времени) и типу
            var statsResult = allCompleted
                .GroupBy(r => new { Day = r.TourDate.Date, Type = r.TourType })
                .Select(g =>
                {
                    var day = g.Key.Day;
                    var type = g.Key.Type;

                    // Пытаемся найти гида в словаре
                    string? guideName = null;
                    if (toursDictionary.TryGetValue(new { DateOnly = day, TourType = type }, out var foundGuide))
                    {
                        guideName = foundGuide;
                    }

                    var totalClients = g.Sum(x => x.Pax);
                    var checkedInCount = g.Count(x => x.CheckedIn);

                    return new
                    {
                        // Выводим день как TourDate
                        TourDate = day,
                        TourType = type,
                        GuideName = guideName,
                        TotalClients = totalClients,
                        CheckedInCount = checkedInCount,
                        NotArrivedCount = totalClients - checkedInCount
                    };
                })
                .OrderBy(x => x.TourDate)
                .ThenBy(x => x.TourType)
                .ToList();

            return Ok(statsResult);
        }



        // POST: api/records/checkin-unique
        [HttpPost("checkin-unique")]
        public async Task<IActionResult> CheckInByUniqueRef([FromBody] UniqueRefModel model)
        {
            if (string.IsNullOrEmpty(model.UniqueRef))
                return BadRequest("UniqueRef is required.");

            // Find passenger by his UniqueReference 
            var passenger = await _context.PassengerRecords
                .FirstOrDefaultAsync(p => p.UniqueReference == model.UniqueRef);

            if (passenger == null)
                return NotFound("Passenger not found.");

            
            passenger.CheckedIn = true;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Checked in successfully.",
                Passenger = passenger
            });
        }

        public class UniqueRefModel
        {
            public string UniqueRef { get; set; }
        }

    }
}
