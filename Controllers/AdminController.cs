using Microsoft.AspNetCore.Mvc;
using BloodDonationSystem.Models;
using BloodDonationSystem.Attributes;
using BloodDonationSystem.Helpers;
using BloodDonationSystem.Services.SpecialServices;
using BloodDonationSystem.Services.ApplicationServices;

namespace BloodDonationSystem.Controllers;
    [AuthorizeRole("Admin")]
    public class AdminController(ExcelExportService excelService, AdminService adminService) : Controller
    {
        private readonly ExcelExportService _excelService = excelService;
        private readonly AdminService _adminService = adminService;

    // Dashboard
    public IActionResult Dashboard()
        {
            var data = _adminService.GetDashboardData();

            var model = new AdminDashboardViewModel
            {
                TotalBloodStock = data.TotalBloodStock,
                TotalDonations = data.TotalDonations,
                PendingDonations = data.PendingDonations,
                ApprovedDonations = data.ApprovedDonations,
                TotalBloodRequests = data.TotalBloodRequests,
                PendingRequests = data.PendingRequests,
                ApprovedRequests = data.ApprovedRequests,
                TotalUsers = data.TotalUsers,
                BloodTypeStats = [.. data.BloodTypeStats.Select(s => new BloodTypeStatViewModel
                {
                    BloodType = s.BloodType,
                    DonorCount = s.DonorCount,
                    TotalDonations = s.TotalDonations,
                    PendingDonations = s.PendingDonations,
                    RequestCount = s.RequestCount
                })],
                RecentDonations = data.RecentDonations,
                RecentRequests = data.RecentRequests
            };

            return View(model);
        }

        // Users Management
        public IActionResult Users()
        {
            var users = _adminService.GetAllUsers();
            return View(users);
        }

        public IActionResult UserDetails(int id)
        {
            var user = _adminService.GetUserDetails(id);

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
            var result = _adminService.ToggleUserStatus(id);

            if (result.Success)
                TempData["SuccessMessage"] = result.Message;
            else
                TempData["ErrorMessage"] = result.Message;

            return RedirectToAction("Users");
        }

        // Donations Management
        public IActionResult Donations()
        {
            var donations = _adminService.GetAllDonations();
            return View(donations);
        }

        [HttpPost]
        public IActionResult ApproveDonation(int id)
        {
            var result = _adminService.ApproveDonation(id, HttpContext.Session.GetUserId() ?? 0);

            if (result.Success)
                TempData["SuccessMessage"] = result.Message;
            else
                TempData["ErrorMessage"] = result.Message;

            return RedirectToAction("Donations");
        }

        [HttpPost]
        public IActionResult CompleteDonation(int id)
        {
            var result = _adminService.CompleteDonation(id);

            if (result.Success)
                TempData["SuccessMessage"] = result.Message;
            else
                TempData["ErrorMessage"] = result.Message;

            return RedirectToAction("Donations");
        }

        [HttpPost]
        public IActionResult RejectDonation(int id, string reason)
        {
            var result = _adminService.RejectDonation(id, reason, HttpContext.Session.GetUserId() ?? 0);

            if (result.Success)
                TempData["SuccessMessage"] = result.Message;
            else
                TempData["ErrorMessage"] = result.Message;

            return RedirectToAction("Donations");
        }

        // Blood Requests Management
        public IActionResult BloodRequests()
        {
            var requests = _adminService.GetAllBloodRequests();
            return View(requests);
        }

        [HttpPost]
        public IActionResult ApproveRequest(int id)
        {
            var result = _adminService.ApproveBloodRequest(id, HttpContext.Session.GetUserId() ?? 0);

            if (result.Success)
                TempData["SuccessMessage"] = result.Message;
            else
                TempData["ErrorMessage"] = result.Message;

            return RedirectToAction("BloodRequests");
        }

        [HttpPost]
        public IActionResult RejectRequest(int id, string reason)
        {
            var result = _adminService.RejectBloodRequest(id, reason, HttpContext.Session.GetUserId() ?? 0);

            if (result.Success)
                TempData["SuccessMessage"] = result.Message;
            else
                TempData["ErrorMessage"] = result.Message;

            return RedirectToAction("BloodRequests");
        }

        [HttpPost]
        public IActionResult FulfillRequest(int id)
        {
            var result = _adminService.FulfillBloodRequest(id);

            if (result.Success)
                TempData["SuccessMessage"] = result.Message;
            else
                TempData["ErrorMessage"] = result.Message;

            return RedirectToAction("BloodRequests");
        }

        // Reports
        public IActionResult Reports()
        {
            var data = _adminService.GetReportData();

            var model = new ReportsViewModel
            {
                TotalDonors = data.TotalDonors,
                ActiveDonors = data.ActiveDonors,
                TotalDonations = data.TotalDonations,
                CompletedDonations = data.CompletedDonations,
                TotalBloodRequests = data.TotalBloodRequests,
                FulfilledRequests = data.FulfilledRequests,
                BloodTypeDistribution = data.BloodTypeDistribution.Select(d => new BloodTypeDistributionViewModel
                {
                    BloodType = d.BloodType,
                    DonorCount = d.DonorCount,
                    CompletedDonations = d.CompletedDonations,
                    TotalQuantity = d.TotalQuantity,
                    FulfilledQuantity = d.FulfilledQuantity,
                    AvailableQuantity = d.AvailableQuantity,
                    PendingRequests = d.PendingRequests,
                    RequestedQuantity = d.RequestedQuantity,
                    Status = d.Status
                }).ToList(),
                MonthlyDonations = data.MonthlyDonations.Select(m => new MonthlyStatViewModel
                {
                    Month = m.Month,
                    Count = m.Count,
                    TotalQuantity = m.TotalQuantity
                }).ToList()
            };

            return View(model);
        }

        public IActionResult ExportReports()
        {
            var data = _adminService.GetReportData();

            var model = new ReportsViewModel
            {
                TotalDonors = data.TotalDonors,
                ActiveDonors = data.ActiveDonors,
                TotalDonations = data.TotalDonations,
                CompletedDonations = data.CompletedDonations,
                TotalBloodRequests = data.TotalBloodRequests,
                FulfilledRequests = data.FulfilledRequests,
                BloodTypeDistribution = data.BloodTypeDistribution.Select(d => new BloodTypeDistributionViewModel
                {
                    BloodType = d.BloodType,
                    DonorCount = d.DonorCount,
                    CompletedDonations = d.CompletedDonations,
                    TotalQuantity = d.TotalQuantity,
                    FulfilledQuantity = d.FulfilledQuantity,
                    AvailableQuantity = d.AvailableQuantity,
                    PendingRequests = d.PendingRequests,
                    RequestedQuantity = d.RequestedQuantity,
                    Status = d.Status
                }).ToList(),
                MonthlyDonations = data.MonthlyDonations.Select(m => new MonthlyStatViewModel
                {
                    Month = m.Month,
                    Count = m.Count,
                    TotalQuantity = m.TotalQuantity
                }).ToList()
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
        public int AvailableQuantity { get; set; }
        public int PendingRequests { get; set; }
        public int RequestedQuantity { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class MonthlyStatViewModel
    {
        public string Month { get; set; } = string.Empty;
        public int Count { get; set; }
        public int TotalQuantity { get; set; }
    }