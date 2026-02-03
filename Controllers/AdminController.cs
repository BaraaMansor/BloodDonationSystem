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
                TotalBloodStock = _context.Donations.Where(d => d.Status == "Completed").Sum(d => d.Quantity) - 
                                  _context.BloodRequests.Where(r => r.Status == "Fulfilled").Sum(r => r.Quantity),
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

            // Get compatible blood types for this request
            var compatibleBloodTypes = _bloodCompatibility.GetCompatibleDonors(request.BloodType.TypeName);
            
            // Get blood type IDs for compatible types
            var compatibleBloodTypeIds = _context.BloodTypes
                .Where(bt => compatibleBloodTypes.Contains(bt.TypeName))
                .Select(bt => bt.Id)
                .ToList();

            // Calculate available quantity from ALL compatible blood types
            var totalDonated = _context.Donations
                .Where(d => compatibleBloodTypeIds.Contains(d.Donor.BloodTypeId) && d.Status == "Completed")
                .Sum(d => d.Quantity);

            var totalFulfilled = _context.BloodRequests
                .Where(r => compatibleBloodTypeIds.Contains(r.BloodTypeId) && r.Status == "Fulfilled")
                .Sum(r => r.Quantity);

            var availableQuantity = totalDonated - totalFulfilled;

            if (availableQuantity < request.Quantity)
            {
                TempData["ErrorMessage"] = $"Insufficient compatible blood! Available: {availableQuantity}ml, Requested: {request.Quantity}ml. Need {request.Quantity - availableQuantity}ml more. Compatible types: {string.Join(", ", compatibleBloodTypes)}";
                return RedirectToAction("BloodRequests");
            }

            request.Status = "Fulfilled";
            _context.SaveChanges();

            TempData["SuccessMessage"] = $"Blood request fulfilled successfully using compatible blood types. Remaining compatible blood: {availableQuantity - request.Quantity}ml";
            return RedirectToAction("BloodRequests");
        }

        // Reports
        public IActionResult Reports()
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
                    .Select(g => new MonthlyStatViewModel
                    {
                        Month = $"{g.Year}-{g.Month:D2}",
                        Count = g.Count,
                        TotalQuantity = g.TotalQuantity
                    })
                    .OrderByDescending(m => m.Month)
                    .Take(12)
                    .ToList()
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
                    .Select(g => new MonthlyStatViewModel
                    {
                        Month = $"{g.Year}-{g.Month:D2}",
                        Count = g.Count,
                        TotalQuantity = g.TotalQuantity
                    })
                    .OrderByDescending(m => m.Month)
                    .Take(12)
                    .ToList()
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