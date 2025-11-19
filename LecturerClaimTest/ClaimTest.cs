using Xunit;
using ST10395938_POEPart2.Models;

namespace LecturerClaimTest
{
    public class ClaimTest
    {
        [Fact]
        public void CalculatedTotalAmount()
        {
            var claim = new LecturerClaim();

            claim.HoursWorked = 20;
            claim.Rate = 670;

            var getResult = claim.CalculateTotalAmount();

            Assert.Equal(13400, getResult);
        }

        [Fact]
        public void Notes_Simulation()
        {
            var claim = new LecturerClaim();

            claim.Note = "Test for notes";

            var notes = claim.Note;

            Assert.Equal("Test for notes", notes);
        }

        [Fact]
        public void EvidenceFile_StoredCorrectly()
        {
            
            var claim = new LecturerClaim();
            var expectedFileName = "invoice.pdf";

            
            claim.EvidenceFile = expectedFileName;

            
            Assert.Equal(expectedFileName, claim.EvidenceFile);
        }


    }
}
