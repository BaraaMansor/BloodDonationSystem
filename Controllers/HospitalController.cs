using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BloodDonationSystem.Data;
using BloodDonationSystem.Models;
using BloodDonationSystem.Attributes;
using BloodDonationSystem.Helpers;
using System.ComponentModel.DataAnnotations;

namespace BloodDonationSystem.Controllers;
    [AuthorizeRole("Hospital")]
    public class HospitalController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HospitalController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Dashboard
        public async Task<IActionResult> Dashboard()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
                return RedirectToAction("Login", "Account");

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

            var viewModel = new HospitalDashboardViewModel
            {
                HospitalName = user.FullName,
                Email = user.Email,
                TotalRequests = totalRequests,
                PendingRequests = pendingRequests,
                ApprovedRequests = approvedRequests,
                FulfilledRequests = fulfilledRequests,
                RecentRequests = recentRequests
            };

            return View(viewModel);
        }

        // GET: Create Blood Request
        public async Task<IActionResult> CreateRequest()
        {
            var bloodTypes = await _context.BloodTypes.ToListAsync();
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
                var userId = HttpContext.Session.GetInt32("UserId");
                var user = await _context.Users.FindAsync(userId);
                
                var request = new BloodRequest
                {
                    BloodTypeId = model.BloodTypeId,
                    RequestedBy = userId!.Value,
                    Quantity = model.QuantityRequired,
                    HospitalName = user!.FullName,
                    Notes = model.Notes,
                    Status = "Pending",
                    RequestDate = DateTime.Now
                };

                _context.BloodRequests.Add(request);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Blood request submitted successfully! Request ID: " + request.Id;
                return RedirectToAction("Dashboard");
            }

            var bloodTypes = await _context.BloodTypes.ToListAsync();
            ViewBag.BloodTypes = bloodTypes;
            return View(model);
        }

        // Request History
        public async Task<IActionResult> RequestHistory()
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            var requests = await _context.BloodRequests
                .Include(r => r.BloodType)
                .Where(r => r.RequestedBy == userId)
                .OrderByDescending(r => r.RequestDate)
                .ToListAsync();

            return View(requests);
        }

        // Blood Availability
        public async Task<IActionResult> BloodAvailability()
        {
            // Get all completed donations grouped by blood type
            var bloodAvailability = await _context.Donations
                .Include(d => d.Donor)
                    .ThenInclude(donor => donor.BloodType)
                .Where(d => d.Status == "Completed")
                .GroupBy(d => new { d.Donor.BloodType.TypeName, d.Donor.BloodType.Description })
                .Select(g => new BloodAvailabilityViewModel
                {
                    BloodType = g.Key.TypeName,
                    Description = g.Key.Description ?? string.Empty,
                    AvailableUnits = g.Count(),
                    TotalQuantity = g.Sum(d => d.Quantity)
                })
                .OrderBy(b => b.BloodType)
                .ToListAsync();

            // Get available donors by blood type
            var availableDonors = await _context.Donors
                .Include(d => d.BloodType)
                .Where(d => d.IsAvailable)
                .GroupBy(d => d.BloodType.TypeName)
                .Select(g => new { BloodType = g.Key, Count = g.Count() })
                .ToListAsync();

            // Add available donor count to viewmodel
            foreach (var item in bloodAvailability)
            {
                var donorCount = availableDonors.FirstOrDefault(d => d.BloodType == item.BloodType);
                item.AvailableDonors = donorCount?.Count ?? 0;
            }

            return View(bloodAvailability);
        }

        // Request Details
        public async Task<IActionResult> RequestDetails(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            var request = await _context.BloodRequests
                .Include(r => r.BloodType)
                .FirstOrDefaultAsync(r => r.Id == id && r.RequestedBy == userId);

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

        [Display(Name = "Notes / Special Requirements")]
        public string? Notes { get; set; }
    }

    public class BloodAvailabilityViewModel
    {
        public string BloodType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int AvailableUnits { get; set; }
        public int TotalQuantity { get; set; }
        public int AvailableDonors { get; set; }
    }