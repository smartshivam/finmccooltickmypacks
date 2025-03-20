using System.ComponentModel.DataAnnotations;

namespace MyToursApi.Models
{
    public class ArchivePassengerRecord
    {
        public int Id { get; set; }

        public DateTime ArchivedAt { get; set; } 

        public DateTime TourDate { get; set; }
        public string? TourType { get; set; }
        public string? Seats { get; set; }
        public string? Surname { get; set; }
        public string? FirstName { get; set; }
        public int Pax { get; set; }
        public string? EmailAddress { get; set; }
        public string? UniqueReference { get; set; }
        public string? PhoneNumber { get; set; }
        public bool CheckedIn { get; set; }
    }
}
