using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyToursApi.Models
{
    public class Passenger
    {
        public int Id { get; set; }

        public Guid PassengerGuid { get; set; } = Guid.NewGuid();

        [Required]
        public int TourId { get; set; }

        [Required]
        public DateTime Timestamp { get; set; }

        public string? Surname { get; set; }
        public string? FirstName { get; set; }

        public int Pax { get; set; }

        public string? Email { get; set; }
        public string? UniqueReference { get; set; }
        public string? OtherBookingReference { get; set; }
        public string? PhoneNumber { get; set; }

        public string? QRCodeImage { get; set; }

        public bool CheckedIn { get; set; }

        [ForeignKey(nameof(TourId))]
        public Tour? Tour { get; set; }
    }
}
