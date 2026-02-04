using BloodDonationSystem.Data;
using BloodDonationSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace BloodDonationSystem.Services.SpecialServices
{
    public class BloodInventoryService
    {
        private readonly ApplicationDbContext _context;

        public BloodInventoryService(ApplicationDbContext context)
        {
            _context = context;
        }

        public int GetTotalAvailableStock()
        {
            var totalCompleted = _context.Donations
                .Where(d => d.Status == "Completed")
                .Sum(d => (int?)d.Quantity) ?? 0;

            var totalFulfilled = _context.BloodRequests
                .Where(r => r.Status == "Fulfilled")
                .Sum(r => (int?)r.Quantity) ?? 0;

            return totalCompleted - totalFulfilled;
        }

        public int GetTotalCollectedStock()
        {
            return _context.Donations
                .Where(d => d.Status == "Completed")
                .Sum(d => (int?)d.Quantity) ?? 0;
        }

        public int GetTotalFulfilledQuantity()
        {
            return _context.BloodRequests
                .Where(r => r.Status == "Fulfilled")
                .Sum(r => (int?)r.Quantity) ?? 0;
        }

        public int GetAvailableStockByBloodType(int bloodTypeId)
        {
            var totalDonated = _context.Donations
                .Where(d => d.Status == "Completed" && d.Donor.BloodTypeId == bloodTypeId)
                .Sum(d => (int?)d.Quantity) ?? 0;

            var totalFulfilled = _context.BloodRequests
                .Where(r => r.Status == "Fulfilled" && r.FulfilledWithBloodTypeId == bloodTypeId)
                .Sum(r => (int?)r.Quantity) ?? 0;

            return totalDonated - totalFulfilled;
        }

        public int GetCollectedStockByBloodType(int bloodTypeId)
        {
            return _context.Donations
                .Where(d => d.Status == "Completed" && d.Donor.BloodTypeId == bloodTypeId)
                .Sum(d => (int?)d.Quantity) ?? 0;
        }

        public int GetFulfilledQuantityByBloodType(int bloodTypeId)
        {
            return _context.BloodRequests
                .Where(r => r.Status == "Fulfilled" && r.FulfilledWithBloodTypeId == bloodTypeId)
                .Sum(r => (int?)r.Quantity) ?? 0;
        }

        public List<BloodTypeInventoryStats> GetBloodTypeDistribution()
        {
            var bloodTypes = _context.BloodTypes.ToList();
            var stats = new List<BloodTypeInventoryStats>();

            foreach (var bloodType in bloodTypes)
            {
                var totalCollected = GetCollectedStockByBloodType(bloodType.Id);
                var fulfilledQuantity = GetFulfilledQuantityByBloodType(bloodType.Id);
                var available = totalCollected - fulfilledQuantity;

                stats.Add(new BloodTypeInventoryStats
                {
                    BloodTypeId = bloodType.Id,
                    BloodTypeName = bloodType.TypeName,
                    Description = bloodType.Description,
                    DonorCount = _context.Donors.Count(d => d.BloodTypeId == bloodType.Id),
                    CompletedDonations = _context.Donations.Count(d => d.Status == "Completed" && d.Donor.BloodTypeId == bloodType.Id),
                    TotalCollectedML = totalCollected,
                    FulfilledRequestsML = fulfilledQuantity,
                    AvailableML = available,
                    PendingRequestCount = _context.BloodRequests.Count(r => (r.Status == "Pending" || r.Status == "Approved") && r.BloodTypeId == bloodType.Id),
                    PendingRequestQuantityML = _context.BloodRequests
                        .Where(r => (r.Status == "Pending" || r.Status == "Approved") && r.BloodTypeId == bloodType.Id)
                        .Sum(r => (int?)r.Quantity) ?? 0
                });
            }

            return stats;
        }

        public InventorySummary GetInventorySummary()
        {
            var totalCompleted = GetTotalCollectedStock();
            var totalFulfilled = GetTotalFulfilledQuantity();
            var totalAvailable = totalCompleted - totalFulfilled;

            return new InventorySummary
            {
                TotalCollectedML = totalCompleted,
                TotalFulfilledML = totalFulfilled,
                TotalAvailableML = totalAvailable,
                TotalDonations = _context.Donations.Count(d => d.Status == "Completed"),
                TotalDonors = _context.Donors.Count(),
                ActiveDonors = _context.Donors.Count(d => d.IsAvailable),
                TotalRequests = _context.BloodRequests.Count(),
                FulfilledRequests = _context.BloodRequests.Count(r => r.Status == "Fulfilled"),
                PendingRequests = _context.BloodRequests.Count(r => r.Status == "Pending"),
                ApprovedRequests = _context.BloodRequests.Count(r => r.Status == "Approved")
            };
        }
    }

    public class BloodTypeInventoryStats
    {
        public int BloodTypeId { get; set; }
        public string BloodTypeName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int DonorCount { get; set; }
        public int CompletedDonations { get; set; }
        public int TotalCollectedML { get; set; }
        public int FulfilledRequestsML { get; set; }
        public int AvailableML { get; set; }
        public int PendingRequestCount { get; set; }
        public int PendingRequestQuantityML { get; set; }

        public string GetStatus()
        {
            if (AvailableML <= 0) return "Out of Stock";
            if (AvailableML < 1000) return "Critical";
            if (AvailableML < 2000) return "Low Stock";
            return "Sufficient";
        }
    }

    public class InventorySummary
    {
        public int TotalCollectedML { get; set; }
        public int TotalFulfilledML { get; set; }
        public int TotalAvailableML { get; set; }
        public int TotalDonations { get; set; }
        public int TotalDonors { get; set; }
        public int ActiveDonors { get; set; }
        public int TotalRequests { get; set; }
        public int FulfilledRequests { get; set; }
        public int PendingRequests { get; set; }
        public int ApprovedRequests { get; set; }
    }
}
