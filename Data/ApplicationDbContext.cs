using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MyToursApi.Models;

namespace MyToursApi.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }


        public DbSet<Tour> Tours { get; set; }
        public DbSet<Passenger> Passengers { get; set; }
        public DbSet<PassengerRecord> PassengerRecords { get; set; }
        public DbSet<ArchivePassengerRecord> ArchivePassengerRecords { get; set; }
    }
}
