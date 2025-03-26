using System.ComponentModel.DataAnnotations;

namespace MyToursApi.Models
{
    public class Tour
    {
        public int Id { get; set; }

        [Required]
        public DateTime TourDate { get; set; }

        public string? TourType { get; set; }

        public string? TourName { get; set; }

        public ICollection<Passenger>? Passengers { get; set; }

        public string? GuideName { get; set; }

    }
}
