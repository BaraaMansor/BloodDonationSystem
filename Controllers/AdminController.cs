using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BloodDonationSystem.Data;
using BloodDonationSystem.Models;
using BloodDonationSystem.Attributes;
using BloodDonationSystem.Helpers;
using BloodDonationSystem.Services;

namespace BloodDonationSystem.Controllers;
    [AuthorizeRole("Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ExcelExportService _excelService;
        private readonly BloodCompatibilityService _bloodCompatibility;

        public AdminController(ApplicationDbContext context, ExcelExportService excelService, BloodCompatibilityService bloodCompatibility)
        {
            _context = context;
            _excelService = excelService;
            _bloodCompatibility = bloodCompatibility;
        }

        // Dashboard
        public IActionResult Dashboard()
        {
            var model = new AdminDashboardViewModel
            {
                TotalBloodStock = _context.Donations.Where(d => d.Status == "Completed").Sum(d => (int?)d.Quantity) ?? 0 - 
                                  _context.BloodRequests.Where(r => r.Status == "Fulfilled").Sum(r => (int?)r.Quantity) ?? 0,
                TotalDonations = _context.Donations.Count(),
                PendingDonations = _context.Donations.Count(d => d.Status == "Pending"),
                ApprovedDonations = _context.Donations.Count(d => d.Status == "Approved"),
                TotalBloodRequests = _context.BloodRequests.Count(),
                PendingRequests = _context.BloodRequests.Count(r => r.Status == "Pending"),
                ApprovedRequests = _context.BloodRequests.Count(r => r.Status == "Approved"),
                TotalUsers = _context.Users.Count(),
                BloodTypeStats = _context.BloodTypes
                    .Select(bt => new BloodTypeStatViewModel
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

            return View(model);
        }

        // Users Management
        public IActionResult Users()
        {
            var users = _context.Users
                .Include(u => u.Donor)
                .ThenInclude(d => d!.BloodType)
                .OrderByDescending(u => u.CreatedAt)
                .ToList();

            return View(users);
        }

        public IActionResult UserDetails(int id)
        {
            var user = _context.Users
                .Include(u => u.Donor)
                .ThenInclude(d => d!.BloodType)
                .Include(u => u.Donor)
                .ThenInclude(d => d!.Donations)
                .FirstOrDefault(u => u.Id == id);

            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("Users");
            }

            return View(user);
        }

        [HttpPost]
        public IActionResult ToggleUserStatus(int id)
        {
            var user = _context.Users.Find(id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("Users");
            }

            user.IsActive = !user.IsActive;
            _context.SaveChanges();

            TempData["SuccessMessage"] = $"User {(user.IsActive ? "activated" : "deactivated")} successfully.";
            return RedirectToAction("Users");
        }

        // Donations Management
        public IActionResult Donations()
        {
            var donations = _context.Donations
                .Include(d => d.Donor)
                .ThenInclude(d => d.User)
                .Include(d => d.Donor.BloodType)
                .OrderByDescending(d => d.CreatedAt)
                .ToList();

            return View(donations);
        }

        [HttpPost]
        public IActionResult ApproveDonation(int id)
        {
            var donation = _context.Donations.Include(d => d.Donor).FirstOrDefault(d => d.Id == id);
            if (donation == null)
            {
                TempData["ErrorMessage"] = "Donation not found.";
                return RedirectToAction("Donations");
            }

            // Check if donor can donate (90 days rule)
            if (donation.Donor.LastDonationDate.HasValue)
            {
                var daysSinceLastDonation = (DateTime.Now - donation.Donor.LastDonationDate.Value).TotalDays;
                if (daysSinceLastDonation < 90)
                {
                    TempData["ErrorMessage"] = $"Donor must wait {90 - (int)daysSinceLastDonation} more days before donating again.";
                    return RedirectToAction("Donations");
                }
            }

            donation.Status = "Approved";
            donation.ApprovedAt = DateTime.Now;
            donation.ApprovedBy = HttpContext.Session.GetUserId();

            _context.SaveChanges();

            TempData["SuccessMessage"] = "Donation approved successfully.";
            return RedirectToAction("Donations");
        }

        [HttpPost]
        public IActionResult CompleteDonation(int id)
        {
            var donation = _context.Donations.Include(d => d.Donor).FirstOrDefault(d => d.Id == id);
            if (donation == null)
            {
                TempData["ErrorMessage"] = "Donation not found.";
                return RedirectToAction("Donations");
            }

            donation.Status = "Completed";
            donation.Donor.LastDonationDate = donation.DonationDate;
            donation.Donor.IsAvailable = false; // Mark as unavailable for 90 days

            _context.SaveChanges();

            TempData["SuccessMessage"] = "Donation marked as completed.";
            return RedirectToAction("Donations");
        }

        [HttpPost]
        public IActionResult RejectDonation(int id, string reason)
        {
            var donation = _context.Donations.Find(id);
            if (donation == null)
            {
                TempData["ErrorMessage"] = "Donation not found.";
                return RedirectToAction("Donations");
            }

            donation.Status = "Rejected";
            donation.Notes = reason ?? "Rejected by admin";
            donation.ApprovedBy = HttpContext.Session.GetUserId();

            _context.SaveChanges();

            TempData["SuccessMessage"] = "Donation rejected.";
            return RedirectToAction("Donations");
        }

        // Blood Requests Management
        public IActionResult BloodRequests()
        {
            var requests = _context.BloodRequests
                .Include(r => r.BloodType)
                .OrderByDescending(r => r.CreatedAt)
                .ToList();

            return View(requests);
        }

        [HttpPost]
        public IActionResult ApproveRequest(int id)
        {
            var request = _context.BloodRequests.Find(id);
            if (request == null)
            {
                TempData["ErrorMessage"] = "Request not found.";
                return RedirectToAction("BloodRequests");
            }

            request.Status = "Approved";
            request.ApprovedAt = DateTime.Now;
            request.ApprovedBy = HttpContext.Session.GetUserId();

            _context.SaveChanges();

            TempData["SuccessMessage"] = "Blood request approved successfully.";
            return RedirectToAction("BloodRequests");
        }

        [HttpPost]
        public IActionResult RejectRequest(int id, string reason)
        {
            var request = _context.BloodRequests.Find(id);
            if (request == null)
            {
                TempData["ErrorMessage"] = "Request not found.";
                return RedirectToAction("BloodRequests");
            }

            request.Status = "Rejected";
            request.AdminNotes = reason ?? "Rejected by admin";
            request.ApprovedAt = DateTime.Now;
            request.ApprovedBy = HttpContext.Session.GetUserId();

            _context.SaveChanges();

            TempData["SuccessMessage"] = "Blood request rejected.";
            return RedirectToAction("BloodRequests");
        }

        [HttpPost]
        public IActionResult FulfillRequest(int id)
        {
            var request = _context.BloodRequests
                .Include(r => r.BloodType)
                .FirstOrDefault(r => r.Id == id);
                
            if (request == null)
            {
                TempData["ErrorMessage"] = "Request not found.";
                return RedirectToAction("BloodRequests");
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

                // Calculate available quantity for this specific blood type
                var donated = _context.Donations
                    .Where(d => d.Donor.BloodTypeId == bloodTypeId && d.Status == "Completed")
                    .Sum(d => (int?)d.Quantity) ?? 0;

                var fulfilled = _context.BloodRequests
                    .Where(r => r.FulfilledWithBloodTypeId == bloodTypeId && r.Status == "Fulfilled")
                    .Sum(r => (int?)r.Quantity) ?? 0;

                var available = donated - fulfilled;

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
                
                var totalDonated = _context.Donations
                    .Where(d => compatibleBloodTypeIds.Contains(d.Donor.BloodTypeId) && d.Status == "Completed")
                    .Sum(d => (int?)d.Quantity) ?? 0;

                var totalFulfilled = _context.BloodRequests
                    .Where(r => r.FulfilledWithBloodTypeId.HasValue && 
                               compatibleBloodTypeIds.Contains(r.FulfilledWithBloodTypeId.Value) && 
                               r.Status == "Fulfilled")
                    .Sum(r => (int?)r.Quantity) ?? 0;

                var totalAvailable = totalDonated - totalFulfilled;

                TempData["ErrorMessage"] = $"Insufficient compatible blood! Available: {totalAvailable}ml, Requested: {request.Quantity}ml. Need {request.Quantity - totalAvailable}ml more. Compatible types: {string.Join(", ", compatibleBloodTypes)}";
                return RedirectToAction("BloodRequests");
            }

            // Mark as fulfilled and record which blood type was actually used
            request.Status = "Fulfilled";
            request.FulfilledWithBloodTypeId = selectedBloodTypeId.Value;
            _context.SaveChanges();

            var message = selectedBloodType == request.BloodType.TypeName
                ? $"Blood request fulfilled successfully with {selectedBloodType}. Remaining: {selectedAvailable - request.Quantity}ml"
                : $"Blood request fulfilled successfully using compatible blood type {selectedBloodType} (requested: {request.BloodType.TypeName}). Remaining {selectedBloodType}: {selectedAvailable - request.Quantity}ml";

            TempData["SuccessMessage"] = message;
            return RedirectToAction("BloodRequests");
        }

        // Reports
        public IActionResult Reports()
        {
            var fulfilledByActualType = _context.BloodRequests
                .Where(r => r.Status == "Fulfilled" && r.FulfilledWithBloodTypeId.HasValue)
                .GroupBy(r => r.FulfilledWithBloodTypeId!.Value)
                .Select(g => new { BloodTypeId = g.Key, FulfilledQuantity = g.Sum(r => (int?)r.Quantity) ?? 0 })
                .ToDictionary(x => x.BloodTypeId, x => x.FulfilledQuantity);

            var bloodTypeDistribution = _context.BloodTypes
                .Select(bt => new 
                {
                    BloodTypeId = bt.Id,
                    BloodType = bt.TypeName,
                    DonorCount = bt.Donors.Count(),
                    CompletedDonations = bt.Donors.SelectMany(d => d.Donations).Count(d => d.Status == "Completed"),
                    TotalQuantity = bt.Donors.SelectMany(d => d.Donations).Where(d => d.Status == "Completed").Sum(d => (int?)d.Quantity) ?? 0,
                    PendingRequests = bt.BloodRequests.Count(r => r.Status == "Pending" || r.Status == "Approved"),
                    RequestedQuantity = bt.BloodRequests.Where(r => r.Status == "Pending" || r.Status == "Approved").Sum(r => (int?)r.Quantity) ?? 0
                })
                .ToList()
                .Select(bt => new BloodTypeDistributionViewModel
                {
                    BloodType = bt.BloodType,
                    DonorCount = bt.DonorCount,
                    CompletedDonations = bt.CompletedDonations,
                    TotalQuantity = bt.TotalQuantity,
                    FulfilledQuantity = fulfilledByActualType.GetValueOrDefault(bt.BloodTypeId, 0),
                    PendingRequests = bt.PendingRequests,
                    RequestedQuantity = bt.RequestedQuantity
                })
                .ToList();

            var model = new ReportsViewModel
            {
                TotalDonors = _context.Donors.Count(),
                ActiveDonors = _context.Donors.Count(d => d.IsAvailable),
                TotalDonations = _context.Donations.Count(),
                CompletedDonations = _context.Donations.Count(d => d.Status == "Completed"),
                TotalBloodRequests = _context.BloodRequests.Count(),
                FulfilledRequests = _context.BloodRequests.Count(r => r.Status == "Fulfilled"),
                BloodTypeDistribution = bloodTypeDistribution,
                MonthlyDonations = [.. _context.Donations
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
                    .Select(g => new MonthlyStatViewModel
                    {
                        Month = $"{g.Year}-{g.Month:D2}",
                        Count = g.Count,
                        TotalQuantity = g.TotalQuantity
                    })
                    .OrderByDescending(m => m.Month)
                    .Take(12)]
            };

            return View(model);
        }

        public IActionResult ExportReports()
        {
            var model = new ReportsViewModel
            {
                TotalDonors = _context.Donors.Count(),
                ActiveDonors = _context.Donors.Count(d => d.IsAvailable),
                TotalDonations = _context.Donations.Count(),
                CompletedDonations = _context.Donations.Count(d => d.Status == "Completed"),
                TotalBloodRequests = _context.BloodRequests.Count(),
                FulfilledRequests = _context.BloodRequests.Count(r => r.Status == "Fulfilled"),
                BloodTypeDistribution = _context.BloodTypes
                    .Select(bt => new BloodTypeDistributionViewModel
                    {
                        BloodType = bt.TypeName,
                        DonorCount = bt.Donors.Count(),
                        CompletedDonations = bt.Donors.SelectMany(d => d.Donations).Count(d => d.Status == "Completed"),
                        TotalQuantity = bt.Donors.SelectMany(d => d.Donations).Where(d => d.Status == "Completed").Sum(d => d.Quantity),
                        FulfilledQuantity = bt.BloodRequests.Where(r => r.Status == "Fulfilled").Sum(r => r.Quantity),
                        PendingRequests = bt.BloodRequests.Count(r => r.Status == "Pending" || r.Status == "Approved"),
                        RequestedQuantity = bt.BloodRequests.Where(r => r.Status == "Pending" || r.Status == "Approved").Sum(r => r.Quantity)
                    }).ToList(),
                MonthlyDonations = [.. _context.Donations
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
                    .Select(g => new MonthlyStatViewModel
                    {
                        Month = $"{g.Year}-{g.Month:D2}",
                        Count = g.Count,
                        TotalQuantity = g.TotalQuantity
                    })
                    .OrderByDescending(m => m.Month)
                    .Take(12)]
            };

            var excelData = _excelService.GenerateReportsExcel(model);
            return File(excelData, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"BloodDonationReports_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
        }
    }

    // View Models
    public class AdminDashboardViewModel
    {
        public int TotalBloodStock { get; set; }
        public int TotalDonations { get; set; }
        public int PendingDonations { get; set; }
        public int ApprovedDonations { get; set; }
        public int TotalBloodRequests { get; set; }
        public int PendingRequests { get; set; }
        public int ApprovedRequests { get; set; }
        public int TotalUsers { get; set; }
        public List<BloodTypeStatViewModel> BloodTypeStats { get; set; } = new();
        public List<Donation> RecentDonations { get; set; } = new();
        public List<BloodRequest> RecentRequests { get; set; } = new();
    }

    public class BloodTypeStatViewModel
    {
        public string BloodType { get; set; } = string.Empty;
        public int DonorCount { get; set; }
        public int TotalDonations { get; set; }
        public int PendingDonations { get; set; }
        public int RequestCount { get; set; }
    }

    public class ReportsViewModel
    {
        public int TotalDonors { get; set; }
        public int ActiveDonors { get; set; }
        public int TotalDonations { get; set; }
        public int CompletedDonations { get; set; }
        public int TotalBloodRequests { get; set; }
        public int FulfilledRequests { get; set; }
        public List<BloodTypeDistributionViewModel> BloodTypeDistribution { get; set; } = new();
        public List<MonthlyStatViewModel> MonthlyDonations { get; set; } = new();
    }

    public class BloodTypeDistributionViewModel
    {
        public string BloodType { get; set; } = string.Empty;
        public int DonorCount { get; set; }
        public int CompletedDonations { get; set; }
        public int TotalQuantity { get; set; }
        public int FulfilledQuantity { get; set; }
        public int AvailableQuantity => TotalQuantity - FulfilledQuantity;
        public int PendingRequests { get; set; }
        public int RequestedQuantity { get; set; }
    }

    public class MonthlyStatViewModel
    {
        public string Month { get; set; } = string.Empty;
        public int Count { get; set; }
        public int TotalQuantity { get; set; }
    }