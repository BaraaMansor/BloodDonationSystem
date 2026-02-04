using Microsoft.AspNetCore.Mvc;
using BloodDonationSystem.Models;
using BloodDonationSystem.Attributes;
using BloodDonationSystem.Helpers;
using BloodDonationSystem.Services.ApplicationServices;
using System.ComponentModel.DataAnnotations;

namespace BloodDonationSystem.Controllers;
    [AuthorizeRole("Donor")]
    public class DonorController(DonorService donorService) : Controller
    {
        private readonly DonorService _donorService = donorService;

    // Dashboard
    public IActionResult Dashboard()
        {
            var userId = HttpContext.Session.GetUserId() ?? 0;
            var data = _donorService.GetDashboardData(userId);

            if (data == null)
            {
                TempData["ErrorMessage"] = "Donor profile not found.";
                return RedirectToAction("Index", "Home");
            }

            var model = new DonorDashboardViewModel
            {
                Donor = data.Donor,
                TotalDonations = data.TotalDonations,
                CompletedDonations = data.CompletedDonations,
                PendingDonations = data.PendingDonations,
                LastDonationDate = data.LastDonationDate,
                CanDonateAgain = data.CanDonateAgain,
                DaysUntilEligible = data.DaysUntilEligible,
                RecentDonations = data.RecentDonations
            };

            return View(model);
        }

        // View Profile
        public IActionResult Profile()
        {
            var userId = HttpContext.Session.GetUserId() ?? 0;
            var donor = _donorService.GetDonorProfile(userId);

            if (donor == null)
            {
                TempData["ErrorMessage"] = "Donor profile not found.";
                return RedirectToAction("Dashboard");
            }

            return View(donor);
        }

        // GET: Edit Profile
        public IActionResult EditProfile()
        {
            var userId = HttpContext.Session.GetUserId() ?? 0;
            var donor = _donorService.GetDonorWithUser(userId);

            if (donor == null)
            {
                TempData["ErrorMessage"] = "Donor profile not found.";
                return RedirectToAction("Dashboard");
            }

            var model = new EditDonorProfileViewModel
            {
                FullName = donor.User.FullName,
                Phone = donor.User.Phone,
                Address = donor.Address,
                Gender = donor.Gender,
                MedicalNotes = donor.MedicalNotes
            };

            return View(model);
        }

        // POST: Edit Profile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditProfile(EditDonorProfileViewModel model)
        {
            if (ModelState.IsValid)
            {
                var userId = HttpContext.Session.GetUserId() ?? 0;
                var result = _donorService.UpdateProfile(
                    userId,
                    model.FullName,
                    model.Phone,
                    model.Address,
                    model.Gender,
                    model.MedicalNotes
                );

                if (!result.Success)
                {
                    TempData["ErrorMessage"] = result.Message;
                    return RedirectToAction("Dashboard");
                }

                // Update session name if changed
                HttpContext.Session.SetString("UserName", result.Data!);

                TempData["SuccessMessage"] = result.Message;
                return RedirectToAction("Profile");
            }

            return View(model);
        }

        // GET: Create Donation
        public IActionResult CreateDonation()
        {
            var userId = HttpContext.Session.GetUserId() ?? 0;
            var eligibility = _donorService.CheckDonationEligibility(userId);

            if (!eligibility.IsEligible)
            {
                if (eligibility.ErrorMessage == "Donor profile not found.")
                    TempData["ErrorMessage"] = eligibility.ErrorMessage;
                return RedirectToAction("Dashboard");
            }

            ViewBag.BloodType = eligibility.BloodTypeName;
            return View(new CreateDonationViewModel());
        }

        // POST: Create Donation
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateDonation(CreateDonationViewModel model)
        {
            if (ModelState.IsValid)
            {
                var userId = HttpContext.Session.GetUserId() ?? 0;
                var result = _donorService.CreateDonation(
                    userId,
                    model.DonationDate,
                    model.Quantity,
                    model.Notes
                );

                if (result.Success)
                    TempData["SuccessMessage"] = result.Message;
                else
                    TempData["ErrorMessage"] = result.Message;

                return RedirectToAction("Dashboard");
            }

            var userId2 = HttpContext.Session.GetUserId() ?? 0;
            var eligibility = _donorService.CheckDonationEligibility(userId2);
            ViewBag.BloodType = eligibility.BloodTypeName;

            return View(model);
        }

        // Toggle Availability
        [HttpPost]
        public IActionResult ToggleAvailability()
        {
            var userId = HttpContext.Session.GetUserId() ?? 0;
            var result = _donorService.ToggleAvailability(userId);

            if (result.Success)
                TempData["SuccessMessage"] = result.Message;
            else
                TempData["ErrorMessage"] = result.Message;

            return RedirectToAction("Dashboard");
        }
    }

    // View Models
    public class DonorDashboardViewModel
    {
        public Donor Donor { get; set; } = null!;
        public int TotalDonations { get; set; }
        public int CompletedDonations { get; set; }
        public int PendingDonations { get; set; }
        public DateTime? LastDonationDate { get; set; }
        public bool CanDonateAgain { get; set; }
        public int DaysUntilEligible { get; set; }
        public List<Donation> RecentDonations { get; set; } = new();
    }

    public class EditDonorProfileViewModel
    {
        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Phone]
        public string? Phone { get; set; }

        [Required]
        [StringLength(500)]
        public string Address { get; set; } = string.Empty;

        [Required]
        public string Gender { get; set; } = string.Empty;

        [StringLength(500)]
        public string? MedicalNotes { get; set; }
    }

    public class CreateDonationViewModel
    {
        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Preferred Donation Date")]
        public DateTime DonationDate { get; set; } = DateTime.Now.AddDays(1);

        [Required]
        [Range(350, 500)]
        [Display(Name = "Quantity (ml)")]
        public int Quantity { get; set; } = 450;

        [StringLength(500)]
        public string? Notes { get; set; }
    }