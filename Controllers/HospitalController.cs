using Microsoft.AspNetCore.Mvc;
using BloodDonationSystem.Models;
using BloodDonationSystem.Attributes;
using BloodDonationSystem.Helpers;
using BloodDonationSystem.Services.ApplicationServices;
using System.ComponentModel.DataAnnotations;

namespace BloodDonationSystem.Controllers;
    [AuthorizeRole("Hospital")]
    public class HospitalController(HospitalService hospitalService) : Controller
    {
        private readonly HospitalService _hospitalService = hospitalService;

    // Dashboard
    public async Task<IActionResult> Dashboard()
        {
            var userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            var data = await _hospitalService.GetDashboardDataAsync(userId);

            if (data == null)
                return RedirectToAction("Login", "Account");

            var viewModel = new HospitalDashboardViewModel
            {
                HospitalName = data.HospitalName,
                Email = data.Email,
                TotalRequests = data.TotalRequests,
                PendingRequests = data.PendingRequests,
                ApprovedRequests = data.ApprovedRequests,
                FulfilledRequests = data.FulfilledRequests,
                RecentRequests = data.RecentRequests
            };

            return View(viewModel);
        }

        // GET: Create Blood Request
        public async Task<IActionResult> CreateRequest()
        {
            var bloodTypes = await _hospitalService.GetBloodTypesAsync();
            ViewBag.BloodTypes = bloodTypes;
            return View();
        }

        // POST: Create Blood Request
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRequest(CreateBloodRequestViewModel model)
        {
            if (ModelState.IsValid)
            {
                var userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                var result = await _hospitalService.CreateBloodRequestAsync(
                    userId,
                    model.BloodTypeId,
                    model.QuantityRequired,
                    model.Notes,
                    model.IsEmergency
                );

                if (result.Success)
                {
                    TempData["SuccessMessage"] = $"{result.Message} Request ID: {result.Data}";
                    return RedirectToAction("Dashboard");
                }

                TempData["ErrorMessage"] = result.Message;
            }

            var bloodTypes = await _hospitalService.GetBloodTypesAsync();
            ViewBag.BloodTypes = bloodTypes;
            return View(model);
        }

        // Request History
        public async Task<IActionResult> RequestHistory()
        {
            var userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            var requests = await _hospitalService.GetRequestHistoryAsync(userId);
            return View(requests);
        }

        public IActionResult BloodAvailability()
        {
            var bloodAvailability = _hospitalService.GetBloodAvailability();

            var viewModels = bloodAvailability
                .Select(b => new BloodAvailabilityViewModel
                {
                    BloodType = b.BloodType,
                    Description = b.Description,
                    AvailableQuantity = b.AvailableQuantity
                })
                .ToList();

            return View(viewModels);
        }

        // Request Details
        public async Task<IActionResult> RequestDetails(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            var request = await _hospitalService.GetRequestDetailsAsync(userId, id);

            if (request == null)
                return NotFound();

            return View(request);
        }
    }

    // ViewModels
    public class HospitalDashboardViewModel
    {
        public string HospitalName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int TotalRequests { get; set; }
        public int PendingRequests { get; set; }
        public int ApprovedRequests { get; set; }
        public int FulfilledRequests { get; set; }
        public List<BloodRequest> RecentRequests { get; set; } = new();
    }

    public class CreateBloodRequestViewModel
    {
        [Required(ErrorMessage = "Please select a blood type")]
        [Display(Name = "Blood Type")]
        public int BloodTypeId { get; set; }

        [Required(ErrorMessage = "Quantity is required")]
        [Range(100, 5000, ErrorMessage = "Quantity must be between 100ml and 5000ml")]
        [Display(Name = "Quantity Required (ml)")]
        public int QuantityRequired { get; set; }

        [Display(Name = "Emergency Request")]
        public bool IsEmergency { get; set; }

        [Display(Name = "Notes / Special Requirements")]
        public string? Notes { get; set; }
    }

    public class BloodAvailabilityViewModel
    {
        public string BloodType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int AvailableQuantity { get; set; }
    }