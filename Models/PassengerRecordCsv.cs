using CsvHelper.Configuration.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace MyToursApi.Models
{
    public class PassengerRecordCsv
    {
        [Name("Tour date")]
        public string TourDate { get; set; }

        [Name("Tour type")]
        public string TourType { get; set; }

        [Name("Seats")]
        public string Seats { get; set; }

        [Name("Surname")]
        public string Surname { get; set; }

        [Name("First name")]
        public string FirstName { get; set; }

        [Name("Pax")]
        public string Pax { get; set; }

        [Name("E-mail address")]
        public string EmailAddress { get; set; }

        [Name("Unique reference")]
        public string UniqueReference { get; set; }

        [Name("Phone number")]
        public string PhoneNumber { get; set; }
    }
}
