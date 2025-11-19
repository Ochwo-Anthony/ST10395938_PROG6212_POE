using Microsoft.EntityFrameworkCore;
using ST10395938_POEPart2.Models;

namespace ST10395938_POEPart2.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) 
        { 
        
        }
        // Table for the User Model
        public DbSet<LecturerClaim> LecturerClaims { get; set; }
    }
}
