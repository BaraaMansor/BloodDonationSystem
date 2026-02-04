using BloodDonationSystem.Data;
using BloodDonationSystem.Models;
using BloodDonationSystem.Services.SpecialServices;
using Microsoft.EntityFrameworkCore;

namespace BloodDonationSystem.Services.ApplicationServices;

public class AdminService
{
    private readonly ApplicationDbContext _context;
    private readonly BloodInventoryService _inventoryService;
    private readonly BloodCompatibilityService _bloodCompatibility;

    public AdminService(ApplicationDbContext context, BloodInventoryService inventoryService, BloodCompatibilityService bloodCompatibility)
    {
        _context = context;
        _inventoryService = inventoryService;
        _bloodCompatibility = bloodCompatibility;
    }

    public DashboardData GetDashboardData()
    {
        var summary = _inventoryService.GetInventorySummary();

        return new DashboardData
        {
            TotalBloodStock = summary.TotalAvailableML,
            TotalDonations = _context.Donations.Count(),
            PendingDonations = _context.Donations.Count(d => d.Status == "Pending"),
            ApprovedDonations = _context.Donations.Count(d => d.Status == "Approved"),
            TotalBloodRequests = _context.BloodRequests.Count(),
            PendingRequests = _context.BloodRequests.Count(r => r.Status == "Pending"),
            ApprovedRequests = _context.BloodRequests.Count(r => r.Status == "Approved"),
            TotalUsers = _context.Users.Count(),
            BloodTypeStats = _context.BloodTypes
                .Select(bt => new BloodTypeStat
                {
                    BloodType = bt.TypeName,
                    DonorCount = bt.Donors.Count(),
                    TotalDonations = bt.Donors.SelectMany(d => d.Donations).Count(d => d.Status == "Completed"),
                    PendingDonations = bt.Donors.SelectMany(d => d.Donations).Count(d => d.Status == "Pending"),
                    RequestCount = bt.BloodRequests.Count(r => r.Status == "Pending")
                }).ToList(),
            RecentDonations = _context.Donations
                .Include(d => d.Donor)
                .ThenInclude(d => d.User)
                .Include(d => d.Donor.BloodType)
                .OrderByDescending(d => d.CreatedAt)
                .Take(5)
                .ToList(),
            RecentRequests = _context.BloodRequests
                .Include(r => r.BloodType)
                .OrderByDescending(r => r.CreatedAt)
                .Take(5)
                .ToList()
        };
    }

    public List<User> GetAllUsers()
    {
        return _context.Users
            .Include(u => u.Donor)
            .ThenInclude(d => d!.BloodType)
            .OrderByDescending(u => u.CreatedAt)
            .ToList();
    }

    public User? GetUserDetails(int userId)
    {
        return _context.Users
            .Include(u => u.Donor)
            .ThenInclude(d => d!.BloodType)
            .Include(u => u.Donor)
            .ThenInclude(d => d!.Donations)
            .FirstOrDefault(u => u.Id == userId);
    }

    public OperationResult ToggleUserStatus(int userId)
    {
        var user = _context.Users.Find(userId);
        if (user == null)
        {
            return new OperationResult { Success = false, Message = "User not found." };
        }

        user.IsActive = !user.IsActive;
        _context.SaveChanges();

        return new OperationResult
        {
            Success = true,
            Message = $"User {(user.IsActive ? "activated" : "deactivated")} successfully."
        };
    }

    public List<Donation> GetAllDonations()
    {
        return _context.Donations
            .Include(d => d.Donor)
            .ThenInclude(d => d.User)
            .Include(d => d.Donor.BloodType)
            .OrderByDescending(d => d.CreatedAt)
            .ToList();
    }

    public OperationResult ApproveDonation(int donationId, int approvedByUserId)
    {
        var donation = _context.Donations.Include(d => d.Donor).FirstOrDefault(d => d.Id == donationId);
        if (donation == null)
        {
            return new OperationResult { Success = false, Message = "Donation not found." };
        }

        // Check if donor can donate (90 days rule)
        if (donation.Donor.LastDonationDate.HasValue)
        {
            var daysSinceLastDonation = (DateTime.Now - donation.Donor.LastDonationDate.Value).TotalDays;
            if (daysSinceLastDonation < 90)
            {
                return new OperationResult
                {
                    Success = false,
                    Message = $"Donor must wait {90 - (int)daysSinceLastDonation} more days before donating again."
                };
            }
        }

        donation.Status = "Approved";
        donation.ApprovedAt = DateTime.Now;
        donation.ApprovedBy = approvedByUserId;
        _context.SaveChanges();

        return new OperationResult { Success = true, Message = "Donation approved successfully." };
    }

    public OperationResult CompleteDonation(int donationId)
    {
        var donation = _context.Donations.Include(d => d.Donor).FirstOrDefault(d => d.Id == donationId);
        if (donation == null)
        {
            return new OperationResult { Success = false, Message = "Donation not found." };
        }

        donation.Status = "Completed";
        donation.Donor.LastDonationDate = donation.DonationDate;
        donation.Donor.IsAvailable = false;
        _context.SaveChanges();

        return new OperationResult { Success = true, Message = "Donation marked as completed." };
    }

    public OperationResult RejectDonation(int donationId, string reason, int rejectedByUserId)
    {
        var donation = _context.Donations.Find(donationId);
        if (donation == null)
        {
            return new OperationResult { Success = false, Message = "Donation not found." };
        }

        donation.Status = "Rejected";
        donation.Notes = reason ?? "Rejected by admin";
        donation.ApprovedBy = rejectedByUserId;
        _context.SaveChanges();

        return new OperationResult { Success = true, Message = "Donation rejected." };
    }

    public List<BloodRequest> GetAllBloodRequests()
    {
        return _context.BloodRequests
            .Include(r => r.BloodType)
            .OrderByDescending(r => r.CreatedAt)
            .ToList();
    }

    public OperationResult ApproveBloodRequest(int requestId, int approvedByUserId)
    {
        var request = _context.BloodRequests.Find(requestId);
        if (request == null)
        {
            return new OperationResult { Success = false, Message = "Request not found." };
        }

        request.Status = "Approved";
        request.ApprovedAt = DateTime.Now;
        request.ApprovedBy = approvedByUserId;
        _context.SaveChanges();

        return new OperationResult { Success = true, Message = "Blood request approved successfully." };
    }

    public OperationResult RejectBloodRequest(int requestId, string reason, int rejectedByUserId)
    {
        var request = _context.BloodRequests.Find(requestId);
        if (request == null)
        {
            return new OperationResult { Success = false, Message = "Request not found." };
        }

        request.Status = "Rejected";
        request.AdminNotes = reason ?? "Rejected by admin";
        request.ApprovedAt = DateTime.Now;
        request.ApprovedBy = rejectedByUserId;
        _context.SaveChanges();

        return new OperationResult { Success = true, Message = "Blood request rejected." };
    }

    public OperationResult FulfillBloodRequest(int requestId)
    {
        var request = _context.BloodRequests
            .Include(r => r.BloodType)
            .FirstOrDefault(r => r.Id == requestId);

        if (request == null)
        {
            return new OperationResult { Success = false, Message = "Request not found." };
        }

        // Get compatible blood types in priority order
        var compatibleBloodTypes = _bloodCompatibility.GetFulfillmentPriority(request.BloodType.TypeName);

        // Get blood type objects for compatible types
        var bloodTypeMap = _context.BloodTypes
            .Where(bt => compatibleBloodTypes.Contains(bt.TypeName))
            .ToDictionary(bt => bt.TypeName, bt => bt.Id);

        // Find which blood type has enough quantity available
        int? selectedBloodTypeId = null;
        int selectedAvailable = 0;
        string selectedBloodType = "";

        foreach (var bloodTypeName in compatibleBloodTypes)
        {
            if (!bloodTypeMap.ContainsKey(bloodTypeName)) continue;

            var bloodTypeId = bloodTypeMap[bloodTypeName];
            var available = _inventoryService.GetAvailableStockByBloodType(bloodTypeId);

            if (available >= request.Quantity)
            {
                selectedBloodTypeId = bloodTypeId;
                selectedAvailable = available;
                selectedBloodType = bloodTypeName;
                break;
            }
        }

        // If no single blood type has enough, check total across all compatible types
        if (!selectedBloodTypeId.HasValue)
        {
            var compatibleBloodTypeIds = bloodTypeMap.Values.ToList();
            var totalAvailable = compatibleBloodTypeIds
                .Sum(btId => _inventoryService.GetAvailableStockByBloodType(btId));

            return new OperationResult
            {
                Success = false,
                Message = $"Insufficient compatible blood! Available: {totalAvailable}ml, Requested: {request.Quantity}ml. Need {request.Quantity - totalAvailable}ml more. Compatible types: {string.Join(", ", compatibleBloodTypes)}"
            };
        }

        // Mark as fulfilled and record which blood type was actually used
        request.Status = "Fulfilled";
        request.FulfilledWithBloodTypeId = selectedBloodTypeId.Value;
        _context.SaveChanges();

        var message = selectedBloodType == request.BloodType.TypeName
            ? $"Blood request fulfilled successfully with {selectedBloodType}. Remaining: {selectedAvailable - request.Quantity}ml"
            : $"Blood request fulfilled successfully using compatible blood type {selectedBloodType} (requested: {request.BloodType.TypeName}). Remaining {selectedBloodType}: {selectedAvailable - request.Quantity}ml";

        return new OperationResult { Success = true, Message = message };
    }

    public ReportData GetReportData()
    {
        var bloodTypeStats = _inventoryService.GetBloodTypeDistribution();
        var summary = _inventoryService.GetInventorySummary();

        return new ReportData
        {
            TotalDonors = summary.TotalDonors,
            ActiveDonors = summary.ActiveDonors,
            TotalDonations = _context.Donations.Count(),
            CompletedDonations = summary.TotalDonations,
            TotalBloodRequests = summary.TotalRequests,
            FulfilledRequests = summary.FulfilledRequests,
            BloodTypeDistribution = bloodTypeStats.Select(stat => new BloodTypeDistribution
            {
                BloodType = stat.BloodTypeName,
                DonorCount = stat.DonorCount,
                CompletedDonations = stat.CompletedDonations,
                TotalQuantity = stat.TotalCollectedML,
                FulfilledQuantity = stat.FulfilledRequestsML,
                AvailableQuantity = stat.AvailableML,
                PendingRequests = stat.PendingRequestCount,
                RequestedQuantity = stat.PendingRequestQuantityML,
                Status = stat.GetStatus()
            }).ToList(),
            MonthlyDonations = _context.Donations
                .Where(d => d.Status == "Completed")
                .GroupBy(d => new { d.DonationDate.Year, d.DonationDate.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Count = g.Count(),
                    TotalQuantity = g.Sum(d => d.Quantity)
                })
                .ToList()
                .Select(g => new MonthlyDonationStat
                {
                    Month = $"{g.Year}-{g.Month:D2}",
                    Count = g.Count,
                    TotalQuantity = g.TotalQuantity
                })
                .OrderByDescending(m => m.Month)
                .Take(12)
                .ToList()
        };
    }
}

public class OperationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Data { get; set; }
}

public class DashboardData
{
    public int TotalBloodStock { get; set; }
    public int TotalDonations { get; set; }
    public int PendingDonations { get; set; }
    public int ApprovedDonations { get; set; }
    public int TotalBloodRequests { get; set; }
    public int PendingRequests { get; set; }
    public int ApprovedRequests { get; set; }
    public int TotalUsers { get; set; }
    public List<BloodTypeStat> BloodTypeStats { get; set; } = new();
    public List<Donation> RecentDonations { get; set; } = new();
    public List<BloodRequest> RecentRequests { get; set; } = new();
}

public class BloodTypeStat
{
    public string BloodType { get; set; } = string.Empty;
    public int DonorCount { get; set; }
    public int TotalDonations { get; set; }
    public int PendingDonations { get; set; }
    public int RequestCount { get; set; }
}

public class ReportData
{
    public int TotalDonors { get; set; }
    public int ActiveDonors { get; set; }
    public int TotalDonations { get; set; }
    public int CompletedDonations { get; set; }
    public int TotalBloodRequests { get; set; }
    public int FulfilledRequests { get; set; }
    public List<BloodTypeDistribution> BloodTypeDistribution { get; set; } = new();
    public List<MonthlyDonationStat> MonthlyDonations { get; set; } = new();
}

public class BloodTypeDistribution
{
    public string BloodType { get; set; } = string.Empty;
    public int DonorCount { get; set; }
    public int CompletedDonations { get; set; }
    public int TotalQuantity { get; set; }
    public int FulfilledQuantity { get; set; }
    public int AvailableQuantity { get; set; }
    public int PendingRequests { get; set; }
    public int RequestedQuantity { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class MonthlyDonationStat
{
    public string Month { get; set; } = string.Empty;
    public int Count { get; set; }
    public int TotalQuantity { get; set; }
}
