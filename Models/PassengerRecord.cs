using System.ComponentModel.DataAnnotations;

namespace MyToursApi.Models
{
    public class PassengerRecord
    {
        public int Id { get; set; }

        [Required]
        public DateTime TourDate { get; set; }

        public string? TourType { get; set; }
        public string? Seats { get; set; }
        public string? Surname { get; set; }
        public string? FirstName { get; set; }

        public int Pax { get; set; }
        public int OriginalPax { get; set; }
        public string? Notes { get; set; }



        public string? EmailAddress { get; set; }
        public string? UniqueReference { get; set; }
        public string? PhoneNumber { get; set; }
        public bool CheckedIn { get; set; }

        public string? CheckedInBy { get; set; }

    }
}
