using System.ComponentModel.DataAnnotations;

namespace BloodDonationSystem.Models
{
    public class BloodType
    {
        public int Id { get; set; }

        [Required]
        [StringLength(10)]
        public string TypeName { get; set; } = string.Empty; // A+, A-, B+, B-, AB+, AB-, O+, O-

        [StringLength(200)]
        public string? Description { get; set; }

        // Navigation properties
        public ICollection<Donor> Donors { get; set; } = new List<Donor>();
        public ICollection<BloodRequest> BloodRequests { get; set; } = new List<BloodRequest>();
    }
}
