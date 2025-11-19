using System.ComponentModel.DataAnnotations;

namespace ST10395938_POEPart2.Models
{
    public class LecturerClaim
    {
        public int Id { get; set; }

        [Required]
        public string LecturerName { get; set; } = "";

        public decimal HoursWorked { get; set; }

        public decimal Rate {  get; set; }

        [MaxLength(500)]
        public string? Note { get; set; }
        
        public string? EvidenceFile { get; set; }
        
        [Required]
        public string Status { get; set; } = "Pending";

        public string? ReviewNote { get; set; }

        public string? ReviewedBy { get; set; }

        public DateTime? ReviewedAt { get; set; }

        [Required]
        public string PaymentStatus { get; set; } = "Unpaid";

        public string? PaymentReference { get; set; }

        public DateTime? PaidUTc {  get; set; }

        public DateTime CreateAt { get; set; } = DateTime.UtcNow;

        public decimal CalculateTotalAmount()
        {
            return HoursWorked * Rate; 
        }
    }
}
