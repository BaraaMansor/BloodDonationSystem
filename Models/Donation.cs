using System.ComponentModel.DataAnnotations;

namespace BloodDonationSystem.Models
{
    public class Donation
    {
        public int Id { get; set; }

        [Required]
        public int DonorId { get; set; }

        [Required]
        public DateTime DonationDate { get; set; }

        [Required]
        [Range(1, 1000)]
        public int Quantity { get; set; } // in ml

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected, Completed

        [StringLength(500)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? ApprovedAt { get; set; }

        public int? ApprovedBy { get; set; } // Admin User Id

        // Navigation property
        public Donor Donor { get; set; } = null!;
    }
}
