using System.ComponentModel.DataAnnotations;

namespace BloodDonationSystem.Models
{
    public class BloodRequest
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string HospitalName { get; set; } = string.Empty;

        [Required]
        public int BloodTypeId { get; set; }

        [Required]
        [Range(1, 10000)]
        public int Quantity { get; set; } // in ml

        [Required]
        public DateTime RequestDate { get; set; } = DateTime.Now;

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected, Fulfilled

        public bool IsEmergency { get; set; } = false;

        [StringLength(500)]
        public string? Notes { get; set; }

        [Required]
        public int RequestedBy { get; set; } // Hospital User Id

        public DateTime? ApprovedAt { get; set; }

        public int? ApprovedBy { get; set; } // Admin User Id

        [StringLength(500)]
        public string? AdminNotes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation property
        public BloodType BloodType { get; set; } = null!;
    }
}
