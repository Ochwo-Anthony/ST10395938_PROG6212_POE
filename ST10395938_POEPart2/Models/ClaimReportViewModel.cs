namespace ST10395938_POEPart2.Models
{
    public class ClaimReportViewModel
    {
        public int Id { get; set; }
        public string LecturerName { get; set; }
        public string ClaimType { get; set; }
        public decimal Amount { get; set; }
        public string CoordinatorApproval { get; set; }
        public string ManagerApproval { get; set; }
        public DateTime DateSubmitted { get; set; }
    }
}
