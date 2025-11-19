using Microsoft.AspNetCore.Identity;

namespace ST10395938_POEPart2.Models
{
    public class Users : IdentityUser
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public decimal HourlyRate { get; set; }
    }
}
