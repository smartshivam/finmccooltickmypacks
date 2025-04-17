using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyToursApi.Data;
using MyToursApi.Models;
using ClosedXML.Excel;
using System.Globalization;
using DocumentFormat.OpenXml.ExtendedProperties;

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

            var oldRecords = await _context.PassengerRecords.ToListAsync();
            _context.PassengerRecords.RemoveRange(oldRecords);
            await _context.SaveChangesAsync();

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
                        string notes = row.Cell(12).GetString();

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
                            OriginalPax = pax,
                            EmailAddress = emailAddress,
                            UniqueReference = uniqueReference,
                            PhoneNumber = phoneNumber,
                            CheckedIn = false,
                            CheckedInBy = null,
                            Notes = notes
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

        [HttpPost("{id}/checkin")]
        public async Task<IActionResult> CheckIn(int id)
        {
            var record = await _context.PassengerRecords.FindAsync(id);
            if (record == null) return NotFound();

            var guideName = User.Identity?.Name ?? "Unknown";

            record.CheckedIn = true;
            record.CheckedInBy = guideName;

            await _context.SaveChangesAsync();
            return Ok("Checked in");
        }

        [HttpPost("{id}/remove-checkin")]
        public async Task<IActionResult> RemoveCheckIn(int id)
        {
            var record = await _context.PassengerRecords.FindAsync(id);
            if (record == null) return NotFound();

            record.CheckedIn = false;
            record.CheckedInBy = null;

            await _context.SaveChangesAsync();
            return Ok("Check-in removed");
        }

        // 4) Stats 
        // GET: api/records/stats
        [HttpGet("stats")]
        public IActionResult GetStats()
        {
            DateTime now = DateTime.UtcNow;

            var records = _context.PassengerRecords
                .Where(r => r.TourDate < now)
                .ToList();

            var statsResult = records
                .GroupBy(r => r.TourType)
                .Select(g =>
                {
                    var tourType = g.Key ?? "Unknown";
                    var totalClients = g.Sum(x => x.Pax);
                    var checkedInCount = g.Count(x => x.CheckedIn);
                    var notArrivedCount = totalClients - checkedInCount;
                    var tourDate = g.Min(x => x.TourDate.Date);

                    var guidesStats = g.GroupBy(x => x.CheckedInBy ?? "None")
                                       .Select(gg => new
                                       {
                                           GuideName = gg.Key,
                                           Clients = gg.Sum(x => x.Pax),
                                           CheckedInCount = gg.Count(x => x.CheckedIn),
                                           NotArrivedCount = gg.Sum(x => x.Pax) - gg.Count(x => x.CheckedIn)
                                       })
                                       .ToList();

                    return new
                    {
                        TourDate = tourDate,
                        TourType = tourType,
                        TotalClients = totalClients,
                        CheckedInCount = checkedInCount,
                        NotArrivedCount = notArrivedCount,
                        Guides = guidesStats
                    };
                })
                .OrderBy(x => x.TourDate)
                .ThenBy(x => x.TourType)
                .ToList();

            return Ok(statsResult);
        }

        [HttpPut("{id}/pax")]
        public async Task<IActionResult> UpdatePax(int id, [FromBody] PaxModel model)
        {
            var record = await _context.PassengerRecords.FindAsync(id);
            if (record == null)
                return NotFound("Record not found.");

            // Update Pax
            record.Pax = model.Pax;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Pax updated" });
        }




        // POST: api/records/checkin-unique
        [HttpPost("checkin-unique")]
        public async Task<IActionResult> CheckInByUniqueRef([FromBody] UniqueRefModel model)
        {
            if (string.IsNullOrEmpty(model.UniqueRef))
                return BadRequest("UniqueRef is required.");

            var passenger = await _context.PassengerRecords
                .FirstOrDefaultAsync(p => p.UniqueReference == model.UniqueRef);

            if (passenger == null)
                return NotFound("Passenger not found.");

            if (!string.Equals(passenger.TourType, model.TourType, StringComparison.OrdinalIgnoreCase))
            {
                var msg = $"This passenger belongs to a different tour: {passenger.TourType}.\n" +
                          $"Name: {passenger.FirstName} {passenger.Surname}";
                return BadRequest(msg);
            }

            string guideName = User.Identity?.Name ?? "Unknown";

            passenger.CheckedIn = true;
            passenger.CheckedInBy = guideName;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Checked in successfully.",
                Passenger = passenger
            });
        }




        // GET: api/records/download-today
        [HttpGet("download-today")]
        public async Task<IActionResult> DownloadTodayReport()
        {
            DateTime today = DateTime.UtcNow.Date;

            var records = await _context.PassengerRecords
                .OrderBy(r => r.TourType)
                .ThenBy(r => r.TourDate)
                .ToListAsync();

            if (records == null || records.Count == 0)
            {
                return NotFound("No records found for today.");
            }

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Today Report");
                int row = 1;

                var groups = records
                    .GroupBy(r => r.TourType)
                    .OrderBy(g => g.Key);

                foreach (var group in groups)
                {
                    int groupPassengers = group.Sum(r => r.Pax);
                    worksheet.Cell(row, 1).Value = $"{group.Key} ({groupPassengers} passengers)";
                    worksheet.Range(row, 1, row, 6).Merge().Style.Font.Bold = true;
                    row++;

                    worksheet.Cell(row, 1).Value = "Tour Date";
                    worksheet.Cell(row, 2).Value = "Surname";
                    worksheet.Cell(row, 3).Value = "First Name";
                    worksheet.Cell(row, 4).Value = "Pax";
                    worksheet.Cell(row, 5).Value = "Checked";
                    worksheet.Cell(row, 6).Value = "Guide";
                    row++;

                    foreach (var record in group)
                    {
                        worksheet.Cell(row, 1).Value = record.TourDate.ToString("d", CultureInfo.InvariantCulture);
                        worksheet.Cell(row, 2).Value = record.Surname;
                        worksheet.Cell(row, 3).Value = record.FirstName;
                        worksheet.Cell(row, 4).Value = record.Pax;
                        worksheet.Cell(row, 5).Value = record.CheckedIn ? "Yes" : "No";
                        worksheet.Cell(row, 6).Value = record.CheckedInBy;
                        row++;
                    }
                    row++;
                }

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    string fileName = $"TodayReport_{today:yyyyMMdd}.xlsx";
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
        }

        // POST: api/records/create
        [HttpPost("create")]
        public async Task<IActionResult> CreatePassenger([FromBody] CreatePassengerDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.TourType))
                return BadRequest("TourType is required.");

            // creating new passenger record
            var newRecord = new PassengerRecord
            {
                TourDate = dto.TourDate,
                TourType = dto.TourType,
                Surname = dto.Surname,
                FirstName = dto.FirstName,
                Pax = dto.Pax,
                Seats = dto.Seats,
                EmailAddress = dto.EmailAddress,
                UniqueReference = dto.UniqueReference,
                PhoneNumber = dto.PhoneNumber,
                CheckedIn = false,
                CheckedInBy = null
            };

            _context.PassengerRecords.Add(newRecord);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "New passenger created successfully.",
                PassengerId = newRecord.Id
            });
        }

        // POST: api/records/remove
        [HttpPost("remove")]
        public async Task<IActionResult> RemovePassenger(int id)
        {
            var record = await _context.PassengerRecords.FindAsync(id);

            _context.PassengerRecords.Remove(record);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "New passenger removed successfully.",
             
            });
        }
        public class CreatePassengerDto
        {
            public DateTime TourDate { get; set; }
            public string TourType { get; set; }
            public string Surname { get; set; }
            public string FirstName { get; set; }
            public int Pax { get; set; }
            public string? Seats { get; set; }
            public string? EmailAddress { get; set; }
            public string? UniqueReference { get; set; }
            public string? PhoneNumber { get; set; }
        }
        public class UniqueRefModel
        {
            public string UniqueRef { get; set; }
            public string TourType { get; set; }
        }

        public class PaxModel
        {
            public int Pax { get; set; }
        }



    }
}
