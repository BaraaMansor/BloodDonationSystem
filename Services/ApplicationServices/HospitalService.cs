using BloodDonationSystem.Data;
using BloodDonationSystem.Models;
using BloodDonationSystem.Services.SpecialServices;
using Microsoft.EntityFrameworkCore;

namespace BloodDonationSystem.Services.ApplicationServices;

public class HospitalService
{
    private readonly ApplicationDbContext _context;
    private readonly BloodInventoryService _inventoryService;

    public HospitalService(ApplicationDbContext context, BloodInventoryService inventoryService)
    {
        _context = context;
        _inventoryService = inventoryService;
    }

    public async Task<HospitalDashboardData?> GetDashboardDataAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);

        if (user == null)
            return null;

        var totalRequests = await _context.BloodRequests
            .Where(r => r.RequestedBy == userId)
            .CountAsync();

        var pendingRequests = await _context.BloodRequests
            .Where(r => r.RequestedBy == userId && r.Status == "Pending")
            .CountAsync();

        var approvedRequests = await _context.BloodRequests
            .Where(r => r.RequestedBy == userId && r.Status == "Approved")
            .CountAsync();

        var fulfilledRequests = await _context.BloodRequests
            .Where(r => r.RequestedBy == userId && r.Status == "Fulfilled")
            .CountAsync();

        var recentRequests = await _context.BloodRequests
            .Include(r => r.BloodType)
            .Where(r => r.RequestedBy == userId)
            .OrderByDescending(r => r.RequestDate)
            .Take(5)
            .ToListAsync();

        return new HospitalDashboardData
        {
            HospitalName = user.FullName,
            Email = user.Email,
            TotalRequests = totalRequests,
            PendingRequests = pendingRequests,
            ApprovedRequests = approvedRequests,
            FulfilledRequests = fulfilledRequests,
            RecentRequests = recentRequests
        };
    }

    public async Task<List<BloodType>> GetBloodTypesAsync()
    {
        return await _context.BloodTypes.ToListAsync();
    }

    public async Task<OperationResult> CreateBloodRequestAsync(int userId, int bloodTypeId, int quantity, string? notes, bool isEmergency)
    {
        var user = await _context.Users.FindAsync(userId);

        if (user == null)
        {
            return new OperationResult { Success = false, Message = "User not found." };
        }

        var request = new BloodRequest
        {
            BloodTypeId = bloodTypeId,
            RequestedBy = userId,
            Quantity = quantity,
            HospitalName = user.FullName,
            Notes = notes,
            IsEmergency = isEmergency,
            Status = "Pending",
            RequestDate = DateTime.Now
        };

        _context.BloodRequests.Add(request);
        await _context.SaveChangesAsync();

        return new OperationResult
        {
            Success = true,
            Message = "Blood request submitted successfully!",
            Data = request.Id.ToString()
        };
    }

    public async Task<List<BloodRequest>> GetRequestHistoryAsync(int userId)
    {
        return await _context.BloodRequests
            .Include(r => r.BloodType)
            .Where(r => r.RequestedBy == userId)
            .OrderByDescending(r => r.RequestDate)
            .ToListAsync();
    }

    public List<BloodAvailabilityInfo> GetBloodAvailability()
    {
        var bloodAvailability = _inventoryService.GetBloodTypeDistribution();

        return bloodAvailability
            .Select(b => new BloodAvailabilityInfo
            {
                BloodType = b.BloodTypeName,
                Description = b.Description ?? string.Empty,
                AvailableQuantity = b.AvailableML
            })
            .OrderBy(b => b.BloodType)
            .ToList();
    }

    public async Task<BloodRequest?> GetRequestDetailsAsync(int userId, int requestId)
    {
        return await _context.BloodRequests
            .Include(r => r.BloodType)
            .FirstOrDefaultAsync(r => r.Id == requestId && r.RequestedBy == userId);
    }
}

public class HospitalDashboardData
{
    public string HospitalName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int TotalRequests { get; set; }
    public int PendingRequests { get; set; }
    public int ApprovedRequests { get; set; }
    public int FulfilledRequests { get; set; }
    public List<BloodRequest> RecentRequests { get; set; } = new();
}

public class BloodAvailabilityInfo
{
    public string BloodType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int AvailableQuantity { get; set; }
}
