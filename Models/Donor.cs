using System.ComponentModel.DataAnnotations;

namespace BloodDonationSystem.Models
{
    public class Donor
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int BloodTypeId { get; set; }

        [Required]
        public DateTime DateOfBirth { get; set; }

        [Required]
        [StringLength(10)]
        public string Gender { get; set; } = string.Empty; // Male, Female

        [Required]
        [StringLength(500)]
        public string Address { get; set; } = string.Empty;

        public bool IsAvailable { get; set; } = true;

        public DateTime? LastDonationDate { get; set; }

        [StringLength(500)]
        public string? MedicalNotes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        public User User { get; set; } = null!;
        public BloodType BloodType { get; set; } = null!;
        public ICollection<Donation> Donations { get; set; } = new List<Donation>();
    }
}
